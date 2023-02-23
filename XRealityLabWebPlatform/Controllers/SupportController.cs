/*using Application.Security;
using ARI.Application;
using ARI.Security;
using ARI.Users.Queries;
using AzureStorage;
using Common.Constants;
using Infrastructure.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ARI.Controllers
{
    public class SupportController : Controller
    {
        private readonly ARIContext _context;
        private IUserRepository _userRepository;
        private readonly IHostingEnvironment _environment;
        private readonly GoogleReCAPTCHAService _googleReCAPTCHAService;
        private readonly CustomIDataProtection protector;

        public SupportController(ARIContext context, IHostingEnvironment IHostingEnvironment, CustomIDataProtection customIDataProtection,
            GoogleReCAPTCHAService googleReCAPTCHAService)
        {
            _context = context;
            _userRepository = new UserRepository(_context);
            _environment = IHostingEnvironment;
            _googleReCAPTCHAService = googleReCAPTCHAService;
            protector = customIDataProtection;
        }
        
        public IActionResult Index()
        {
            return RedirectToAction(nameof(RequestList));  
        }

        [Authorize(Roles = "IT,Υπεύθυνος Ροών Εταιρίας")]
        public async Task<IActionResult> RequestList()
        {
            List<RequestDTO> complaintDto = await _context.Complaints.Include(k => k.Subject).Select(x => new RequestDTO
            {
                RequestId = x.ComplaintId,
                PassToken = protector.Decode(x.ComplaintId.ToString()),
                FullName = x.FullName,
                Email = x.Email,
                Street = x.Street,
                City = x.City,
                Telephone = x.Telephone,
                Description = x.Description,
                SubmissionDateTime = x.IssuedDateTime,
                FilePath = x.Documents
            }).ToListAsync();

            return View(complaintDto);
        }
   
        public IActionResult Request()
        {
            return View(new RequestDTO { ComplaintSubjectListItems = LoadComplaintSubjects().Result.OrderByDescending(k => k.Text), SelectedComplaintSubject = "-" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Request(RequestDTO requestDTO)
        {
            //Google reCAPTCHA validation
            var _GoogleReCAPTCHA = _googleReCAPTCHAService.VerifyReCAPTCHA(requestDTO.Token);

            if (!_GoogleReCAPTCHA.Result.success && _GoogleReCAPTCHA.Result.score <= 0.8) return AuthResult(requestDTO, "Προέκυψε κάποιο πρόβλημα, παρακαλώ προσπαθήστε αργότερα.");

            //if (!ModelState.IsValid) return RedirectToAction(nameof(Request));

            if (requestDTO.SelectedComplaintSubject == null) return AuthResult(requestDTO, "Συμπληρώστε όλα τα πεδία");

            if(requestDTO.Street != null)
                if(requestDTO.Street.Length > 20 || requestDTO.Street.Length < 2) return AuthResult(requestDTO, "Το μέγεθος του πεδίου Διεύθυνση είναι μεγάλο. Παρακαλώ προσπαθήστε ξανά");

            if (requestDTO.City != null)
                if (requestDTO.City.Length > 20 || requestDTO.City.Length < 2) return AuthResult(requestDTO, "Το μέγεθος του πεδίου Πόλη είναι μεγάλο. Παρακαλώ προσπαθήστε ξανά");

            if (requestDTO.Telephone != null)
                if (requestDTO.Telephone.Length > 15 || requestDTO.Telephone.Length < 2) return AuthResult(requestDTO, "Το μέγεθος του πεδίου Τηλεφωνο είναι μεγάλο. Παρακαλώ προσπαθήστε ξανά");

            string filePath = string.Empty;
            if (requestDTO.iFormFile != null)
            {

                if (requestDTO.iFormFile.ContentType != ".pdf")
                {
                    if (requestDTO.iFormFile.Length > 5000000) return AuthResult(requestDTO, "Το μέγεθος του συνημμένου αρχείου είναι μεγάλο. Παρακαλώ προσπαθήστε ξανά με μικρότερο μέγεθος.");

                    //Create a copy
                    string randomIndex = RandomString(15);
                    string PDFNewFileName = "ari_foods_" + randomIndex + ".pdf";
                    using (Stream fileStream = new FileStream(Path.Combine(_environment.WebRootPath,"", "pdf-requests") + $@"\" + PDFNewFileName, FileMode.Create))
                    {
                        await requestDTO.iFormFile.CopyToAsync(fileStream);
                    }

                    //Upload to cloud
                    bool isUploaded = await ContainerService.UploadOperationAsync(Path.Combine(_environment.WebRootPath, "pdf-requests") + $@"\", PDFNewFileName, AzureGPV2Credentials.CONTAINER_REQUESTS_FILE);
                    if (!isUploaded)
                        return AuthenticationError(View(requestDTO), string.Empty, "Προέκυψε πρόβλημα με το συνημμένο αρχείο");
                    
                    filePath = Path.Combine("https://arisa.blob.core.windows.net/requests/", PDFNewFileName);
                }

                if (string.IsNullOrEmpty(filePath))
                {
                    requestDTO = new RequestDTO { ComplaintSubjectListItems = LoadComplaintSubjects().Result.OrderBy(k => k.Text), SelectedComplaintSubject = "-" };
                    return AuthenticationError(View(requestDTO), string.Empty, "Προέκυψε πρόβλημα με το συνημμένο αρχείο");
                }
            }

            var complaint = new Complaint 
            {
                FullName = requestDTO.FullName,
                Email = requestDTO.Email,
                Street = requestDTO.Street,
                Telephone = requestDTO.Telephone,
                City = requestDTO.City,
                SubjectId = int.Parse(requestDTO.SelectedComplaintSubject),
                Description = requestDTO.Description,
                IssuedDateTime = DateTime.Now,
                Documents = filePath                
            };

            _context.Add(complaint);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(RequestAcceptance));
        }

        public ViewResult AuthResult(RequestDTO requestDTO, string message)
        {
            requestDTO = new RequestDTO { ComplaintSubjectListItems = LoadComplaintSubjects().Result.OrderBy(k => k.Text), SelectedComplaintSubject = "-" };
            return AuthenticationError(View(requestDTO), string.Empty, message);
        }

        public IActionResult RequestAcceptance()
        {
            return View();
        }

        private static Random random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        public async Task<List<SelectListItem>> LoadComplaintSubjects()
        {
            var complaintSubjects = await _context.ComplaintSubjects.ToListAsync();
            var complaintListItems = new List<SelectListItem>();
            foreach (var item in complaintSubjects)
                complaintListItems.Add(new SelectListItem { Text = item.Name, Value = item.ComplaintSubjectId.ToString() });
            return complaintListItems;
        }
        private ViewResult AuthenticationError(ViewResult viewResult, string key, string message)
        {
            ModelState.AddModelError(key, message);
            return viewResult;
        }
    }
}
*/
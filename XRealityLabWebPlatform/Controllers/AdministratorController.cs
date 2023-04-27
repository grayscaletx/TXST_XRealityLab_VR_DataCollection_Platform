using Application.Security;
using XRealityLab.Common;
using XRealityLab.Models;
using XRealityLab.PDFTemplates.Models;
using XRealityLab.Users.Queries;
using Common.Constants;
using Infrastructure.Entities;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using EdiTools;
using Microsoft.AspNetCore.Http;
using System.Web;
using System.Net.Http.Headers;
using DocumentFormat.OpenXml.EMMA;
using Newtonsoft.Json;
using EnumsNET;
using Microsoft.AspNetCore.Authorization;
using Common.Helpers;
using Microsoft.AspNetCore.Identity;
using Common;
using System.Drawing;
using Mail;

namespace XRealityLab.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class AdministratorController : Controller
    {
        private readonly XRealityLabdbContext _context;
        private IUserRepository _userRepository;
        private readonly IHostingEnvironment _environment;
        private readonly CustomIDataProtection _protector;
        private readonly UserManager<IdentityUser> _userManager;

        public AdministratorController(XRealityLabdbContext context, IHostingEnvironment IHostingEnvironment, CustomIDataProtection customIDataProtection,
            UserManager<IdentityUser> userManager)
        {
             _context = context;
            _userRepository = new UserRepository(_context);
            _environment = IHostingEnvironment;
            _userManager = userManager;
            _protector = customIDataProtection;
        }

        public async Task<IActionResult> Organizations(string _actionMessage)
        {
            List<OrganizationDTO> organizationDTOs = await _context.Organization.Include(k => k.Itdirector).Select(k => new OrganizationDTO
            {
                OrganizationId = k.OrganizationId,
                PassToken = _protector.Decode(k.OrganizationId.ToString()),
                FullName = k.FullName.Length < 25 ? k.FullName : string.Format("{0}...", k.FullName.Substring(0,25)),
                ITDirectorId = k.ItdirectorId,
                Address = string.Format("{0},{1},{2},{3},{4}",k.Street, k.City, k.State, k.Country, k.Zipcode),
                Color = k.Color,
                PhotoUrl = k.PhotoUrl,
                NoAllowedLabs = k.NoAllowedLabs,
                NotStarted = k.SubscriptionStarted > DateTime.Now,
                Expired = k.SubscriptionExpiration < DateTime.Now,
                Registered = k.Registered,
                SubscriptionStarted = k.SubscriptionStarted,
                SubscriptionExpiration = k.SubscriptionExpiration,
                ITDirector = new UserDTO
                {
                    UserId = k.Itdirector.UserId,
                    Rank = k.Itdirector.Rank,
                    Firstname = k.Itdirector.Firstname,
                    Lastname = k.Itdirector.Lastname,
                    Email = k.Itdirector.Email,
                    Telephone = k.Itdirector.Telephone,
                    PhotoUrl = k.Itdirector.PhotoUrl,
                    RoleId = k.Itdirector.RoleId,
                    IsActive = k.Itdirector.IsActive == 1 ? true : false,
                    RegistrationDateTime = k.Itdirector.RegistrationDateTime
                }
            }).ToListAsync();

            ViewBag.actionMessage = _actionMessage;
            return View(organizationDTOs);
        }
        public IActionResult AddOrganization(string actionMessage) 
        {
            ViewBag.actionMessage = actionMessage;
            return View(); 
        }
        [HttpPost]
        public async Task<IActionResult> AddOrganization(OrganizationDirectorDTO organizationDirectorDTO)
        {
            if (!ModelState.IsValid) return RedirectToAction(nameof(AddOrganization), new { actionMessage = "Didnt fill the form correctly" });

            UserDTO userDTO = new UserDTO
            {
                Rank = organizationDirectorDTO.Rank,
                Firstname = organizationDirectorDTO.Firstname,
                Lastname = organizationDirectorDTO.Lastname,
                Email = organizationDirectorDTO.Email,
                Password =organizationDirectorDTO.Password,
                RoleId = (int)RoleType.DirectorOfIT,
                PhotoUrl = PhotoUrl.DIRECTOR_OF_IT_ICON_PNG,
                Telephone = organizationDirectorDTO.Telephone,
                RegistrationDateTime = DateTime.Now,
                IsActive = true
            };

            ResponseDTO responseDTO = await DataValidation.UserValidation(_context, userDTO);
            if (responseDTO.Result == "Failed")
                return RedirectToAction(nameof(AddOrganization), new { actionMessage = responseDTO.Message });

            if (await _context.User.AnyAsync(k => k.Email == userDTO.Email.Trim()))
                return RedirectToAction(nameof(AddOrganization), new { actionMessage = "Failed to create the user" });

            var hasCreated = await IdentityMigration.CreateIdentityUserAsync(_userManager, userDTO.Email.Trim(), userDTO.Password);
            if (!hasCreated.Succeeded)
                return RedirectToAction(nameof(AddOrganization), new { actionMessage = "Couldn't create the user identity" });

            User user = await Migration.ToUser(_context, userDTO);

            if (user == null)
                return RedirectToAction(nameof(AddOrganization), new { actionMessage = "Couldn't create the user identity" });


            await Migration.InsertUserAsync(_context, user);
            if (_context.Organization.Where(k => k.Registered.AddDays(1) > DateTime.Now).Count() >= 10)
                return RedirectToAction(nameof(AddOrganization), new { actionMessage = "No new organizations can be added today" });


            //Get director id
            var directorIT = _context.User.Where(k => k.Email == organizationDirectorDTO.Email).FirstOrDefault();

            //Save data
            Organization organization = new Organization
            {
                FullName = organizationDirectorDTO.FullName,
                ItdirectorId = directorIT.UserId,
                Street = organizationDirectorDTO.Street,
                City = organizationDirectorDTO.City,
                State = organizationDirectorDTO.State,
                Zipcode = organizationDirectorDTO.ZIPCode,
                Color = organizationDirectorDTO.Color,
                NoAllowedLabs = organizationDirectorDTO.NoAllowedLabs,
                SubscriptionStarted = organizationDirectorDTO.SubscriptionStarted,
                SubscriptionExpiration = organizationDirectorDTO.SubscriptionExpiration,
                Registered = DateTime.Now,
                Country = organizationDirectorDTO.Country
            };
            responseDTO = await DataValidation.OrganizationValidation(_context, organization);
            if (responseDTO.Result == Enum.GetName(typeof(RoleType), MethodResult.Failed))
            {
                _context.Remove(directorIT);
                await _context.SaveChangesAsync();
                return Json(responseDTO);
            }

            //Upload icon
            if (!string.IsNullOrWhiteSpace(organizationDirectorDTO.PhotoBase64))
            {
                //Check if the file is jpg or png
                if (!(organizationDirectorDTO.PhotoBase64.StartsWith("data:image/png;base64,") || organizationDirectorDTO.PhotoBase64.StartsWith("data:image/jpeg;base64,")))
                    return RedirectToAction(nameof(Organizations), new { _actionMessage = "Invalid Data" });

                //CONVERT base64 to Image <-Library
                string base64 = organizationDirectorDTO.PhotoBase64.Substring(organizationDirectorDTO.PhotoBase64.IndexOf(',') + 1);
                base64 = base64.Trim('\0');
                byte[] chartData = Convert.FromBase64String(base64);
                System.Drawing.Image image;
                using (var ms = new MemoryStream(chartData, 0, chartData.Length))
                {
                    image = System.Drawing.Image.FromStream(ms);
                }
                image = new Bitmap(image, new Size(150, 150));
                string myUniqueFileName = Convert.ToString(Guid.NewGuid());
                string fileExtension = string.Empty;
                if (organizationDirectorDTO.PhotoBase64.StartsWith("data:image/png;base64,"))
                    fileExtension = ".png";
                else if (organizationDirectorDTO.PhotoBase64.StartsWith("data:image/jpeg;base64,"))
                    fileExtension = ".jpeg";
                string newFileName = myUniqueFileName + fileExtension;
                var fileName = Path.Combine(_environment.WebRootPath, "_images", "organization-photos") + $@"\{newFileName}";
                image.Save(fileName);

                bool isUploaded = await ContainerService.UploadOperationAsync(Path.Combine(_environment.WebRootPath, "_images", "organization-photos"),
                    newFileName, AzureGPV2Credentials.CONTAINER_ORGANIZATION_ICONS);

                if (isUploaded)
                    organization.PhotoUrl = Path.Combine(BaseLink.AZURE_BLOB_URL, AzureGPV2Credentials.CONTAINER_ORGANIZATION_ICONS, newFileName);

                //DELETE from root
                if (System.IO.File.Exists(Path.Combine(_environment.WebRootPath, "_images", "organization-photos") + $@"\{newFileName}"))
                    System.IO.File.Delete(Path.Combine(_environment.WebRootPath, "_images", "organization-photos") + $@"\{newFileName}");
            }
            else
            {
                organization.PhotoUrl = PhotoUrl.ORGANIZATION_ICON_PNG;
            }

            await _context.Organization.AddAsync(organization);
            await _context.SaveChangesAsync();

            //AUTOMATED EMAIL FOR USER
            //generation of the email tokens
            string password = organizationDirectorDTO.Password;
            string organizationName = organizationDirectorDTO.FullName;

            //Send email
            MailService mailService = new MailService();
            bool check = await mailService.SendAccountCreationAsync(directorIT.Firstname, password, organizationName, directorIT.Email);

            return RedirectToAction("Organizations", "Administrator");
        }
        public IActionResult EditOrganization(string passToken,  string _actionMessage)
        {
            OrganizationDirectorDTO organizationDirectorDTO = _context.Organization
                .Where(k=>k.OrganizationId == int.Parse(_protector.Encode(passToken))).Select(k => new OrganizationDirectorDTO
            {
                FullName = k.FullName,
                PassToken = _protector.Decode(k.OrganizationId.ToString()),
                UserId = k.ItdirectorId,
                Street = k.Street,
                City = k.City,
                State = k.State,
                Country = k.Country,
                ZIPCode = k.Zipcode,
                Color = k.Color,
                PhotoUrl = k.PhotoUrl,
                NoAllowedLabs = k.NoAllowedLabs,
                SubscriptionStarted = k.SubscriptionStarted,
                SubscriptionExpiration = k.SubscriptionExpiration,
                Registered = DateTime.Now,
            }).FirstOrDefault();

            var directorIT = _context.User.Where(k => k.UserId == organizationDirectorDTO.UserId).FirstOrDefault();

            organizationDirectorDTO.Rank = directorIT.Rank;
            organizationDirectorDTO.Firstname = directorIT.Firstname;
            organizationDirectorDTO.Lastname = directorIT.Lastname;
            organizationDirectorDTO.Email = directorIT.Email;
            organizationDirectorDTO.Telephone = directorIT.Telephone;
            organizationDirectorDTO.UserPhotoUrl = directorIT.PhotoUrl;
            organizationDirectorDTO.IsActive = directorIT.IsActive == 1? true : false;
            organizationDirectorDTO.RegistrationDateTime = directorIT.RegistrationDateTime;

            ViewBag.actionMessage = _actionMessage;
            return View(organizationDirectorDTO);
        }
        [HttpPost]
        public async Task<IActionResult> EditOrganization(OrganizationDirectorDTO organizationDirectorDTO)
        {
            if (!ModelState.IsValid) return RedirectToAction(nameof(EditOrganization), new { passToken = organizationDirectorDTO.PassToken, _actionMessage = "Invalid Data" });

            //Get director id
            var directorIT = _context.User.Where(k => k.Email == organizationDirectorDTO.Email).FirstOrDefault();

            Organization organization = _context.Organization.Where(k => k.OrganizationId == int.Parse(_protector.Encode(organizationDirectorDTO.PassToken))).FirstOrDefault();
            if (_context.OrganizationLab.Where(k => k.OrganizationId == organization.OrganizationId).Count() > organizationDirectorDTO.NoAllowedLabs)
                return RedirectToAction(nameof(EditOrganization), new { passToken =organizationDirectorDTO.PassToken, _actionMessage = "No of Allowed Labs Invalid" });

            //Upload icon
            if (!string.IsNullOrWhiteSpace(organizationDirectorDTO.PhotoBase64))
            {
                //Check if the file is jpg or png
                if (!(organizationDirectorDTO.PhotoBase64.StartsWith("data:image/png;base64,") || organizationDirectorDTO.PhotoBase64.StartsWith("data:image/jpeg;base64,")))
                    return RedirectToAction(nameof(EditOrganization), new { passToken = organizationDirectorDTO.PassToken, _actionMessage = "Invalid Data" });

                //CONVERT base64 to Image <-Library
                string base64 = organizationDirectorDTO.PhotoBase64.Substring(organizationDirectorDTO.PhotoBase64.IndexOf(',') + 1);
                base64 = base64.Trim('\0');
                byte[] chartData = Convert.FromBase64String(base64);
                System.Drawing.Image image;
                using (var ms = new MemoryStream(chartData, 0, chartData.Length))
                {
                    image = System.Drawing.Image.FromStream(ms);
                }
                image = new Bitmap(image, new Size(150, 150));
                string myUniqueFileName = Convert.ToString(Guid.NewGuid());
                string fileExtension = string.Empty;
                if (organizationDirectorDTO.PhotoBase64.StartsWith("data:image/png;base64,"))
                    fileExtension = ".png";
                else if (organizationDirectorDTO.PhotoBase64.StartsWith("data:image/jpeg;base64,"))
                    fileExtension = ".jpeg";
                string newFileName = myUniqueFileName + fileExtension;
                var fileName = Path.Combine(_environment.WebRootPath, "_images", "organization-photos") + $@"\{newFileName}";
                image.Save(fileName);

                bool isUploaded = await ContainerService.UploadOperationAsync(Path.Combine(_environment.WebRootPath, "_images", "organization-photos"),
                    newFileName, AzureGPV2Credentials.CONTAINER_ORGANIZATION_ICONS);

                if (isUploaded)
                    organization.PhotoUrl = Path.Combine(BaseLink.AZURE_BLOB_URL, AzureGPV2Credentials.CONTAINER_ORGANIZATION_ICONS, newFileName);

                //DELETE from root
                if (System.IO.File.Exists(Path.Combine(_environment.WebRootPath, "_images", "organization-photos") + $@"\{newFileName}"))
                    System.IO.File.Delete(Path.Combine(_environment.WebRootPath, "_images", "organization-photos") + $@"\{newFileName}");
            }

            //Update organization data
            organization.OrganizationId = int.Parse(_protector.Encode(organizationDirectorDTO.PassToken));
            organization.FullName = organizationDirectorDTO.FullName;
            organization.ItdirectorId = directorIT.UserId;
            organization.Street = organizationDirectorDTO.Street;
            organization.City = organizationDirectorDTO.City;
            organization.State = organizationDirectorDTO.State;
            organization.Zipcode = organizationDirectorDTO.ZIPCode;
            organization.Color = organizationDirectorDTO.Color;
            organization.NoAllowedLabs = organizationDirectorDTO.NoAllowedLabs;
            organization.SubscriptionStarted = organizationDirectorDTO.SubscriptionStarted;
            organization.SubscriptionExpiration = organizationDirectorDTO.SubscriptionExpiration;
            organization.Registered = DateTime.Now;
            organization.Country = organizationDirectorDTO.Country;
            
            ResponseDTO responseDTO = await DataValidation.OrganizationValidation(_context, organization);
            if (responseDTO.Result == Enum.GetName(typeof(RoleType), MethodResult.Failed))
                return Json(responseDTO);

            _context.Organization.Update(organization);

            //Update user data
            directorIT.Rank = organizationDirectorDTO.Rank;
            directorIT.Firstname = organizationDirectorDTO.Firstname;
            directorIT.Lastname = organizationDirectorDTO.Lastname;
            directorIT.Telephone = organizationDirectorDTO.Telephone;

            _context.User.Update(directorIT);
            await _context.SaveChangesAsync();

            return RedirectToAction("Organizations", "Administrator");
        }
        public async Task<IActionResult> RemoveOrganization(string passToken)
        {
            Organization organization = await _context.Organization.Where(k => k.OrganizationId == int.Parse(_protector.Encode(passToken))).FirstOrDefaultAsync();
            await Migration.RemoveOrganizationDirectorAsync(_context, _userManager, organization);

            return RedirectToAction(nameof(Organizations));
        }

        public IActionResult ChangeITDirector(string passToken, string _actionMessage) 
        {
            OrganizationDirectorDTO organizationDirectorDTO = _context.Organization
                .Where(k => k.OrganizationId == int.Parse(_protector.Encode(passToken))).Select(k => new OrganizationDirectorDTO
                {
                    FullName = k.FullName,
                    PassToken = _protector.Decode(k.OrganizationId.ToString()),
                    UserId = k.ItdirectorId,
                    Street = k.Street,
                    City = k.City,
                    State = k.State,
                    Country = k.Country,
                    ZIPCode = k.Zipcode,
                    Color = k.Color,
                    PhotoUrl = k.PhotoUrl,
                    NoAllowedLabs = k.NoAllowedLabs,
                    SubscriptionStarted = k.SubscriptionStarted,
                    SubscriptionExpiration = k.SubscriptionExpiration,
                    Registered = DateTime.Now,
                }).FirstOrDefault();

            var directorIT = _context.User.Where(k => k.UserId == organizationDirectorDTO.UserId).FirstOrDefault();

            organizationDirectorDTO.Rank = directorIT.Rank;
            organizationDirectorDTO.Firstname = directorIT.Firstname;
            organizationDirectorDTO.Lastname = directorIT.Lastname;
            organizationDirectorDTO.Email = directorIT.Email;
            organizationDirectorDTO.Telephone = directorIT.Telephone;
            organizationDirectorDTO.UserPhotoUrl = directorIT.PhotoUrl;
            organizationDirectorDTO.IsActive = directorIT.IsActive == 1 ? true : false;
            organizationDirectorDTO.RegistrationDateTime = directorIT.RegistrationDateTime;

            ViewBag.actionMessage = _actionMessage;
            return View(organizationDirectorDTO); 
        }
        [HttpPost]
        public async Task<IActionResult> ChangeITDirector(OrganizationDirectorDTO organizationDirectorDTO)
        {
            //Data validation
            if (!ModelState.IsValid) return RedirectToAction(nameof(ChangeITDirector), new { passToken = organizationDirectorDTO.PassToken , _actionMessage = "Invalid Data" });

            UserDTO userDTO = new UserDTO
            {
                Rank = organizationDirectorDTO.Rank,
                Firstname = organizationDirectorDTO.Firstname,
                Lastname = organizationDirectorDTO.Lastname,
                Email = organizationDirectorDTO.Email,
                Password = organizationDirectorDTO.Password,
                RoleId = (int)RoleType.DirectorOfIT,
                PhotoUrl = PhotoUrl.DIRECTOR_OF_IT_ICON_PNG,
                Telephone = organizationDirectorDTO.Telephone,
                RegistrationDateTime = DateTime.Now,
                IsActive = true
            };
            ResponseDTO responseDTO = await DataValidation.UserValidation(_context, userDTO);
            if (responseDTO.Result == "Failed")
                return RedirectToAction(nameof(ChangeITDirector), new { passToken = organizationDirectorDTO.PassToken, _actionMessage = responseDTO.Message });

            if (await _context.User.AnyAsync(k => k.Email == userDTO.Email.Trim()))
                return RedirectToAction(nameof(ChangeITDirector), new { passToken = organizationDirectorDTO.PassToken, _actionMessage = "Failed to create user" });

            var hasCreated = await IdentityMigration.CreateIdentityUserAsync(_userManager, userDTO.Email.Trim(), userDTO.Password);
            if (!hasCreated.Succeeded)
                return RedirectToAction(nameof(ChangeITDirector), new { passToken = organizationDirectorDTO.PassToken, _actionMessage = "Couldn't create user identity" });

            User user = await Migration.ToUser(_context, userDTO);

            if (user == null)
                return RedirectToAction(nameof(ChangeITDirector), new { passToken = organizationDirectorDTO.PassToken, _actionMessage = "Failed to create user" });

            await Migration.InsertUserAsync(_context, user);
            if (_context.Organization.Where(k => k.Registered.AddDays(1) > DateTime.Now).Count() >= 10)
                return RedirectToAction(nameof(ChangeITDirector), new { passToken = organizationDirectorDTO.PassToken, _actionMessage = "no new organization can be added today" });

            //Get new director id
            var newDirectorIT = _context.User.Where(k => k.Email == organizationDirectorDTO.Email).FirstOrDefault();
            //Get organization
            Organization organization = _context.Organization.Where(k => k.OrganizationId == int.Parse(_protector.Encode(organizationDirectorDTO.PassToken))).FirstOrDefault();
            //find old director
            User oldDirectorIT = _context.User.Where(k => k.UserId == organization.ItdirectorId).FirstOrDefault();
            //Change director id of organization
            organization.ItdirectorId = newDirectorIT.UserId;
            _context.Organization.Update(organization);
            //Delete old director
            _context.User.Remove(oldDirectorIT);

            await _context.SaveChangesAsync();

            return RedirectToAction("Organizations", "Administrator");
        }
        //Init Data
        [HttpGet]
        public JsonResult GetPhotoUrl()
        {
            return new JsonResult(_context.User.Where(k => k.Email == User.Identity.Name).FirstOrDefault().PhotoUrl);
        }
    }
}
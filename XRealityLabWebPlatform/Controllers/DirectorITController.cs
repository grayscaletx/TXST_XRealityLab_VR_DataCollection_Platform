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
using System.Security.Claims;
using System.Drawing;

namespace XRealityLab.Controllers
{
    [Authorize(Roles = "DirectorIT")]
    public class DirectorITController : Controller
    {
        private readonly XRealityLabdbContext _context;
        private IUserRepository _userRepository;
        private readonly IHostingEnvironment _environment;
        private readonly CustomIDataProtection _protector;
        private readonly UserManager<IdentityUser> _userManager;

        public DirectorITController(XRealityLabdbContext context, IHostingEnvironment IHostingEnvironment, CustomIDataProtection customIDataProtection,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userRepository = new UserRepository(_context);
            _environment = IHostingEnvironment;
            _userManager = userManager;
            _protector = customIDataProtection;
        }

        public async Task<IActionResult> Labs(string actionMessage)
        {      
            Organization organization = _context.Organization.Where(k => k.ItdirectorId == (_context.User.Where(k => k.Email == User.Identity.Name).FirstOrDefault().UserId)).FirstOrDefault();
            
            List<LabDTO> labDTOs = await _context.Lab.Include(k => k.OrganizationLab).Where(k => k.OrganizationLab.Select(k => k.OrganizationId).Contains(organization.OrganizationId)).Include(k => k.LabDirector).Select(k => new LabDTO
            {
                LabId = k.LabId,
                PassToken = _protector.Decode(k.LabId.ToString()),
                FullName = k.FullName.Length < 25 ? k.FullName : k.FullName.Substring(0, 25) + "...",
                LabDirectorId = k.LabDirectorId,
                Color = k.Color,
                PhotoUrl = k.PhotoUrl,
                Registered = k.Registered,
                LabDirector = new UserDTO
                {
                    UserId = k.LabDirector.UserId,
                    Rank = k.LabDirector.Rank,
                    Firstname = k.LabDirector.Firstname,
                    Lastname = k.LabDirector.Lastname,
                    Email = k.LabDirector.Email,
                    Telephone = k.LabDirector.Telephone,
                    PhotoUrl = k.LabDirector.PhotoUrl,
                    RoleId = k.LabDirector.RoleId,
                    IsActive = k.LabDirector.IsActive == 1 ? true : false,
                    RegistrationDateTime = k.LabDirector.RegistrationDateTime
                }
            }).ToListAsync();
            OrganizationDTO organizationDTO = new OrganizationDTO
            {
                FullName = organization.FullName,
                NoAllowedLabs = organization.NoAllowedLabs,
                LabDTOs = labDTOs
            };
            return View(organizationDTO);
        }
        public IActionResult AddLab() { return View(); }
        [HttpPost]
        public async Task<IActionResult> AddLab(LabDirectorDTO labDirectorDTO)
        {
            if (_context.OrganizationLab.Where(k => k.OrganizationId == _context.Organization.Where(k => k.ItdirectorId == (_context.User.Where(k => k.Email == User.Identity.Name).FirstOrDefault().UserId)).FirstOrDefault().OrganizationId).Count() >= _context.Organization.Where(k => k.ItdirectorId == (_context.User.Where(k => k.Email == User.Identity.Name).FirstOrDefault().UserId)).FirstOrDefault().NoAllowedLabs)
                return RedirectToAction(nameof(Labs), new { _actionMessage = "Please Refrain From Hacking Me" });

            //Data validation
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(AddLab), new { _actionMessage = "Δεν προσθέσατε τα πεδία σωστά. Παρακαλώ προσπαθήστε ξανά." });
            }
            UserDTO userDTO = new UserDTO
            {
                Rank = labDirectorDTO.Rank,
                Firstname = labDirectorDTO.Firstname,
                Lastname = labDirectorDTO.Lastname,
                Email = labDirectorDTO.Email,
                Password = "test123",
                RoleId = (int)RoleType.LabDirector,
                PhotoUrl = PhotoUrl.DIRECTOR_OF_IT_ICON_PNG,
                Telephone = "698132154",
                RegistrationDateTime = DateTime.Now,
                IsActive = true
            };
            ResponseDTO responseDTO = await DataValidation.UserValidation(_context, userDTO);
            if (responseDTO.Result == "Failed")
                return Json(responseDTO);

            if (await _context.User.AnyAsync(k => k.Email == userDTO.Email.Trim()))
                return Json(MessageConverter.GetResponse("Failed to create the user", MethodResult.Failed));

            var hasCreated = await IdentityMigration.CreateIdentityUserAsync(_userManager, userDTO.Email.Trim(), userDTO.Password);
            if (!hasCreated.Succeeded)
                return Json(MessageConverter.GetResponse("Couldn't create the user identity", MethodResult.Failed));

            User user = await Migration.ToUser(_context, userDTO);

            if (user == null)
                return Json(MessageConverter.GetResponse("Failed to create the user", MethodResult.Failed));

            await Migration.InsertUserAsync(_context, user);
            if (_context.Lab.Where(k => k.Registered.AddDays(1) > DateTime.Now).Count() >= 10)
                return Json(MessageConverter.GetResponse("No new labs can be added today", MethodResult.Failed));

            //Get director id
            var director = _context.User.Where(k => k.Email == labDirectorDTO.Email).FirstOrDefault();
            int organizationId = _context.Organization.Where(k => k.ItdirectorId == (_context.User.Where(k => k.Email == User.Identity.Name).FirstOrDefault().UserId)).FirstOrDefault().OrganizationId;
            
            //Save data
            Lab lab = new Lab
            {
                FullName = labDirectorDTO.FullName,
                LabDirectorId = director.UserId,
                Description = labDirectorDTO.Description,
                Color = "#6a5738",
                PhotoUrl = PhotoUrl.LAB_ICON_PNG,
                Registered = DateTime.Now
            };
            responseDTO = await DataValidation.LabValidation(_context, lab);
            if (responseDTO.Result == Enum.GetName(typeof(RoleType), MethodResult.Failed))
            {
                _context.User.Remove(director);
                await _context.SaveChangesAsync();
                return Json(responseDTO);
            }                
            await _context.Lab.AddAsync(lab);
            //Get LabId
            await _context.SaveChangesAsync();
            Lab thisLab = _context.Lab.Where(k => k.FullName == lab.FullName).FirstOrDefault();
            OrganizationLab organizationLab = new OrganizationLab
            {
                OrganizationId = organizationId,                
                LabId = thisLab.LabId
            };
            LabUser labUser = new LabUser
            {
                LabId = thisLab.LabId,
                UserId = director.UserId,
                RoleId = 3
            };
            await _context.LabUser.AddAsync(labUser);
            await _context.OrganizationLab.AddAsync(organizationLab);
            await _context.SaveChangesAsync();


            return RedirectToAction("Labs", RoleType.DirectorOfIT.GetEnumDescription());
        }
        public IActionResult EditLab(string passToken)
        {
            LabDirectorDTO labDirectorDTO = _context.Lab
                .Where(k => k.LabId == int.Parse(_protector.Encode(passToken))).Select(k => new LabDirectorDTO
                {
                    FullName = k.FullName,
                    PassToken = _protector.Decode(k.LabId.ToString()),
                    UserId = k.LabDirectorId,
                    Description = k.Description,
                    Color = k.Color,
                    PhotoUrl = k.PhotoUrl,
                    Registered = DateTime.Now,
                }).FirstOrDefault();

            var director = _context.User.Where(k => k.UserId == labDirectorDTO.UserId).FirstOrDefault();

            labDirectorDTO.Rank = director.Rank;
            labDirectorDTO.Firstname = director.Firstname;
            labDirectorDTO.Lastname = director.Lastname;
            labDirectorDTO.Email = director.Email;
            labDirectorDTO.Telephone = director.Telephone;
            labDirectorDTO.UserPhotoUrl = director.PhotoUrl;
            labDirectorDTO.IsActive = director.IsActive == 1 ? true : false;
            labDirectorDTO.RegistrationDateTime = director.RegistrationDateTime;

            return View(labDirectorDTO);
        }
        [HttpPost]
        public async Task<IActionResult> EditLab(LabDirectorDTO labDirectorDTO)
        {
            //Data validation
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(AddLab), new { _actionMessage = "Δεν προσθέσατε τα πεδία σωστά. Παρακαλώ προσπαθήστε ξανά." });
            }

            //Get director id
            var director = _context.User.Where(k => k.Email == labDirectorDTO.Email).FirstOrDefault();

            //Save data
            Lab lab = _context.Lab.Where(k => k.LabId == int.Parse(_protector.Encode(labDirectorDTO.PassToken))).FirstOrDefault();

            //Upload icon
            if (!string.IsNullOrWhiteSpace(labDirectorDTO.PhotoBase64))
            {
                //Check if the file is jpg or png
                if (!(labDirectorDTO.PhotoBase64.StartsWith("data:image/png;base64,") || labDirectorDTO.PhotoBase64.StartsWith("data:image/jpeg;base64,")))
                {
                    return RedirectToAction(nameof(Labs), new { _actionMessage = "Invalid Data" });
                }
                //CONVERT base64 to Image <-Library
                string base64 = labDirectorDTO.PhotoBase64.Substring(labDirectorDTO.PhotoBase64.IndexOf(',') + 1);
                base64 = base64.Trim('\0');
                byte[] chartData = Convert.FromBase64String(base64);
                System.Drawing.Image image;
                using (var ms = new MemoryStream(chartData, 0, chartData.Length))
                {
                    image = System.Drawing.Image.FromStream(ms);
                }
                image = new Bitmap(image, new Size(150, 150));
                string myUniqueFileName = Convert.ToString(Guid.NewGuid());
                string fileExtension = "";
                if (labDirectorDTO.PhotoBase64.StartsWith("data:image/png;base64,"))
                {
                    fileExtension = ".png";
                }
                else if (labDirectorDTO.PhotoBase64.StartsWith("data:image/jpeg;base64,"))
                {
                    fileExtension = ".jpeg";
                }
                string newFileName = myUniqueFileName + fileExtension;
                var fileName = string.Empty;
                fileName = Path.Combine(_environment.WebRootPath, "_images", "lab-photos") + $@"\{newFileName}";
                image.Save(fileName);

                bool isUploaded = await ContainerService.UploadOperationAsync(Path.Combine(_environment.WebRootPath, "_images", "lab-photos"),
                    newFileName, AzureGPV2Credentials.CONTAINER_LAB_ICONS);

                if (isUploaded)
                {
                    lab.PhotoUrl = Path.Combine(BaseLink.AZURE_BLOB_URL, AzureGPV2Credentials.CONTAINER_LAB_ICONS, newFileName);
                }

                //DELETE from root
                if (System.IO.File.Exists(Path.Combine(_environment.WebRootPath, "_images", "lab-photos") + $@"\{newFileName}"))
                {
                    System.IO.File.Delete(Path.Combine(_environment.WebRootPath, "_images", "lab-photos") + $@"\{newFileName}");
                }



            }



            lab.LabId = int.Parse(_protector.Encode(labDirectorDTO.PassToken));
            lab.FullName = labDirectorDTO.FullName;
            lab.LabDirectorId = director.UserId;
            lab.Description = labDirectorDTO.Description;
            lab.Color = labDirectorDTO.Color;
            lab.PhotoUrl = PhotoUrl.LAB_ICON_PNG;
            lab.Registered = DateTime.Now;
            ResponseDTO responseDTO = await DataValidation.LabValidation(_context, lab);
            if (responseDTO.Result == Enum.GetName(typeof(RoleType), MethodResult.Failed))
                return Json(responseDTO);

            _context.Lab.Update(lab);

            //Update user data
            director.Rank = labDirectorDTO.Rank;
            director.Firstname = labDirectorDTO.Firstname;
            director.Lastname = labDirectorDTO.Lastname;
            director.Telephone = labDirectorDTO.Telephone;

            _context.User.Update(director);
            await _context.SaveChangesAsync();

            return RedirectToAction("Labs", RoleType.DirectorOfIT.GetEnumDescription());
        }
        public async Task<IActionResult> RemoveLab(string passToken)
        {
            Lab lab = await _context.Lab.Where(k => k.LabId == int.Parse(_protector.Encode(passToken))).FirstOrDefaultAsync();
            await Migration.RemoveLabDirectorAsync(_context, _userManager, lab);

            return RedirectToAction(nameof(Labs));
        }
        public IActionResult ChangeLabDirector(string passToken/*LabPassToken*/)
        {
            LabDirectorDTO labDirectorDTO = _context.Lab
                .Where(k => k.LabId == int.Parse(_protector.Encode(passToken))).Select(k => new LabDirectorDTO
                {
                    FullName = k.FullName,
                    PassToken = _protector.Decode(k.LabId.ToString()),
                    UserId = k.LabDirectorId,
                    Description = k.Description,
                    Color = k.Color,
                    PhotoUrl = k.PhotoUrl,
                    Registered = DateTime.Now,
                }).FirstOrDefault();

            var director = _context.User.Where(k => k.UserId == labDirectorDTO.UserId).FirstOrDefault();

            labDirectorDTO.Rank = director.Rank;
            labDirectorDTO.Firstname = director.Firstname;
            labDirectorDTO.Lastname = director.Lastname;
            labDirectorDTO.Email = director.Email;
            labDirectorDTO.Telephone = director.Telephone;
            labDirectorDTO.UserPhotoUrl = director.PhotoUrl;
            labDirectorDTO.IsActive = director.IsActive == 1 ? true : false;
            labDirectorDTO.RegistrationDateTime = director.RegistrationDateTime;

            return View(labDirectorDTO);
        }
        [HttpPost]
        public async Task<IActionResult> ChangeLabDirector(LabDirectorDTO labDirectorDTO)
        {
            //Data validation
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(ChangeLabDirector), new { _actionMessage = "Δεν προσθέσατε τα πεδία σωστά. Παρακαλώ προσπαθήστε ξανά." });
            }

            //Upload icon
            if (!string.IsNullOrWhiteSpace(labDirectorDTO.PhotoBase64))
            {

            }

            UserDTO userDTO = new UserDTO
            {
                Rank = labDirectorDTO.Rank,
                Firstname = labDirectorDTO.Firstname,
                Lastname = labDirectorDTO.Lastname,
                Email = labDirectorDTO.Email,
                Password = "test123",
                RoleId = (int)RoleType.LabDirector,
                PhotoUrl = PhotoUrl.LAB_DIRECTOR_ICON_PNG,
                Telephone = labDirectorDTO.Telephone,
                RegistrationDateTime = DateTime.Now,
                IsActive = true
            };
            ResponseDTO responseDTO = await DataValidation.UserValidation(_context, userDTO);
            if (responseDTO.Result == "Failed")
                return Json(responseDTO);

            if (await _context.User.AnyAsync(k => k.Email == userDTO.Email.Trim()))
                return Json(MessageConverter.GetResponse("Failed to create the user", MethodResult.Failed));

            var hasCreated = await IdentityMigration.CreateIdentityUserAsync(_userManager, userDTO.Email.Trim(), userDTO.Password);
            if (!hasCreated.Succeeded)
                return Json(MessageConverter.GetResponse("Couldn't create the user identity", MethodResult.Failed));

            User user = await Migration.ToUser(_context, userDTO);

            if (user == null)
                return Json(MessageConverter.GetResponse("Failed to create the user", MethodResult.Failed));

            await Migration.InsertUserAsync(_context, user);
            if (_context.Organization.Where(k => k.Registered.AddDays(1) > DateTime.Now).Count() >= 10)
                return Json(MessageConverter.GetResponse("No new organizations can be added today", MethodResult.Failed));

            //Get new director id
            var newLabDirector = _context.User.Where(k => k.Email == labDirectorDTO.Email).FirstOrDefault();
            //Get Lab
            Lab lab = _context.Lab.Where(k => k.LabId == int.Parse(_protector.Encode(labDirectorDTO.PassToken))).FirstOrDefault();
            //find old director and change role from LabDirector to to PrincipleInvestigator
            User oldLabDirector = _context.User.Where(k => k.UserId == _context.LabUser.Where(x => x.LabId == lab.LabId && x.RoleId == (int)RoleType.LabDirector).FirstOrDefault().UserId).FirstOrDefault();
            oldLabDirector.RoleId = (int)RoleType.PrincipleInvestigator;
            _context.User.Update(oldLabDirector);
            //Change old LabUser from LabDirector to PrincipleInvestigator
            LabUser oldLabUser = _context.LabUser.Where(k => k.UserId == oldLabDirector.UserId && k.RoleId == (int)RoleType.LabDirector).FirstOrDefault();
            oldLabUser.RoleId = (int)RoleType.PrincipleInvestigator;
            _context.LabUser.Update(oldLabUser);
            //Create new LabUser
            LabUser newLabUser = new LabUser
            {
                LabId = lab.LabId,
                UserId = newLabDirector.UserId,
                RoleId = (int)RoleType.LabDirector
            };
            _context.LabUser.Add(newLabUser);
            //Change labDirector of ProjectUser
            List<ProjectUser> projectUsers = _context.ProjectUser.Where(k => k.UserId == oldLabDirector.UserId && k.RoleId == (int)RoleType.LabDirector).ToList();
            foreach (var projectUser in projectUsers)
            {
                projectUser.UserId = newLabDirector.UserId;
            }
            _context.ProjectUser.UpdateRange(projectUsers);
            //Change LabDirector of Lab
            lab.LabDirectorId = newLabDirector.UserId;
            _context.Lab.Update(lab);

            await _context.SaveChangesAsync();

            return RedirectToAction("Labs", "DirectorIT");
        }
    }
}
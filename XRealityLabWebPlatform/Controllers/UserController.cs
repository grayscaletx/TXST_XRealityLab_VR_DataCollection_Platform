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
using System.Text.RegularExpressions;

namespace XRealityLab.Controllers
{
    //[Authorize(Roles = "LabDirector,PrincipleInvestigator")]
    public class UserController : Controller
    {
        private readonly XRealityLabdbContext _context;
        private IUserRepository _userRepository;
        private readonly IHostingEnvironment _environment;
        private readonly CustomIDataProtection _protector;
        private readonly UserManager<IdentityUser> _userManager;

        public UserController(XRealityLabdbContext context, IHostingEnvironment IHostingEnvironment, CustomIDataProtection customIDataProtection,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userRepository = new UserRepository(_context);
            _environment = IHostingEnvironment;
            _userManager = userManager;
            _protector = customIDataProtection;
        }
        public IActionResult EditProfile(string _actionMessage) 
        {
            User user = _context.User.Where(k => k.Email == User.Identity.Name).FirstOrDefault();
            UserDTO userDTO = new UserDTO
            {
                Rank = user.Rank,
                Firstname = user.Firstname,
                Lastname = user.Lastname,
                PassToken = _protector.Decode(user.UserId.ToString()),
                Telephone = user.Telephone,
                PhotoUrl = user.PhotoUrl
            };
            ViewBag.actionMessage = _actionMessage;
            return View(userDTO);
        }
        [HttpPost]
        public async Task<IActionResult> EditProfile(UserDTO _userDTO)
        {
            //Data validation
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(AddPrincipleInvestigator), new { _actionMessage = "Invalid Data" });
            }
            User user = _context.User.Where(k => k.UserId == int.Parse(_protector.Encode(_userDTO.PassToken))).FirstOrDefault();
            _userDTO.Email = user.Email;
            ResponseDTO responseDTO = await DataValidation.UserValidation(_context, _userDTO);
            if (responseDTO.Result == "Failed")
                return RedirectToAction(nameof(EditProfile), new { _actionMessage = "Invalid Data" });


            //Upload icon
            if (!string.IsNullOrWhiteSpace(_userDTO.PhotoBase64))
            {
                //Check if the file is jpg or png
                if (!(_userDTO.PhotoBase64.StartsWith("data:image/png;base64,") || _userDTO.PhotoBase64.StartsWith("data:image/jpeg;base64,")))
                {
                    return RedirectToAction(nameof(EditProfile), new { _actionMessage = "Invalid Data" });
                }
                //CONVERT base64 to Image <-Library
                string base64 = _userDTO.PhotoBase64.Substring(_userDTO.PhotoBase64.IndexOf(',') + 1);
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
                if (_userDTO.PhotoBase64.StartsWith("data:image/png;base64,"))
                {
                    fileExtension = ".png";
                }
                else if (_userDTO.PhotoBase64.StartsWith("data:image/jpeg;base64,"))
                {
                    fileExtension = ".jpeg";
                }
                string newFileName = myUniqueFileName + fileExtension;
                var fileName = string.Empty;
                fileName = Path.Combine(_environment.WebRootPath, "_images", "user-photos") + $@"\{newFileName}";
                image.Save(fileName);

                bool isUploaded = await ContainerService.UploadOperationAsync(Path.Combine(_environment.WebRootPath, "_images", "user-photos"),
                    newFileName, AzureGPV2Credentials.CONTAINER_USER_ICONS);

                if (isUploaded)
                {
                    user.PhotoUrl = Path.Combine(BaseLink.AZURE_BLOB_URL, AzureGPV2Credentials.CONTAINER_USER_ICONS, newFileName);
                }

                //DELETE from root
                if (System.IO.File.Exists(Path.Combine(_environment.WebRootPath, "_images", "user-photos") + $@"\{newFileName}"))
                {
                    System.IO.File.Delete(Path.Combine(_environment.WebRootPath, "_images", "user-photos") + $@"\{newFileName}");
                }
            }

            if (!Regex.IsMatch(_userDTO.Firstname, @"^[a-zA-Z]+$") || !Regex.IsMatch(_userDTO.Lastname, @"^[a-zA-Z]+$"))
                return RedirectToAction(nameof(EditProfile), new { _actionMessage = "Invalid Data" });

            user.Rank = _userDTO.Rank;
            user.Firstname = _userDTO.Firstname;
            user.Lastname = _userDTO.Lastname;
            user.Telephone = _userDTO.Telephone;
            //user.PasswordHash = IdentityConversions.MD5Hash(_userDTO.Password);
            _context.User.Update(user);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(EditProfile), new { _actionMessage = "Success" });
        }
        public IActionResult ChangePassword(string _actionMessage)
        {
            ViewBag.actionMessage = _actionMessage;
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> ChangePassword(PasswordDTO passwordDTO)
        {
            User user = _context.User.Where(k => k.Email == User.Identity.Name).FirstOrDefault();
            //Check Old Password if correct            
            if(user.PasswordHash != IdentityConversions.MD5Hash(passwordDTO.OldPassword))
                return RedirectToAction(nameof(ChangePassword), new { _actionMessage = "Old Password incorect" });
            //Check if new passwords match
            if(passwordDTO.NewPassword != passwordDTO.NewPasswordConfirm)
                return RedirectToAction(nameof(ChangePassword), new { _actionMessage = "New Passwords dont match" });
            //Update passwords
            user.PasswordHash = IdentityConversions.MD5Hash(passwordDTO.NewPassword);
            _context.User.Update(user);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(EditProfile), new { _actionMessage = "Success" });
        }
        public async Task<IActionResult> PrincipleInvestigators(string _actionMessage)
        {
            User user = _context.User.Where(k => k.Email == User.Identity.Name).FirstOrDefault();
            int labId = _context.LabUser.Where(k => k.UserId == user.UserId).FirstOrDefault().LabId;
            List<int> userIds = await _context.LabUser.Where(k => k.LabId == labId && (k.RoleId == 3 || k.RoleId == 4)).Select(k => k.UserId).ToListAsync();
            List<UserDTO> userDTOs = new List<UserDTO>();
            foreach (int userId in userIds)
            {
                User principleInvestigator = _context.User.Where(k => k.UserId == userId).FirstOrDefault();
                userDTOs.Add(new UserDTO{
                    Rank = principleInvestigator.Rank,
                    Firstname = principleInvestigator.Firstname,
                    Lastname = principleInvestigator.Lastname,
                    PassToken = _protector.Decode(principleInvestigator.UserId.ToString()),
                    PhotoUrl = principleInvestigator.PhotoUrl,
                    Email = principleInvestigator.Email,
                    Telephone = principleInvestigator.Telephone,
                    RegistrationDateTime = principleInvestigator.RegistrationDateTime
                });
            }

            ViewBag.actionMessage = _actionMessage;
            return View(userDTOs);
        }
        public IActionResult AddPrincipleInvestigator(string _actionMessage) { ViewBag.actionMessage = _actionMessage; return View(); }
        [HttpPost]
        public async Task<IActionResult> AddPrincipleInvestigator(UserDTO _userDTO)
        {
            int labId = _context.LabUser.Where(k => k.UserId == (_context.User.Where(k => k.Email == User.Identity.Name).FirstOrDefault().UserId)).FirstOrDefault().LabId;
            //Data validation
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(AddPrincipleInvestigator), new { _actionMessage = "Invalid Data" });
            }
            UserDTO userDTO = new UserDTO
            {
                Rank = _userDTO.Rank,
                Firstname = _userDTO.Firstname,
                Lastname = _userDTO.Lastname,
                Email = _userDTO.Email,
                Password = _userDTO.Password,
                RoleId = (int)RoleType.PrincipleInvestigator,
                PhotoUrl = PhotoUrl.DIRECTOR_OF_IT_ICON_PNG,
                Telephone = _userDTO.Telephone,
                RegistrationDateTime = DateTime.Now,
                IsActive = true
            };
            ResponseDTO responseDTO = await DataValidation.UserValidation(_context, userDTO);
            if (responseDTO.Result == "Failed")
                return RedirectToAction(nameof(AddPrincipleInvestigator), new { _actionMessage = responseDTO.Message });

            if (await _context.User.AnyAsync(k => k.Email == userDTO.Email.Trim()))
                return RedirectToAction(nameof(AddPrincipleInvestigator), new { _actionMessage = "Failed to create the user" });

            var hasCreated = await IdentityMigration.CreateIdentityUserAsync(_userManager, userDTO.Email.Trim(), userDTO.Password);
            if (!hasCreated.Succeeded)
                return RedirectToAction(nameof(AddPrincipleInvestigator), new { _actionMessage = "Couldn't create the user identity" });

            User user = await Migration.ToUser(_context, userDTO);

            if (user == null)
                return RedirectToAction(nameof(AddPrincipleInvestigator), new { _actionMessage = "Failed to create the user" });

            await Migration.InsertUserAsync(_context, user);

            LabUser labUser = new LabUser
            {
                UserId = user.UserId,
                LabId = labId,
                RoleId = 4
            };
            //Save data
            await _context.AddAsync(labUser);
            await _context.SaveChangesAsync();
            return RedirectToAction("PrincipleInvestigators", "User");
        }
        public async Task<IActionResult> RemovePrincipleInvestigator(string passToken)
        {
            User user = await _context.User.Where(k => k.UserId == int.Parse(_protector.Encode(passToken))).FirstOrDefaultAsync();
            await Migration.RemovePrincipleInvestigator(_context, user);
            return RedirectToAction(nameof(PrincipleInvestigators));
        }
        public async Task<IActionResult> AddPrincipleInvestigatorsToProject(string passToken) 
        {
            ProjectDTO projectDTO = _context.Project
                .Where(k => k.ProjectId == int.Parse(_protector.Encode(passToken))).Select(k => new ProjectDTO
                {
                    FullName = k.FullName,
                    PassToken = _protector.Decode(k.ProjectId.ToString()),
                    Description = k.Description,
                    Color = k.Color,
                    PhotoUrl = k.PhotoUrl,
                    Registered = DateTime.Now,
                }).FirstOrDefault();
            User user = _context.User.Where(k => k.Email == User.Identity.Name).FirstOrDefault();
            int labId = _context.LabUser.Where(k => k.UserId == user.UserId).FirstOrDefault().LabId;
            List<int> userIds = await _context.LabUser.Where(k => k.LabId == labId && (k.RoleId == 3 || k.RoleId == 4)).Select(k => k.UserId).ToListAsync();
            List<int> projectUserIds = await _context.ProjectUser.Where(k => k.ProjectId == int.Parse(_protector.Encode(passToken)) && k.RoleId == 4).Select(k => k.UserId).ToListAsync();
            List<UserDTO> userDTOs = new List<UserDTO>();
            foreach (int userId in userIds)
            {
                User principleInvestigator = _context.User.Where(k => k.UserId == userId).FirstOrDefault();
                userDTOs.Add(new UserDTO
                {
                    Firstname = principleInvestigator.Firstname,
                    Lastname = principleInvestigator.Lastname,
                    PassToken = _protector.Decode(principleInvestigator.UserId.ToString()),
                    Email = principleInvestigator.Email,
                    Telephone = principleInvestigator.Telephone,
                    RegistrationDateTime = principleInvestigator.RegistrationDateTime,
                    IsInProject = projectUserIds.Contains(principleInvestigator.UserId)
                });
            }
            projectDTO.UserDTOs = userDTOs;

            return View(projectDTO);
        }
        public async Task<IActionResult> AddPrincipleInvestigatorToProject(string passToken, string userPassToken)
        {
            ProjectUser projectUser = new ProjectUser
            {
                ProjectId = int.Parse(_protector.Encode(passToken)),
                UserId = int.Parse(_protector.Encode(userPassToken)),
                RoleId = 4
            };
            _context.ProjectUser.Add(projectUser);
            await _context.SaveChangesAsync();
            return RedirectToAction("AddPrincipleInvestigatorsToProject", "User" , new { passToken = passToken });
        }
        public async Task<IActionResult> RemovePrincipleInvestigatorFromProject(string passToken, string userPassToken)
        {
            ProjectUser projectUser = _context.ProjectUser.Where(k => k.ProjectId == int.Parse(_protector.Encode(passToken)) && k.UserId == int.Parse(_protector.Encode(userPassToken)) && k.RoleId == 4).FirstOrDefault();
            _context.ProjectUser.Remove(projectUser);
            await _context.SaveChangesAsync();
            return RedirectToAction("AddPrincipleInvestigatorsToProject", "User", new { passToken = passToken });
        }
        public async Task<IActionResult> ResearchAssistants(string _actionMessage)
        {
            User user = _context.User.Where(k => k.Email == User.Identity.Name).FirstOrDefault();
            int labId = _context.LabUser.Where(k => k.UserId == user.UserId).FirstOrDefault().LabId;
            List<int> userIds = await _context.LabUser.Where(k => k.LabId == labId && k.RoleId == 5).Select(k => k.UserId).ToListAsync();
            List<UserDTO> userDTOs = new List<UserDTO>();
            foreach (int userId in userIds)
            {
                User principleInvestigator = _context.User.Where(k => k.UserId == userId).FirstOrDefault();
                userDTOs.Add(new UserDTO
                {
                    Rank = principleInvestigator.Rank,
                    Firstname = principleInvestigator.Firstname,
                    Lastname = principleInvestigator.Lastname,
                    PassToken = _protector.Decode(principleInvestigator.UserId.ToString()),
                    PhotoUrl = principleInvestigator.PhotoUrl,
                    Email = principleInvestigator.Email,
                    Telephone = principleInvestigator.Telephone,
                    RegistrationDateTime = principleInvestigator.RegistrationDateTime
                });
            }

            ViewBag.actionMessage = _actionMessage;
            return View(userDTOs);
        }
        public IActionResult AddResearchAssistant() { return View(); }
        [HttpPost]
        public async Task<IActionResult> AddResearchAssistant(UserDTO _userDTO)
        {
            int labId = _context.LabUser.Where(k => k.UserId == (_context.User.Where(k => k.Email == User.Identity.Name).FirstOrDefault().UserId)).FirstOrDefault().LabId;
            //Data validation
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(AddResearchAssistant), new { _actionMessage = "Invalid Data" });
            }
            UserDTO userDTO = new UserDTO
            {
                Rank = _userDTO.Rank,
                Firstname = _userDTO.Firstname,
                Lastname = _userDTO.Lastname,
                Email = _userDTO.Email,
                Password = _userDTO.Password,
                RoleId = (int)RoleType.ResearchAssistant,
                PhotoUrl = PhotoUrl.DIRECTOR_OF_IT_ICON_PNG,
                Telephone = _userDTO.Telephone,
                RegistrationDateTime = DateTime.Now,
                IsActive = true
            };
            ResponseDTO responseDTO = await DataValidation.UserValidation(_context, userDTO);
            if (responseDTO.Result == "Failed")
                return RedirectToAction(nameof(AddResearchAssistant), new { _actionMessage = responseDTO.Message });

            if (await _context.User.AnyAsync(k => k.Email == userDTO.Email.Trim()))
                return RedirectToAction(nameof(AddResearchAssistant), new { _actionMessage = "Failed to create the user" });

            var hasCreated = await IdentityMigration.CreateIdentityUserAsync(_userManager, userDTO.Email.Trim(), userDTO.Password);
            if (!hasCreated.Succeeded)
                return RedirectToAction(nameof(AddResearchAssistant), new { _actionMessage = "Couldn't create the user identity" });

            User user = await Migration.ToUser(_context, userDTO);

            if (user == null)
                return RedirectToAction(nameof(AddResearchAssistant), new { _actionMessage = "Failed to create the user" });

            await Migration.InsertUserAsync(_context, user);
            if (_context.Lab.Where(k => k.Registered.AddDays(1) > DateTime.Now).Count() >= 10)
                return Json(MessageConverter.GetResponse("No new labs can be added today", MethodResult.Failed));

            LabUser labUser = new LabUser
            {
                UserId = user.UserId,
                LabId = labId,
                RoleId = 5
            };
            //Save data
            await _context.AddAsync(labUser);
            await _context.SaveChangesAsync();
            return RedirectToAction("ResearchAssistants", "User");
        }
        public async Task<IActionResult> RemoveResearchAssistant(string passToken)
        {
            User user = await _context.User.Where(k => k.UserId == int.Parse(_protector.Encode(passToken))).FirstOrDefaultAsync();
            await Migration.RemoveResearchAssistant(_context, user);
            return RedirectToAction(nameof(ResearchAssistants));
        }
        public async Task<IActionResult> AddResearchAssistantsToProject(string passToken)
        {
            ProjectDTO projectDTO = _context.Project
                .Where(k => k.ProjectId == int.Parse(_protector.Encode(passToken))).Select(k => new ProjectDTO
                {
                    FullName = k.FullName,
                    PassToken = _protector.Decode(k.ProjectId.ToString()),
                    Description = k.Description,
                    Color = k.Color,
                    PhotoUrl = k.PhotoUrl,
                    Registered = DateTime.Now,
                }).FirstOrDefault();
            User user = _context.User.Where(k => k.Email == User.Identity.Name).FirstOrDefault();
            int labId = _context.LabUser.Where(k => k.UserId == user.UserId).FirstOrDefault().LabId;
            List<int> userIds = await _context.LabUser.Where(k => k.LabId == labId && k.RoleId == 5).Select(k => k.UserId).ToListAsync();
            List<int> projectUserIds = await _context.ProjectUser.Where(k => k.ProjectId == int.Parse(_protector.Encode(passToken)) && k.RoleId == 5).Select(k => k.UserId).ToListAsync();
            List<UserDTO> userDTOs = new List<UserDTO>();
            foreach (int userId in userIds)
            {
                User principleInvestigator = _context.User.Where(k => k.UserId == userId).FirstOrDefault();
                userDTOs.Add(new UserDTO
                {
                    Firstname = principleInvestigator.Firstname,
                    Lastname = principleInvestigator.Lastname,
                    PassToken = _protector.Decode(principleInvestigator.UserId.ToString()),
                    Email = principleInvestigator.Email,
                    Telephone = principleInvestigator.Telephone,
                    RegistrationDateTime = principleInvestigator.RegistrationDateTime,
                    IsInProject = projectUserIds.Contains(principleInvestigator.UserId)
                });
            }
            projectDTO.UserDTOs = userDTOs;

            return View(projectDTO);
        }
        public async Task<IActionResult> AddResearchAssistantToProject(string passToken, string userPassToken)
        {
            ProjectUser projectUser = new ProjectUser
            {
                ProjectId = int.Parse(_protector.Encode(passToken)),
                UserId = int.Parse(_protector.Encode(userPassToken)),
                RoleId = 5
            };
            _context.ProjectUser.Add(projectUser);
            await _context.SaveChangesAsync();
            return RedirectToAction("AddResearchAssistantsToProject", "User", new { passToken = passToken });
        }
        public async Task<IActionResult> RemoveResearchAssistantFromProject(string passToken, string userPassToken)
        {
            ProjectUser projectUser = _context.ProjectUser.Where(k => k.ProjectId == int.Parse(_protector.Encode(passToken)) && k.UserId == int.Parse(_protector.Encode(userPassToken)) && k.RoleId == 5).FirstOrDefault();
            _context.ProjectUser.Remove(projectUser);
            await _context.SaveChangesAsync();
            return RedirectToAction("AddResearchAssistantsToProject", "User", new { passToken = passToken });
        }

        //Init Data
        [HttpGet]
        public JsonResult GetPhotoUrl()
        {
            return new JsonResult(_context.User.Where(k => k.Email == User.Identity.Name).FirstOrDefault().PhotoUrl);
        }
    }
}

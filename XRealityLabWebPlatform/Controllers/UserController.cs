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

namespace XRealityLab.Controllers
{
    [Authorize(Roles = "LabDirector,Principle Investigator")]
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
        public async Task<IActionResult> PrincipleInvestigators(string actionMessage)
        {
            User user = _context.User.Where(k => k.Email == User.Identity.Name).FirstOrDefault();
            int labId = _context.LabUser.Where(k => k.UserId == user.UserId).FirstOrDefault().LabId;
            List<int> userIds = await _context.LabUser.Where(k => k.LabId == labId && (k.RoleId == 3 || k.RoleId == 4)).Select(k => k.UserId).ToListAsync();
            List<UserDTO> userDTOs = new List<UserDTO>();
            foreach (int userId in userIds)
            {
                User principleInvestigator = _context.User.Where(k => k.UserId == userId).FirstOrDefault();
                userDTOs.Add(new UserDTO{
                    Firstname = principleInvestigator.Firstname,
                    Lastname = principleInvestigator.Lastname,
                    PassToken = _protector.Decode(principleInvestigator.UserId.ToString()),
                    Email = principleInvestigator.Email,
                    Telephone = principleInvestigator.Telephone,
                    RegistrationDateTime = principleInvestigator.RegistrationDateTime
                });
            }
            
            return View(userDTOs);
        }
        public IActionResult AddPrincipleInvestigator() { return View(); }
        [HttpPost]
        public async Task<IActionResult> AddPrincipleInvestigator(UserDTO _userDTO)
        {
            int labId = _context.LabUser.Where(k => k.UserId == (_context.User.Where(k => k.Email == User.Identity.Name).FirstOrDefault().UserId)).FirstOrDefault().LabId;
            //Data validation
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(AddPrincipleInvestigator), new { _actionMessage = "Δεν προσθέσατε τα πεδία σωστά. Παρακαλώ προσπαθήστε ξανά." });
            }
            UserDTO userDTO = new UserDTO
            {
                Firstname = _userDTO.Firstname,
                Lastname = _userDTO.Lastname,
                Email = _userDTO.Email,
                Password = "test123",
                RoleId = (int)RoleType.PrincipleInvestigator,
                PhotoUrl = PhotoUrl.DIRECTOR_OF_IT_ICON_PNG,
                Telephone = _userDTO.Telephone,
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
        public async Task<IActionResult> ResearchAssistants(string actionMessage)
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
                    Firstname = principleInvestigator.Firstname,
                    Lastname = principleInvestigator.Lastname,
                    PassToken = _protector.Decode(principleInvestigator.UserId.ToString()),
                    Email = principleInvestigator.Email,
                    Telephone = principleInvestigator.Telephone,
                    RegistrationDateTime = principleInvestigator.RegistrationDateTime
                });
            }

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
                return RedirectToAction(nameof(AddPrincipleInvestigator), new { _actionMessage = "Δεν προσθέσατε τα πεδία σωστά. Παρακαλώ προσπαθήστε ξανά." });
            }
            UserDTO userDTO = new UserDTO
            {
                Firstname = _userDTO.Firstname,
                Lastname = _userDTO.Lastname,
                Email = _userDTO.Email,
                Password = "test123",
                RoleId = (int)RoleType.ResearchAssistant,
                PhotoUrl = PhotoUrl.DIRECTOR_OF_IT_ICON_PNG,
                Telephone = _userDTO.Telephone,
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
    }
}

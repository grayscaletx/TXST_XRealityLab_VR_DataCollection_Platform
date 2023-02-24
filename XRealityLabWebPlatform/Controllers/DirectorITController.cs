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
            List<LabDTO> labDTOs = await _context.Lab.Include(k => k.LabDirector).Select(k => new LabDTO
            {
                LabId = k.LabId,
                PassToken = _protector.Decode(k.LabId.ToString()),
                FullName = k.FullName,
                LabDirectorId = k.LabDirectorId,
                Color = k.Color,
                PhotoUrl = k.PhotoUrl,
                Registered = k.Registered,
                LabDirector = new UserDTO
                {
                    UserId = k.LabDirector.UserId,
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
            return View(labDTOs);
        }
        public IActionResult AddLab() { return View(); }
        [HttpPost]
        public async Task<IActionResult> AddLab(LabDirectorDTO labDirectorDTO)
        {
            //Data validation
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(AddLab), new { _actionMessage = "Δεν προσθέσατε τα πεδία σωστά. Παρακαλώ προσπαθήστε ξανά." });
            }

            //ResponseDTO responseDTO = await ModelValidation.UserValidation(_context, _data);
            //if (responseDTO.Result == Enum.GetName(typeof(RoleType), MethodResult.Failed))
            //    return Json(responseDTO);

            UserDTO userDTO = new UserDTO
            {
                Firstname = labDirectorDTO.Firstname,
                Lastname = labDirectorDTO.Lastname,
                Email = labDirectorDTO.Email,
                Password = IdentityConversions.MD5Hash("test123"),
                RoleId = (int)RoleType.DirectorOfIT,
                PhotoUrl = PhotoUrl.DIRECTOR_OF_IT_ICON_PNG,
                Telephone = "698132154",
                RegistrationDateTime = DateTime.Now,
                IsActive = true
            };

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
            await _context.Lab.AddAsync(lab);
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

            labDirectorDTO.Rank = "Rank";
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
            lab.LabId = int.Parse(_protector.Encode(labDirectorDTO.PassToken));
            lab.FullName = labDirectorDTO.FullName;
            lab.LabDirectorId = director.UserId;
            lab.Description = labDirectorDTO.Description;
            lab.Color = labDirectorDTO.Color;
            lab.PhotoUrl = PhotoUrl.LAB_ICON_PNG;
            lab.Registered = DateTime.Now;

            _context.Lab.Update(lab);
            await _context.SaveChangesAsync();

            return RedirectToAction("Labs", RoleType.DirectorOfIT.GetEnumDescription());
        }
        public async Task<IActionResult> RemoveLab(string passToken)
        {
            Lab lab = await _context.Lab.Where(k => k.LabId == int.Parse(_protector.Encode(passToken))).FirstOrDefaultAsync();
            await Migration.RemoveLabDirectorAsync(_context, _userManager, lab);

            return RedirectToAction(nameof(Labs));
        }
    }
}
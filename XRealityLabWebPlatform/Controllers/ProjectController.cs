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
    [Authorize(Roles = "LabDirector")]
    public class ProjectController : Controller
    {
        private readonly XRealityLabdbContext _context;
        private IUserRepository _userRepository;
        private readonly IHostingEnvironment _environment;
        private readonly CustomIDataProtection _protector;
        private readonly UserManager<IdentityUser> _userManager;

        public ProjectController(XRealityLabdbContext context, IHostingEnvironment IHostingEnvironment, CustomIDataProtection customIDataProtection,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userRepository = new UserRepository(_context);
            _environment = IHostingEnvironment;
            _userManager = userManager;
            _protector = customIDataProtection;
        }

        public async Task<IActionResult> AllProjects(string actionMessage)
        {
            List<ProjectDTO> projectDTOs = await _context.Project.Select(k => new ProjectDTO
            {
                ProjectId = k.ProjectId,
                PassToken = _protector.Decode(k.ProjectId.ToString()),
                FullName = k.FullName,
                //LabDirectorId = k.LabDirectorId,
                Color = k.Color,
                PhotoUrl = k.PhotoUrl,
                Registered = k.Registered,
                //LabDirector = new UserDTO
                //{
                //    UserId = k.LabDirector.UserId,
                //    Firstname = k.LabDirector.Firstname,
                //    Lastname = k.LabDirector.Lastname,
                //    Email = k.LabDirector.Email,
                //    Telephone = k.LabDirector.Telephone,
                //    PhotoUrl = k.LabDirector.PhotoUrl,
                //    RoleId = k.LabDirector.RoleId,
                //    IsActive = k.LabDirector.IsActive == 1 ? true : false,
                //    RegistrationDateTime = k.LabDirector.RegistrationDateTime
                //}
            }).ToListAsync();
            return View(projectDTOs);
        }
        public async Task<IActionResult> MyProjects(string actionMessage)
        {
            List<ProjectDTO> projectDTOs = await _context.Project.Select(k => new ProjectDTO
            {
                ProjectId = k.ProjectId,
                PassToken = _protector.Decode(k.ProjectId.ToString()),
                FullName = k.FullName,
                //LabDirectorId = k.LabDirectorId,
                Color = k.Color,
                PhotoUrl = k.PhotoUrl,
                Registered = k.Registered,
                //LabDirector = new UserDTO
                //{
                //    UserId = k.LabDirector.UserId,
                //    Firstname = k.LabDirector.Firstname,
                //    Lastname = k.LabDirector.Lastname,
                //    Email = k.LabDirector.Email,
                //    Telephone = k.LabDirector.Telephone,
                //    PhotoUrl = k.LabDirector.PhotoUrl,
                //    RoleId = k.LabDirector.RoleId,
                //    IsActive = k.LabDirector.IsActive == 1 ? true : false,
                //    RegistrationDateTime = k.LabDirector.RegistrationDateTime
                //}
            }).ToListAsync();
            return View(projectDTOs);
        }

        public IActionResult AddProject() { return View(); }
    }
}
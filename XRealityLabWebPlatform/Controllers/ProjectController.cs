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
    [Authorize(Roles = "LabDirector,PrincipleInvestigator,ResearchAssistant")]
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
            //Get labDirectorId
            int labDirectrorId = _context.User.Where(k => k.Email == User.Identity.Name).FirstOrDefault().UserId;
            List<int> projectIds = await _context.ProjectUser.Where(x => x.UserId == labDirectrorId && x.RoleId == 3).Select(k => k.ProjectId).ToListAsync();
            List<ProjectDTO> projectDTOs = new List<ProjectDTO>();
            foreach (int projectId in projectIds)
            {
                List<int> projectPIIds = await _context.ProjectUser.Where(k => k.ProjectId == projectId && k.RoleId == 4).Select(k => k.UserId).ToListAsync();
                List<UserDTO> userDTOs = new List<UserDTO>();
                foreach (int projectPIId in projectPIIds)
                {
                    User principleInvestigator = _context.User.Where(k => k.UserId == projectPIId).FirstOrDefault();
                    userDTOs.Add(new UserDTO
                    {
                        Rank = principleInvestigator.Rank,
                        Firstname = principleInvestigator.Firstname,
                        Lastname = principleInvestigator.Lastname,
                        PassToken = _protector.Decode(principleInvestigator.UserId.ToString()),
                        Email = principleInvestigator.Email,
                        Telephone = principleInvestigator.Telephone,
                        RegistrationDateTime = principleInvestigator.RegistrationDateTime
                    });
                    if (userDTOs.Count() > 3)
                        break;
                }
                Project project = _context.Project.Where(k => k.ProjectId == projectId).FirstOrDefault();
                projectDTOs.Add(new ProjectDTO
                {
                    ProjectId = project.ProjectId,
                    PassToken = _protector.Decode(project.ProjectId.ToString()),
                    FullName = project.FullName.Length < 25 ? project.FullName : project.FullName.Substring(0, 25) + "...",
                    Color = project.Color,
                    PhotoUrl = project.PhotoUrl,
                    Registered = project.Registered,
                    UserDTOs = userDTOs
                });
            }       
            
            return View(projectDTOs);
        }
        public async Task<IActionResult> MyProjects(string actionMessage)
        {
            int userId = _context.User.Where(k => k.Email == User.Identity.Name).FirstOrDefault().UserId;
            List<int> projectIds = await _context.ProjectUser.Where(x => (x.UserId == userId && (x.RoleId == 4 || x.RoleId == 5))).Select(k => k.ProjectId).ToListAsync();
            List<ProjectDTO> projectDTOs = new List<ProjectDTO>();
            foreach (int projectId in projectIds)
            {
                List<int> projectPIIds = await _context.ProjectUser.Where(k => k.ProjectId == projectId && k.RoleId == 4).Select(k => k.UserId).ToListAsync();
                List<UserDTO> userDTOs = new List<UserDTO>();
                foreach (int projectPIId in projectPIIds)
                {
                    User principleInvestigator = _context.User.Where(k => k.UserId == projectPIId).FirstOrDefault();
                    userDTOs.Add(new UserDTO
                    {
                        Rank = principleInvestigator.Rank,
                        Firstname = principleInvestigator.Firstname,
                        Lastname = principleInvestigator.Lastname,
                        PassToken = _protector.Decode(principleInvestigator.UserId.ToString()),
                        Email = principleInvestigator.Email,
                        Telephone = principleInvestigator.Telephone,
                        RegistrationDateTime = principleInvestigator.RegistrationDateTime
                    });
                    if (userDTOs.Count() > 3)
                        break;
                }
                Project project = _context.Project.Where(k => k.ProjectId == projectId).FirstOrDefault();
                projectDTOs.Add(new ProjectDTO
                {
                    ProjectId = project.ProjectId,
                    PassToken = _protector.Decode(project.ProjectId.ToString()),
                    FullName = project.FullName.Length < 25 ? project.FullName : project.FullName.Substring(0, 25) + "...",
                    Color = project.Color,
                    PhotoUrl = project.PhotoUrl,
                    Registered = project.Registered,
                    UserDTOs = userDTOs
                });
            }
            return View(projectDTOs);
        }

        public IActionResult AddProject() { return View(); }
        [HttpPost]
        public async Task<IActionResult> AddProject(ProjectDTO projectDTO)
        {
            //Data validation
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(AddProject), new { _actionMessage = "Δεν προσθέσατε τα πεδία σωστά. Παρακαλώ προσπαθήστε ξανά." });
            }
            //User that call AddProject
            User user = _context.User.Where(k => k.Email == User.Identity.Name).FirstOrDefault();
            int labId = _context.LabUser.Where(k => k.UserId == user.UserId).FirstOrDefault().LabId;
            //Get LabDirectorID
            int labDirectrorId = _context.Lab.Where(k => k.LabId == labId).FirstOrDefault().LabDirectorId;
            //Save data
            Project project = new Project
            {
                FullName = projectDTO.FullName,
                Description = projectDTO.Description,
                Color = "#6a5738",
                PhotoUrl = PhotoUrl.PROJECT_ICON_PNG,
                Registered = DateTime.Now
            };
            ResponseDTO responseDTO = await DataValidation.ProjectValidation(_context, project);
            if (responseDTO.Result == "Failed")
                return Json(responseDTO);

            await _context.Project.AddAsync(project);
            //Get ProjectId
            await _context.SaveChangesAsync();
            Project thisProject = _context.Project.Where(k => k.FullName == project.FullName).FirstOrDefault();            
            ProjectUser projectUser = new ProjectUser
            {
                ProjectId = thisProject.ProjectId,
                UserId = labDirectrorId,
                RoleId = 3
            };
            //If caller is a PI add him as PI
            if(user.RoleId == 4)
            {
                ProjectUser projectUserPi = new ProjectUser
                {
                    ProjectId = thisProject.ProjectId,
                    UserId = user.UserId,
                    RoleId = 4
                };
                await _context.ProjectUser.AddAsync(projectUserPi);
            }
            await _context.ProjectUser.AddAsync(projectUser);
            await _context.SaveChangesAsync();


            return RedirectToAction("AllProjects","Project");
        }
        public IActionResult EditProject(string passToken)
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

            return View(projectDTO);
        }
        [HttpPost]
        public async Task<IActionResult> EditProject(ProjectDTO projectDTO)
        {
            //Data validation
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(AllProjects), new { _actionMessage = "Δεν προσθέσατε τα πεδία σωστά. Παρακαλώ προσπαθήστε ξανά." });
            }
            //Save data
            Project project = _context.Project.Where(k => k.ProjectId == int.Parse(_protector.Encode(projectDTO.PassToken))).FirstOrDefault();
            project.ProjectId = int.Parse(_protector.Encode(projectDTO.PassToken));
            project.FullName = projectDTO.FullName;
            project.Description = projectDTO.Description;
            project.Color = projectDTO.Color;
            project.PhotoUrl = PhotoUrl.LAB_ICON_PNG;
            project.Registered = DateTime.Now;

            ResponseDTO responseDTO = await DataValidation.ProjectValidation(_context, project);
            if (responseDTO.Result == "Failed")
                return Json(responseDTO);

            _context.Project.Update(project);
            await _context.SaveChangesAsync();

            return RedirectToAction("AllProjects", "Project");
        }
        public async Task<IActionResult> RemoveProject(string passToken)
        {
            Project project = await _context.Project.Where(k => k.ProjectId == int.Parse(_protector.Encode(passToken))).FirstOrDefaultAsync();
            await Migration.RemoveProjectAsync(_context, project);
            return RedirectToAction(nameof(AllProjects));
        }
        public async Task<IActionResult> EditResearchMethods(string passToken)
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
            List<int> researchIds = await _context.ProjectResearch.Where(k => k.ProjectId == int.Parse(_protector.Encode(passToken))).Select(k => k.ResearchId).ToListAsync();
            List<ResearchDTO> researchDTOs = new List<ResearchDTO>();
            foreach (var researchId in researchIds)
            {
                Research research = _context.Research.Where(k => k.ResearchId == researchId).FirstOrDefault();
                string typeId = _context.ResearchMethod.Where(k => k.ResearchMethodId == research.ResearchMethodId).FirstOrDefault().TypeId;
                researchDTOs.Add(new ResearchDTO 
                {
                    ResearchId = researchId,
                    Title = research.Title,
                    TypeId = typeId,
                    Resgistered = research.Registered
                });
            }
            projectDTO.ResearchDTOs = researchDTOs;

            return View(projectDTO);
        }
        public IActionResult NewResearchMethod(string passToken) 
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
            List<ResearchDTO> researchDTOs = _context.ResearchMethod.Select(k => new ResearchDTO
            {
                ResearchMethodId = k.ResearchMethodId,
                TypeId = k.TypeId
            }).ToList();
            projectDTO.ResearchDTOs = researchDTOs;
            return View(projectDTO); 
        }
        [HttpPost]
        public async Task<IActionResult> AddNewResearchMethod(ProjectDTO projectDTO)
        {
            Research research = new Research
            {
                Title = projectDTO.Title,
                ResearchMethodId = projectDTO.ResearchMethodId,
                Registered = DateTime.Now
            };
            _context.Research.Add(research);
            await _context.SaveChangesAsync();
            int researchId = _context.Research.Where(k => k.Title == projectDTO.Title && k.ResearchMethodId == projectDTO.ResearchMethodId).FirstOrDefault().ResearchId;
            ProjectResearch projectResearch = new ProjectResearch
            {
                ResearchId = researchId,
                ProjectId = int.Parse(_protector.Encode(projectDTO.PassToken))
            };
            _context.ProjectResearch.Add(projectResearch);
            _context.SaveChanges();
            return RedirectToAction("EditResearchMethods", "Project", new { passToken = projectDTO.PassToken });
        }
        public async Task<IActionResult> DeleteResearchMethod(string passToken, string researchId)
        {
            ProjectResearch projectResearch = _context.ProjectResearch.Where(k => k.ResearchId == int.Parse(researchId)).FirstOrDefault();
            Research research = _context.Research.Where(k => k.ResearchId == int.Parse(researchId)).FirstOrDefault();
            _context.ProjectResearch.Remove(projectResearch);
            _context.Research.Remove(research);
            await _context.SaveChangesAsync();
            return RedirectToAction("EditResearchMethods", "Project", new { passToken = passToken });
        }
        public async Task<IActionResult> DataCollections(string passToken)
        {
            ProjectDTO projectDTO = _context.Project
                .Where(k => k.ProjectId == int.Parse(_protector.Encode(passToken))).Select(k => new ProjectDTO
                {
                    FullName = k.FullName,
                    PassToken = _protector.Decode(k.ProjectId.ToString()),
                    Description = k.Description,
                    Color = k.Color,
                    PhotoUrl = k.PhotoUrl,
                    Registered = k.Registered,
                }).FirstOrDefault();
            List<int> sessionIds = await _context.ProjectSession.Where(k => k.ProjectId == int.Parse(_protector.Encode(passToken))).Select(k => k.SessionId).ToListAsync();
            List<SessionDTO> sessionDTOs = new List<SessionDTO>();
            foreach (var sessionId in sessionIds)
            {
                Session session = _context.Session.Where(k => k.SessionId == sessionId).FirstOrDefault();
                int participantsCount = _context.SessionParticipant.Where(k => k.SessionId == sessionId).Count();
                sessionDTOs.Add(new SessionDTO
                {
                    SessionId = session.SessionId,
                    PassToken = _protector.Decode(session.SessionId.ToString()),
                    Title = session.Title,
                    Description = session.Description,
                    Registered = session.Registered,
                    ParticipantsCount = participantsCount
                });
            }
            projectDTO.SessionDTOs = sessionDTOs;
            return View(projectDTO);
        }
        public IActionResult NewSession(string passToken)
        {
            SessionDTO sessionDTO = new SessionDTO
            {
                ProjectPassToken = passToken
            };
            return View(sessionDTO);
        }
        [HttpPost]
        public async Task<IActionResult> NewSession(SessionDTO sessionDTO)
        {
            Session session = new Session
            {
                Title = sessionDTO.Title,
                Description = sessionDTO.Description,
                Registered = DateTime.Now
            };
            _context.Session.Add(session);
            await _context.SaveChangesAsync();
            int sessionId = _context.Session.Where(k => k.Title == sessionDTO.Title).FirstOrDefault().SessionId;
            ProjectSession projectSession = new ProjectSession
            {
                SessionId = sessionId,
                ProjectId = int.Parse(_protector.Encode(sessionDTO.ProjectPassToken))
            };
            _context.ProjectSession.Add(projectSession);
            _context.SaveChanges();
            return RedirectToAction("DataCollections", "Project", new { passToken = sessionDTO.ProjectPassToken });
        }
        public async Task<IActionResult> DeleteSession(string passToken, string sessionId)
        {
            ProjectSession projectSession = _context.ProjectSession.Where(k => k.SessionId == int.Parse(sessionId)).FirstOrDefault();
            Session session = _context.Session.Where(k => k.SessionId == int.Parse(sessionId)).FirstOrDefault();
            _context.ProjectSession.Remove(projectSession);
            _context.Session.Remove(session);
            await _context.SaveChangesAsync();
            return RedirectToAction("DataCollections", "Project", new { passToken = passToken });
        }
        public async Task<IActionResult> Participants(string passToken)
        {
            SessionDTO sessionDTO = _context.Session
                .Where(k => k.SessionId == int.Parse(_protector.Encode(passToken))).Select(k => new SessionDTO
                {
                    PassToken = passToken,
                    Title = k.Title,
                    Description = k.Description,
                    Registered = k.Registered
                }).FirstOrDefault();
            List<int> participantsIds = await _context.SessionParticipant.Where(k => k.SessionId == int.Parse(_protector.Encode(passToken))).Select(k => k.ParticipantId).ToListAsync();
            List<ParticipantDTO> participantDTOs = new List<ParticipantDTO>();
            foreach (var participantId in participantsIds)
            {
                Participant participant = _context.Participant.Where(k => k.ParticipantId == participantId).FirstOrDefault();
                participantDTOs.Add(new ParticipantDTO
                {
                    ParticipantId = participant.ParticipantId,
                    PassToken = _protector.Decode(participant.ParticipantId.ToString()),
                    FirstName = participant.FirstName,
                    LastName = participant.LastName,
                    Email = participant.Email,
                    Registered = participant.Registered                    
                });
            }
            sessionDTO.participantDTOs = participantDTOs;
            return View(sessionDTO);
        }
        public IActionResult NewParticipant(string passToken)
        {
            ParticipantDTO participantDTO = new ParticipantDTO
            {
                SessionPassToken = passToken
            };
            return View(participantDTO);
        }
        [HttpPost]
        public async Task<IActionResult> NewParticipant(ParticipantDTO participantDTO)
        {
            Participant _participant = _context.Participant.Where(k => k.Email == participantDTO.Email).FirstOrDefault();
            if (_participant != null)
            {
                if(_context.SessionParticipant.Where(k => k.SessionId == int.Parse(_protector.Encode(participantDTO.SessionPassToken))).Any())
                    return RedirectToAction("Participants", "Project", new { passToken = participantDTO.SessionPassToken });

                SessionParticipant _sessionParticipant = new SessionParticipant
                {
                    ParticipantId = _participant.ParticipantId,
                    SessionId = int.Parse(_protector.Encode(participantDTO.SessionPassToken))
                };
                _context.SessionParticipant.Add(_sessionParticipant);
                _context.SaveChanges();
                return RedirectToAction("Participants", "Project", new { passToken = participantDTO.SessionPassToken });
            }
            Participant participant = new Participant
            {
                FirstName = participantDTO.FirstName,
                LastName = participantDTO.LastName,
                Email = participantDTO.Email,
                Registered = DateTime.Now
            };
            _context.Participant.Add(participant);
            await _context.SaveChangesAsync();
            int participantId = _context.Participant.Where(k => k.Email == participant.Email).FirstOrDefault().ParticipantId;
            SessionParticipant sessionParticipant = new SessionParticipant
            {
                ParticipantId = participantId,
                SessionId = int.Parse(_protector.Encode(participantDTO.SessionPassToken))
            };
            _context.SessionParticipant.Add(sessionParticipant);
            _context.SaveChanges();
            return RedirectToAction("Participants", "Project", new { passToken = participantDTO.SessionPassToken });
        }
        public async Task<IActionResult> DeleteParticipant(string passToken, string participantId)
        {
            SessionParticipant sessionParticipant = _context.SessionParticipant.Where(k => k.ParticipantId == int.Parse(participantId)).FirstOrDefault();
            Participant participant = _context.Participant.Where(k => k.ParticipantId == int.Parse(participantId)).FirstOrDefault();
            _context.SessionParticipant.Remove(sessionParticipant);
            _context.Participant.Remove(participant);
            await _context.SaveChangesAsync();
            return RedirectToAction("Participants", "Project", new { passToken = passToken });
        }
    }    
}
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
using System.Text;
using System.IO.Compression;

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

        public async Task<IActionResult> AllProjects(string _actionMessage)
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

            ViewBag.actionMessage = _actionMessage;
            return View(projectDTOs);
        }
        public async Task<IActionResult> MyProjects(string _actionMessage)
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
            ViewBag.actionMessage = _actionMessage;
            return View(projectDTOs);
        }

        public IActionResult AddProject(string _actionMessage) { ViewBag.actionMessage = _actionMessage; return View(); }
        [HttpPost]
        public async Task<IActionResult> AddProject(ProjectDTO projectDTO)
        {
            //Data validation
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(AddProject), new { _actionMessage = "Invalid Data" });
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
                Color = projectDTO.Color,
                Registered = DateTime.Now
            };

            //Upload icon
            if (!string.IsNullOrWhiteSpace(projectDTO.PhotoBase64))
            {
                //Check if the file is jpg or png
                if (!(projectDTO.PhotoBase64.StartsWith("data:image/png;base64,") || projectDTO.PhotoBase64.StartsWith("data:image/jpeg;base64,")))
                {
                    return RedirectToAction(nameof(AddProject), new { _actionMessage = "Invalid Data" });
                }
                //CONVERT base64 to Image <-Library
                string base64 = projectDTO.PhotoBase64.Substring(projectDTO.PhotoBase64.IndexOf(',') + 1);
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
                if (projectDTO.PhotoBase64.StartsWith("data:image/png;base64,"))
                {
                    fileExtension = ".png";
                }
                else if (projectDTO.PhotoBase64.StartsWith("data:image/jpeg;base64,"))
                {
                    fileExtension = ".jpeg";
                }
                string newFileName = myUniqueFileName + fileExtension;
                var fileName = string.Empty;
                fileName = Path.Combine(_environment.WebRootPath, "_images", "project-photos") + $@"\{newFileName}";
                image.Save(fileName);

                bool isUploaded = await ContainerService.UploadOperationAsync(Path.Combine(_environment.WebRootPath, "_images", "project-photos"),
                    newFileName, AzureGPV2Credentials.CONTAINER_PROJECT_ICONS);

                if (isUploaded)
                {
                    project.PhotoUrl = Path.Combine(BaseLink.AZURE_BLOB_URL, AzureGPV2Credentials.CONTAINER_PROJECT_ICONS, newFileName);
                }

                //DELETE from root
                if (System.IO.File.Exists(Path.Combine(_environment.WebRootPath, "_images", "project-photos") + $@"\{newFileName}"))
                {
                    System.IO.File.Delete(Path.Combine(_environment.WebRootPath, "_images", "project-photos") + $@"\{newFileName}");
                }
            }
            else
            {
                project.PhotoUrl = PhotoUrl.PROJECT_ICON_PNG;
            }

            ResponseDTO responseDTO = await DataValidation.ProjectValidation(_context, project);
            if (responseDTO.Result == "Failed")
                return RedirectToAction(nameof(AddProject), new { _actionMessage = responseDTO.Message });

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


            return RedirectToAction("MyProjects","Project");
        }
        public IActionResult EditProject(string passToken, string _actionMessage)
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

            ViewBag.actionMessage = _actionMessage;
            return View(projectDTO);
        }
        [HttpPost]
        public async Task<IActionResult> EditProject(ProjectDTO projectDTO)
        {
            //Data validation
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(EditProject), new {passToken = projectDTO.PassToken , _actionMessage = "Invalid Data" });
            }
            //Save data
            Project project = _context.Project.Where(k => k.ProjectId == int.Parse(_protector.Encode(projectDTO.PassToken))).FirstOrDefault();

            //Upload icon
            if (!string.IsNullOrWhiteSpace(projectDTO.PhotoBase64))
            {
                //Check if the file is jpg or png
                if (!(projectDTO.PhotoBase64.StartsWith("data:image/png;base64,") || projectDTO.PhotoBase64.StartsWith("data:image/jpeg;base64,")))
                {
                    return RedirectToAction(nameof(EditProject), new { passToken = projectDTO.PassToken, _actionMessage = "Invalid Data" });
                }
                //CONVERT base64 to Image <-Library
                string base64 = projectDTO.PhotoBase64.Substring(projectDTO.PhotoBase64.IndexOf(',') + 1);
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
                if (projectDTO.PhotoBase64.StartsWith("data:image/png;base64,"))
                {
                    fileExtension = ".png";
                }
                else if (projectDTO.PhotoBase64.StartsWith("data:image/jpeg;base64,"))
                {
                    fileExtension = ".jpeg";
                }
                string newFileName = myUniqueFileName + fileExtension;
                var fileName = string.Empty;
                fileName = Path.Combine(_environment.WebRootPath, "_images", "project-photos") + $@"\{newFileName}";
                image.Save(fileName);

                bool isUploaded = await ContainerService.UploadOperationAsync(Path.Combine(_environment.WebRootPath, "_images", "project-photos"),
                    newFileName, AzureGPV2Credentials.CONTAINER_PROJECT_ICONS);

                if (isUploaded)
                {
                    project.PhotoUrl = Path.Combine(BaseLink.AZURE_BLOB_URL, AzureGPV2Credentials.CONTAINER_PROJECT_ICONS, newFileName);
                }

                //DELETE from root
                if (System.IO.File.Exists(Path.Combine(_environment.WebRootPath, "_images", "project-photos") + $@"\{newFileName}"))
                {
                    System.IO.File.Delete(Path.Combine(_environment.WebRootPath, "_images", "project-photos") + $@"\{newFileName}");
                }
            }


            project.ProjectId = int.Parse(_protector.Encode(projectDTO.PassToken));
            project.FullName = projectDTO.FullName;
            project.Description = projectDTO.Description;
            project.Color = projectDTO.Color;
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
                    Registered = research.Registered
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
        /*public async Task<IActionResult> DeleteResearchMethod(string passToken, string researchId)
        {
            ProjectResearch projectResearch = _context.ProjectResearch.Where(k => k.ResearchId == int.Parse(researchId)).FirstOrDefault();
            Research research = _context.Research.Where(k => k.ResearchId == int.Parse(researchId)).FirstOrDefault();
            _context.ProjectResearch.Remove(projectResearch);
            _context.Research.Remove(research);
            await _context.SaveChangesAsync();
            return RedirectToAction("EditResearchMethods", "Project", new { passToken = passToken });
        }*/
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

            //Remove all participants from this session



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
            List<int> researchIds = await _context.ProjectResearch.Where(k => k.ProjectId == _context.ProjectSession.Where(x => x.SessionId == int.Parse(_protector.Encode(passToken))).FirstOrDefault().ProjectId).Select(k => k.ResearchId).ToListAsync();
            List<ResearchDTO> researchDTOs = new List<ResearchDTO>();
            foreach (var researchId in researchIds)
            {
                Research research = _context.Research.Where(k => k.ResearchId == researchId).FirstOrDefault();
                string typeId = _context.ResearchMethod.Where(k => k.ResearchMethodId == research.ResearchMethodId).FirstOrDefault().TypeId;
                researchDTOs.Add(new ResearchDTO
                {
                    ResearchId = research.ResearchId,
                    Title = research.Title,
                    TypeId = typeId,
                    Registered = research.Registered,
                    ResearchMethodId = research.ResearchMethodId
                });
            }
            sessionDTO.researchDTOs = researchDTOs;
            List<int> participantsIds = await _context.SessionParticipant.Where(k => k.SessionId == int.Parse(_protector.Encode(passToken))).Select(k => k.ParticipantId).ToListAsync();
            List<ParticipantDTO> participantDTOs = new List<ParticipantDTO>();
            foreach (var participantId in participantsIds)
            {
                Participant participant = _context.Participant.Where(k => k.ParticipantId == participantId).FirstOrDefault();
                List<ResearchCompletionDTO> researchCompletionDTOs = new List<ResearchCompletionDTO>(researchDTOs.Count());
                foreach (var researchDTO in researchDTOs)
                {
                    bool completed = false;
                    if (researchDTO.ResearchMethodId == (int)ResearchType.VirtualAugmentedReality)
                    {
                        //VAR can have more than one entry
                        completed = _context.ParticipantResult.Where(k => k.ParticipantId == participant.ParticipantId && k.SessionId == int.Parse(_protector.Encode(passToken)) && k.ResearchId == researchDTO.ResearchId).Any();
                    }
                    else if (researchDTO.ResearchMethodId == (int)ResearchType.QualtricsQuestionnaire)
                    {
                        completed = _context.QualtricsResult.Where(k => k.ParticipantId == participant.ParticipantId && k.SessionId == int.Parse(_protector.Encode(passToken)) && k.ResearchId == researchDTO.ResearchId).Any();
                    }
                    else if (researchDTO.ResearchMethodId == (int)ResearchType.OfflineMethod)
                    {
                        OfflineResult offlineResult = _context.OfflineResult.Where(k => k.ParticipantId == participant.ParticipantId && k.SessionId == int.Parse(_protector.Encode(passToken)) && k.ResearchId == researchDTO.ResearchId).FirstOrDefault();
                        completed = offlineResult != null ? offlineResult.Completed == 1 ? true : false : false ;
                    }
                    researchCompletionDTOs.Add(new ResearchCompletionDTO
                    {
                        ResearchId = researchDTO.ResearchId,
                        PassToken = _protector.Decode(researchDTO.ResearchId.ToString()),
                        ResearchMethodId = researchDTO.ResearchMethodId,
                        Title = researchDTO.Title,
                        TypeId = researchDTO.TypeId,
                        Resgistered = researchDTO.Registered,
                        //Need info for Completed on how to handle
                        Completed = completed
                    });
                }
                participantDTOs.Add(new ParticipantDTO
                {
                    ParticipantId = participant.ParticipantId,
                    PassToken = _protector.Decode(participant.ParticipantId.ToString()),
                    FirstName = participant.FirstName,
                    LastName = participant.LastName,
                    Email = participant.Email,
                    Registered = participant.Registered,
                    researchCompletionDTOs = researchCompletionDTOs
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
            Session session = _context.Session.Where(k => k.SessionId == int.Parse(_protector.Encode(passToken))).FirstOrDefault();
            Participant participant = _context.Participant.Where(k => k.ParticipantId == int.Parse(participantId)).FirstOrDefault();
            ResponseDTO responseDTO = await Migration.RemoveParticipantAsync(_context, session, participant);                        
            return RedirectToAction("Participants", "Project", new { passToken = passToken });
        }
        //Init Data
        [HttpGet]
        public JsonResult GetPhotoUrl()
        {
            return new JsonResult(_context.User.Where(k => k.Email == User.Identity.Name).FirstOrDefault().PhotoUrl);
        }
        public async Task<IActionResult> Results(string passToken/*participant*/, string sessionPassToken)
        {
            ResultDTO resultDTO = new ResultDTO
            {
                SessionPassToken = sessionPassToken,
                SessionMD5 = IdentityConversions.MD5Hash(_protector.Encode(sessionPassToken)),
                ParticipantPassToken = passToken,
                ParticipantMD5 = IdentityConversions.MD5Hash(_protector.Encode(passToken)),
                Title = _context.Session.Where(k => k.SessionId == int.Parse(_protector.Encode(sessionPassToken))).FirstOrDefault().Title,
                Notes = _context.SessionParticipant.Where(k => k.ParticipantId == int.Parse(_protector.Encode(passToken)) && k.SessionId == int.Parse(_protector.Encode(sessionPassToken))).FirstOrDefault().Notes,
                ParticipantDTO = _context.Participant.Where(k => k.ParticipantId == int.Parse(_protector.Encode(passToken)))
                .Select(x => new ParticipantDTO {
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                    Email = x.Email
                }).FirstOrDefault()
            };
            //ResearchMethods Lists

            List<int> researchIds = await _context.ProjectResearch.Where(k => k.ProjectId == _context.ProjectSession.Where(x => x.SessionId == int.Parse(_protector.Encode(sessionPassToken))).FirstOrDefault().ProjectId).Select(k => k.ResearchId).ToListAsync();
            List<ResearchDTO> researchDTOs = new List<ResearchDTO>();
            
            foreach (var researchId in researchIds)
            {
                Research research = _context.Research.Where(k => k.ResearchId == researchId).FirstOrDefault();
                string typeId = _context.ResearchMethod.Where(k => k.ResearchMethodId == research.ResearchMethodId).FirstOrDefault().TypeId;
                List<VAResultDTO> vAResultDTOs = new List<VAResultDTO>();
                QualtricsResultDTO qualtricsResultDTO = new QualtricsResultDTO();
                OfflineResultDTO offlineResultDTO = new OfflineResultDTO();
                if (research.ResearchMethodId == (int)ResearchType.VirtualAugmentedReality)
                {
                    //VAR can have more than one entry
                    vAResultDTOs.AddRange(
                        await _context.ParticipantResult.Where(k => k.ResearchId == research.ResearchId && k.ParticipantId == int.Parse(_protector.Encode(passToken)) && k.SessionId == int.Parse(_protector.Encode(sessionPassToken)))
                        .Select(x => new VAResultDTO
                        {
                            Title = research.Title,
                            ResearchPassToken = _protector.Decode(research.ResearchId.ToString()),
                            ResearchMD5 = IdentityConversions.MD5Hash(research.ResearchId.ToString()),
                            ParticipantResultId = x.ParticipantResultId,
                            ParticipantId = x.ParticipantId,
                            SessionId = x.SessionId,
                            ResearchId = (int)x.ResearchId,
                            Time = x.Time,
                            Registered = x.Registered,
                            NoOfMistakes = x.NoOfMistakes,
                            Notes = x.Notes
                        }).ToListAsync());
                }
                else if (research.ResearchMethodId == (int)ResearchType.QualtricsQuestionnaire)
                {
                    qualtricsResultDTO = (
                        _context.QualtricsResult.Where(k => k.ResearchId == research.ResearchId && k.ParticipantId == int.Parse(_protector.Encode(passToken)) && k.SessionId == int.Parse(_protector.Encode(sessionPassToken)))
                        .Select(x => new QualtricsResultDTO
                        {
                            Title = research.Title,
                            ResearchPassToken = _protector.Decode(research.ResearchId.ToString()),
                            ResearchMD5 = IdentityConversions.MD5Hash(research.ResearchId.ToString()),
                            ParticipantId = x.ParticipantId,
                            SessionId = x.SessionId,
                            ResearchId = (int)x.ResearchId,
                            DocumentUrl = x.DocumentUrl,
                            //Notes = x.Notes
                        }).FirstOrDefault());                        
                }
                else if (research.ResearchMethodId == (int)ResearchType.OfflineMethod)
                {
                    offlineResultDTO = (
                        _context.OfflineResult.Where(k => k.ResearchId == research.ResearchId && k.ParticipantId == int.Parse(_protector.Encode(passToken)) && k.SessionId == int.Parse(_protector.Encode(sessionPassToken)))
                        .Select(x => new OfflineResultDTO
                        {
                            Title = research.Title,
                            ResearchPassToken = _protector.Decode(research.ResearchId.ToString()),
                            ResearchMD5 = IdentityConversions.MD5Hash(research.ResearchId.ToString()),
                            ParticipantId = x.ParticipantId,
                            SessionId = x.SessionId,
                            ResearchId = (int)x.ResearchId,
                            DocumentUrl = x.DocumentUrl,
                            //Notes = x.Notes
                        }).FirstOrDefault());
                }
                researchDTOs.Add(new ResearchDTO
                {
                    ResearchId = research.ResearchId,
                    ResearchPassToken = _protector.Decode(research.ResearchId.ToString()),
                    ResearchMD5 = IdentityConversions.MD5Hash(research.ResearchId.ToString()),
                    Title = research.Title,
                    TypeId = typeId,
                    Registered = research.Registered,
                    VAResultDTOs = vAResultDTOs,
                    QualtricsResultDTO = qualtricsResultDTO,
                    OfflineResultDTO = offlineResultDTO
                });
            }

            resultDTO.ResearchDTOs = researchDTOs;
            //resultDTO.VAResultDTOs = vAResultDTOs;
            //resultDTO.QualtricsResultDTOs = qualtricsResultDTOs;
            //resultDTO.OfflineResultDTOs = offlineResultDTOs;

            return View(resultDTO);
        }

        //needs work rework controller for view first
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadFile(string participantPassToken, string sessionPassToken, string researchPassToken)
        {
            if (false) return RedirectToAction("Results", "Project", new { passToken = participantPassToken, sessionPassToken = sessionPassToken });

            var newFileName = string.Empty;
            if (HttpContext.Request.Form.Files != null)
            {
                var fileName = string.Empty;
                string PathDB = string.Empty;
                var files = HttpContext.Request.Form.Files;

                foreach (var file in files)
                {
                    if (file.Length > 0)
                    {
                        fileName = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');
                        var myUniqueFileName = Convert.ToString(Guid.NewGuid());
                        var FileExtension = Path.GetExtension(fileName);
                        //if (!(FileExtension == ".jpg" || FileExtension == ".png") && file.Length > 220000)
                        //    return RedirectToAction("Results", "Project", new { passToken = participantPassToken, sessionPassToken = sessionPassToken });



                        
                        newFileName = myUniqueFileName + FileExtension;
                        fileName = Path.Combine(_environment.WebRootPath, "research-files", "for-upload") + $@"\{newFileName}";
                        using (var stream = new FileStream(fileName, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        bool isUploaded = await ContainerService.UploadOperationAsync(Path.Combine(_environment.WebRootPath, "research-files", "for-upload"),
                            newFileName, AzureGPV2Credentials.CONTAINER_RESEARCH_FILES);

                        if (isUploaded)
                        {
                            OfflineResult offlineResult = _context.OfflineResult.Where(k => k.ParticipantId == int.Parse(_protector.Encode(participantPassToken)) && k.SessionId == int.Parse(_protector.Encode(sessionPassToken)) && k.ResearchId == int.Parse(_protector.Encode(researchPassToken))).FirstOrDefault();
                            if (offlineResult != null)
                            {
                                offlineResult.DocumentUrl = Path.Combine(BaseLink.AZURE_BLOB_URL, AzureGPV2Credentials.CONTAINER_RESEARCH_FILES, newFileName);
                                _context.OfflineResult.Update(offlineResult);
                                _context.SaveChanges();
                            }
                            else
                            {
                                offlineResult = new OfflineResult
                                {
                                    ParticipantId = int.Parse(_protector.Encode(participantPassToken)),
                                    SessionId = int.Parse(_protector.Encode(sessionPassToken)),
                                    ResearchId = int.Parse(_protector.Encode(researchPassToken)),
                                    DocumentUrl = Path.Combine(BaseLink.AZURE_BLOB_URL, AzureGPV2Credentials.CONTAINER_RESEARCH_FILES, newFileName),
                                    Notes = string.Empty
                                };
                                _context.OfflineResult.Add(offlineResult);
                                _context.SaveChanges();
                            }
                        }
                        //DELETE from root
                        if (System.IO.File.Exists(Path.Combine(_environment.WebRootPath, "research-files", "for-upload") + $@"\{newFileName}"))
                            System.IO.File.Delete(Path.Combine(_environment.WebRootPath, "research-files", "for-upload") + $@"\{newFileName}");
                    }
                }
            }
            return RedirectToAction("Results", "Project", new { passToken = participantPassToken, sessionPassToken = sessionPassToken });
        }
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> TakeNote(string participantPassToken, string sessionPassToken, string researchPassToken, string notes)
        {
            if (false) return RedirectToAction("Results", "Project", new { passToken = participantPassToken, sessionPassToken = sessionPassToken });

            if (notes.Length > 0)
            {
                OfflineResult offlineResult = _context.OfflineResult.Where(k => k.ParticipantId == int.Parse(_protector.Encode(participantPassToken)) && k.SessionId == int.Parse(_protector.Encode(sessionPassToken)) && k.ResearchId == int.Parse(_protector.Encode(researchPassToken))).FirstOrDefault();
                if (offlineResult != null)
                {
                    offlineResult.Notes = notes;
                    _context.OfflineResult.Update(offlineResult);
                    _context.SaveChanges();
                }
                else
                {
                    offlineResult = new OfflineResult
                    {
                        ParticipantId = int.Parse(_protector.Encode(participantPassToken)),
                        SessionId = int.Parse(_protector.Encode(sessionPassToken)),
                        ResearchId = int.Parse(_protector.Encode(researchPassToken)),
                        Notes = notes,
                        DocumentUrl = string.Empty
                    };
                    _context.OfflineResult.Add(offlineResult);
                    _context.SaveChanges();
                }

            }


            return RedirectToAction("Results", "Project", new { passToken = participantPassToken, sessionPassToken = sessionPassToken });
        }
        public async Task<IActionResult> TakeSessionNote(string participantPassToken, string sessionPassToken, string notes)
        {
            if (false) return RedirectToAction("Results", "Project", new { passToken = participantPassToken, sessionPassToken = sessionPassToken });

            if (notes.Length > 0)
            {
                SessionParticipant sessionParticipant = _context.SessionParticipant.Where(k => k.ParticipantId == int.Parse(_protector.Encode(participantPassToken)) && k.SessionId == int.Parse(_protector.Encode(sessionPassToken))).FirstOrDefault();
                sessionParticipant.Notes = notes;
                _context.SessionParticipant.Update(sessionParticipant);
                _context.SaveChanges();
            }

            return RedirectToAction("Results", "Project", new { passToken = participantPassToken, sessionPassToken = sessionPassToken });
        }
        public async Task<IActionResult> OfflineComplete(string participantPassToken, string sessionPassToken, string researchPassToken)
        {
            //if (false) return RedirectToAction("Results", "Project", new { passToken = participantPassToken, sessionPassToken = sessionPassToken });

            int test = int.Parse(_protector.Encode(participantPassToken));
            OfflineResult offlineResult = _context.OfflineResult.Where(k => k.ParticipantId == int.Parse(_protector.Encode(participantPassToken)) && k.SessionId == int.Parse(_protector.Encode(sessionPassToken)) && k.ResearchId == int.Parse(_protector.Encode(researchPassToken))).FirstOrDefault();
                if (offlineResult != null)
                {
                    offlineResult.Completed = offlineResult.Completed == 1? (byte)0 : (byte)1;
                    _context.OfflineResult.Update(offlineResult);
                    _context.SaveChanges();
                }
                else
                {
                    offlineResult = new OfflineResult
                    {
                        ParticipantId = int.Parse(_protector.Encode(participantPassToken)),
                        SessionId = int.Parse(_protector.Encode(sessionPassToken)),
                        ResearchId = int.Parse(_protector.Encode(researchPassToken)),
                        Completed = 1,
                        DocumentUrl = string.Empty,
                        Notes = string.Empty
                    };
                    _context.OfflineResult.Add(offlineResult);
                    _context.SaveChanges();
                }


            return RedirectToAction("Participants", "Project", new { passToken = sessionPassToken });
        }
        public async Task<IActionResult> DownloadParticipantData(string participantPassToken, string sessionPassToken)
        {
            try
            {
                ResultDTO resultDTO = new ResultDTO
                {
                    SessionPassToken = sessionPassToken,
                    SessionMD5 = IdentityConversions.MD5Hash(_protector.Encode(sessionPassToken)),
                    ParticipantPassToken = participantPassToken,
                    ParticipantMD5 = IdentityConversions.MD5Hash(_protector.Encode(participantPassToken)),
                    Title = _context.Session.Where(k => k.SessionId == int.Parse(_protector.Encode(sessionPassToken))).FirstOrDefault().Title,
                    Notes = _context.SessionParticipant.Where(k => k.ParticipantId == int.Parse(_protector.Encode(participantPassToken)) && k.SessionId == int.Parse(_protector.Encode(sessionPassToken))).FirstOrDefault().Notes,
                    ParticipantDTO = _context.Participant.Where(k => k.ParticipantId == int.Parse(_protector.Encode(participantPassToken)))
                .Select(x => new ParticipantDTO
                {
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                    Email = x.Email
                }).FirstOrDefault()
                };
                //ResearchMethods Lists

                List<int> researchIds = await _context.ProjectResearch.Where(k => k.ProjectId == _context.ProjectSession.Where(x => x.SessionId == int.Parse(_protector.Encode(sessionPassToken))).FirstOrDefault().ProjectId).Select(k => k.ResearchId).ToListAsync();
                
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10}",
                        "Session:",
                        resultDTO.Title,
                        "Participant:",
                        resultDTO.ParticipantDTO.FirstName,
                        resultDTO.ParticipantDTO.LastName,
                        "EMAIL:",
                        resultDTO.ParticipantDTO.Email,
                        "SessionPassToken",
                        resultDTO.SessionMD5,
                        "ParticipantPassToken",
                        resultDTO.ParticipantMD5));
                foreach (var researchId in researchIds)
                {
                    stringBuilder.AppendLine(string.Empty);
                    Research research = _context.Research.Where(k => k.ResearchId == researchId).FirstOrDefault();                    
                    if (research.ResearchMethodId == (int)ResearchType.VirtualAugmentedReality)
                    {
                        List<VAResultDTO> vAResultDTOs = new List<VAResultDTO>();
                        stringBuilder.AppendLine(string.Format("{0},{1}",                        
                        "ResearchPassToken:",
                        IdentityConversions.MD5Hash(research.ResearchId.ToString())));
                        stringBuilder.AppendLine(string.Format("{0},{1}",
                        research.Title,
                        ResearchType.VirtualAugmentedReality.GetEnumDescription()));
                        //VAR can have more than one entry
                        vAResultDTOs.AddRange(
                            await _context.ParticipantResult.Where(k => k.ResearchId == research.ResearchId && k.ParticipantId == int.Parse(_protector.Encode(participantPassToken)) && k.SessionId == int.Parse(_protector.Encode(sessionPassToken)))
                            .Select(x => new VAResultDTO
                            {                                
                                Time = x.Time,
                                Registered = x.Registered,
                                NoOfMistakes = x.NoOfMistakes,
                                Notes = x.Notes
                            }).ToListAsync());
                        stringBuilder.AppendLine(string.Format("{0},{1},{2},{3}",
                        "Time",
                        "No of Mistakes",
                        "Registered",
                        "Notes"));
                        foreach (var vAResultDTO in vAResultDTOs)
                        {
                            stringBuilder.AppendLine(string.Format("{0},{1},{2},{3}",
                            vAResultDTO.Time,
                            vAResultDTO.NoOfMistakes,
                            vAResultDTO.Registered,
                            vAResultDTO.Notes));
                        }
                    }
                    else if (research.ResearchMethodId == (int)ResearchType.QualtricsQuestionnaire)
                    {
                        QualtricsResultDTO qualtricsResultDTO = new QualtricsResultDTO();
                        stringBuilder.AppendLine(string.Format("{0},{1}",
                       "ResearchPassToken:",
                       IdentityConversions.MD5Hash(research.ResearchId.ToString())));
                        stringBuilder.AppendLine(string.Format("{0},{1}",
                        research.Title,
                        ResearchType.QualtricsQuestionnaire.GetEnumDescription()));
                        qualtricsResultDTO =_context.QualtricsResult.Where(k => k.ResearchId == research.ResearchId && k.ParticipantId == int.Parse(_protector.Encode(participantPassToken)) && k.SessionId == int.Parse(_protector.Encode(sessionPassToken)))
                            .Select(x => new QualtricsResultDTO
                            {
                                Title = research.Title,
                                ResearchPassToken = _protector.Decode(research.ResearchId.ToString()),
                                ResearchMD5 = IdentityConversions.MD5Hash(research.ResearchId.ToString()),
                                ParticipantId = x.ParticipantId,
                                SessionId = x.SessionId,
                                ResearchId = (int)x.ResearchId,
                                DocumentUrl = x.DocumentUrl,
                                //Notes = x.Notes
                            }).FirstOrDefault();
                        stringBuilder.AppendLine(string.Format("{0},{1},{2},{3}",
                        "No",
                        "Available",
                        "Data",
                        "Yet"));
                    }
                    else if (research.ResearchMethodId == (int)ResearchType.OfflineMethod)
                    {
                        OfflineResultDTO offlineResultDTO = new OfflineResultDTO();
                        stringBuilder.AppendLine(string.Format("{0},{1}",
                       "ResearchPassToken:",
                       IdentityConversions.MD5Hash(research.ResearchId.ToString())));
                        stringBuilder.AppendLine(string.Format("{0},{1}",
                        research.Title,
                        ResearchType.OfflineMethod.GetEnumDescription()));
                        offlineResultDTO = _context.OfflineResult.Where(k => k.ResearchId == research.ResearchId && k.ParticipantId == int.Parse(_protector.Encode(participantPassToken)) && k.SessionId == int.Parse(_protector.Encode(sessionPassToken)))
                            .Select(x => new OfflineResultDTO
                            {                                
                                DocumentUrl = x.DocumentUrl != null ? x.DocumentUrl : "",
                                Completed = x.Completed == 1 ? "Yes" : "No"
                                //Notes = x.Notes
                            }).FirstOrDefault();
                        stringBuilder.AppendLine(string.Format("{0},{1}",
                        "Completed",
                        "Document"));
                        if(offlineResultDTO != null)
                            stringBuilder.AppendLine(string.Format("{0},{1}",
                            offlineResultDTO.Completed,
                            offlineResultDTO.DocumentUrl));

                    }
                }         
                return File(Encoding.UTF8.GetBytes(stringBuilder.ToString()), "text/csv", string.Format("{0}_{1}_{2}_{3}.csv", resultDTO.ParticipantDTO.FirstName, resultDTO.ParticipantDTO.LastName, resultDTO.Title, DateTimeOffset.Now.ToUnixTimeSeconds()));
            }
            catch (Exception ex)
            {
                return RedirectToAction("Participants", "Project", new { passToken = sessionPassToken });
            }
        }
        public async Task<IActionResult> DownloadDataCollectiontData(string sessionPassToken)
        {
            try
            {
                List<int> participantids = _context.SessionParticipant.Where(k => k.SessionId == int.Parse(_protector.Encode(sessionPassToken))).Select(x => x.ParticipantId).ToList();
                List<string> fileNames = new List<string>();
                foreach (var participantid in participantids)
                {

                    ResultDTO resultDTO = new ResultDTO
                    {
                        SessionPassToken = sessionPassToken,
                        SessionMD5 = IdentityConversions.MD5Hash(_protector.Encode(sessionPassToken)),
                        ParticipantMD5 = IdentityConversions.MD5Hash(participantid.ToString()),
                        Title = _context.Session.Where(k => k.SessionId == int.Parse(_protector.Encode(sessionPassToken))).FirstOrDefault().Title,
                        Notes = _context.SessionParticipant.Where(k => k.ParticipantId == participantid && k.SessionId == int.Parse(_protector.Encode(sessionPassToken))).FirstOrDefault().Notes,
                        ParticipantDTO = _context.Participant.Where(k => k.ParticipantId == participantid)
                .Select(x => new ParticipantDTO
                {
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                    Email = x.Email
                }).FirstOrDefault()
                    };
                    //ResearchMethods Lists

                    List<int> researchIds = await _context.ProjectResearch.Where(k => k.ProjectId == _context.ProjectSession.Where(x => x.SessionId == int.Parse(_protector.Encode(sessionPassToken))).FirstOrDefault().ProjectId).Select(k => k.ResearchId).ToListAsync();

                    StringBuilder stringBuilder = new StringBuilder();
                    stringBuilder.AppendLine(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10}",
                            "Session:",
                            resultDTO.Title,
                            "Participant:",
                            resultDTO.ParticipantDTO.FirstName,
                            resultDTO.ParticipantDTO.LastName,
                            "EMAIL:",
                            resultDTO.ParticipantDTO.Email,
                            "SessionPassToken",
                            resultDTO.SessionMD5,
                            "ParticipantPassToken",
                            resultDTO.ParticipantMD5));
                    
                    foreach (var researchId in researchIds)
                    {
                        stringBuilder.AppendLine(string.Empty);
                        Research research = _context.Research.Where(k => k.ResearchId == researchId).FirstOrDefault();
                        if (research.ResearchMethodId == (int)ResearchType.VirtualAugmentedReality)
                        {
                            List<VAResultDTO> vAResultDTOs = new List<VAResultDTO>();
                            stringBuilder.AppendLine(string.Format("{0},{1}",
                            "ResearchPassToken:",
                            IdentityConversions.MD5Hash(research.ResearchId.ToString())));
                            stringBuilder.AppendLine(string.Format("{0},{1}",
                            research.Title,
                            ResearchType.VirtualAugmentedReality.GetEnumDescription()));
                            //VAR can have more than one entry
                            vAResultDTOs.AddRange(
                                await _context.ParticipantResult.Where(k => k.ResearchId == research.ResearchId && k.ParticipantId == participantid && k.SessionId == int.Parse(_protector.Encode(sessionPassToken)))
                                .Select(x => new VAResultDTO
                                {
                                    Time = x.Time,
                                    Registered = x.Registered,
                                    NoOfMistakes = x.NoOfMistakes,
                                    Notes = x.Notes
                                }).ToListAsync());
                            stringBuilder.AppendLine(string.Format("{0},{1},{2},{3}",
                            "Time",
                            "No of Mistakes",
                            "Registered",
                            "Notes"));
                            foreach (var vAResultDTO in vAResultDTOs)
                            {
                                stringBuilder.AppendLine(string.Format("{0},{1},{2},{3}",
                                vAResultDTO.Time,
                                vAResultDTO.NoOfMistakes,
                                vAResultDTO.Registered,
                                vAResultDTO.Notes));
                            }
                        }
                        else if (research.ResearchMethodId == (int)ResearchType.QualtricsQuestionnaire)
                        {
                            QualtricsResultDTO qualtricsResultDTO = new QualtricsResultDTO();
                            stringBuilder.AppendLine(string.Format("{0},{1}",
                           "ResearchPassToken:",
                           IdentityConversions.MD5Hash(research.ResearchId.ToString())));
                            stringBuilder.AppendLine(string.Format("{0},{1}",
                            research.Title,
                            ResearchType.QualtricsQuestionnaire.GetEnumDescription()));
                            qualtricsResultDTO = _context.QualtricsResult.Where(k => k.ResearchId == research.ResearchId && k.ParticipantId == participantid && k.SessionId == int.Parse(_protector.Encode(sessionPassToken)))
                                .Select(x => new QualtricsResultDTO
                                {
                                    Title = research.Title,
                                    ResearchPassToken = _protector.Decode(research.ResearchId.ToString()),
                                    ResearchMD5 = IdentityConversions.MD5Hash(research.ResearchId.ToString()),
                                    ParticipantId = x.ParticipantId,
                                    SessionId = x.SessionId,
                                    ResearchId = (int)x.ResearchId,
                                    DocumentUrl = x.DocumentUrl,
                                //Notes = x.Notes
                            }).FirstOrDefault();
                            stringBuilder.AppendLine(string.Format("{0},{1},{2},{3}",
                            "No",
                            "Available",
                            "Data",
                            "Yet"));
                        }
                        else if (research.ResearchMethodId == (int)ResearchType.OfflineMethod)
                        {
                            OfflineResultDTO offlineResultDTO = new OfflineResultDTO();
                            stringBuilder.AppendLine(string.Format("{0},{1}",
                           "ResearchPassToken:",
                           IdentityConversions.MD5Hash(research.ResearchId.ToString())));
                            stringBuilder.AppendLine(string.Format("{0},{1}",
                            research.Title,
                            ResearchType.OfflineMethod.GetEnumDescription()));
                            offlineResultDTO = _context.OfflineResult.Where(k => k.ResearchId == research.ResearchId && k.ParticipantId == participantid && k.SessionId == int.Parse(_protector.Encode(sessionPassToken)))
                                .Select(x => new OfflineResultDTO
                                {
                                    DocumentUrl = x.DocumentUrl != null ? x.DocumentUrl : "",
                                    Completed = x.Completed == 1 ? "Yes" : "No"
                                //Notes = x.Notes
                            }).FirstOrDefault();
                            stringBuilder.AppendLine(string.Format("{0},{1}",
                            "Completed",
                            "Document"));
                            if (offlineResultDTO != null)
                                stringBuilder.AppendLine(string.Format("{0},{1}",
                                offlineResultDTO.Completed,
                                offlineResultDTO.DocumentUrl));
                        }
                    }

                    var bytes = Encoding.UTF8.GetBytes(stringBuilder.ToString());                    
                    var newFileName = string.Format("{0}_{1}_{2}_{3}.csv", resultDTO.ParticipantDTO.FirstName, resultDTO.ParticipantDTO.LastName, resultDTO.Title, DateTimeOffset.Now.ToUnixTimeSeconds());
                    var fileName = string.Empty;
                    fileName = Path.Combine(_environment.WebRootPath, "research-files", "for-download") + $@"\{newFileName}";
                    System.IO.File.WriteAllBytes(fileName, bytes);
                    fileNames.Add(fileName);
                }

                var zipFileName = string.Format("{0}_{1}.zip", _context.Session.Where(k => k.SessionId == int.Parse(_protector.Encode(sessionPassToken))).FirstOrDefault().Title , DateTimeOffset.Now.ToUnixTimeSeconds());
                var zipFilePath = string.Empty;
                zipFilePath = Path.Combine(_environment.WebRootPath, "research-files", "for-download") + $@"\{zipFileName}";

                using (ZipArchive zipFile = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
                {
                    foreach (string file in fileNames)
                    {
                        zipFile.CreateEntryFromFile(file,Path.GetFileName(file));
                    }                    
                }
                byte[] zipBytes; //= new byte[filestream.Length];
                using (FileStream filestream = new FileStream(zipFilePath, FileMode.Open))
                {
                    zipBytes = new byte[filestream.Length];
                    filestream.Read(zipBytes, 0, zipBytes.Length);

                    //DELETE from root
                    foreach (string file in fileNames)
                    {
                        if (System.IO.File.Exists(file))
                            System.IO.File.Delete(file);
                    }                    
                }
                if (System.IO.File.Exists(zipFilePath))
                    System.IO.File.Delete(zipFilePath);

                return File(zipBytes, "application/zip", zipFileName);
            }
            catch (Exception ex)
            {
                return RedirectToAction("Participants", "Project", new { passToken = sessionPassToken });
            }
        }
    }    
}
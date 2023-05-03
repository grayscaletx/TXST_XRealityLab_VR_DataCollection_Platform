using Application.Security;
using Common;
using Common.Helpers;
using Infrastructure.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using XRealityLab.API;
using XRealityLab.Common;


namespace XRealityLabAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectController : ControllerBase
    {
        private readonly XRealityLabdbContext _context;
        private readonly IConfiguration _configuration;
        private readonly CustomIDataProtection _protector;

        public ProjectController(XRealityLabdbContext context, IConfiguration configuration, CustomIDataProtection customIDataProtection)
        {
            _context = context;
            _configuration = configuration;
            _protector = customIDataProtection;

        }

        // GET: /api/Project/GetMyProjects?_authToken={authToken}
        [HttpGet("GetMyProjects")]
        public async Task<ActionResult<IEnumerable<ProjectAPI>>> GetMyProjects(string _authToken)
        {
            if (!await _context.UserAuthToken.AnyAsync(k => k.AuthToken == _authToken)) return NotFound(MessageConverter.GetResponse("AuthToken is incorrect", MethodResult.Failed));

            var authToken = await _context.UserAuthToken.Where(k => k.AuthToken == _authToken).FirstOrDefaultAsync();
            List<ProjectAPI> projects = _context.ProjectUser.Include(k => k.Project).Where(k => k.UserId == authToken.UserId && (k.RoleId == 4 || k.RoleId == 5))
                .Select(k => new ProjectAPI
                {
                    ProjectId = k.Project.ProjectId,
                    FullName = k.Project.FullName,
                    Description = k.Project.Description,
                    Registered = k.Project.Registered
                }).ToList();
            foreach (var project in projects)
            {
                List<UserAPI> users = _context.ProjectUser.Include(k => k.User).Where(k => k.ProjectId == project.ProjectId && k.RoleId == 4)
                .Select(k => new UserAPI
                {
                    FirstName = k.User.Firstname,
                    LastName = k.User.Lastname,
                }).ToList();
                project.projectPIs = users;
            }

            return projects;
        }
        // GET: /api/Project/GetDataCollections?_authToken={authToken}&projectId={projectId}
        [HttpGet("GetDataCollections")]
        public async Task<ActionResult<IEnumerable<DataCollectionAPI>>> GetDataCollections(string _authToken, int projectId)
        {
            if (!await _context.UserAuthToken.AnyAsync(k => k.AuthToken == _authToken)) 
                return NotFound(MessageConverter.GetResponse("AuthToken is incorrect", MethodResult.Failed));            

            var authToken = await _context.UserAuthToken.Where(k => k.AuthToken == _authToken).FirstOrDefaultAsync();
            if (!await _context.ProjectUser.Where(k => k.UserId == authToken.UserId && k.ProjectId == projectId && (k.RoleId == 4 || k.RoleId == 5)).AnyAsync())
                return NotFound(MessageConverter.GetResponse("Access denied to this project", MethodResult.Failed));

            List<DataCollectionAPI> dataCollections = _context.ProjectSession.Include(k => k.Session).Where(k => k.ProjectId == projectId)
                .Select(k => new DataCollectionAPI
                {
                    SessionId = k.Session.SessionId,
                    Title = k.Session.Title,
                    Description = k.Session.Description,
                    Registered = k.Session.Registered,
                    participantsNo = _context.SessionParticipant.Where(k => k.SessionId == k.Session.SessionId).Count()
                }).ToList();
            

            return dataCollections;
        }
        // GET: /api/Project/GetParticipants?_authToken={authToken}&sessionId={participantId}
        [HttpGet("GetParticipants")]
        public async Task<ActionResult<IEnumerable<ParticipantsAPI>>> GetParticipants(string _authToken, int sessionId)
        {
            if (!await _context.UserAuthToken.AnyAsync(k => k.AuthToken == _authToken))
                return NotFound(MessageConverter.GetResponse("AuthToken is incorrect", MethodResult.Failed));

            var authToken = await _context.UserAuthToken.Where(k => k.AuthToken == _authToken).FirstOrDefaultAsync();
            if (!await _context.ProjectUser.Where(k => k.UserId == authToken.UserId && k.ProjectId == _context.ProjectSession.Where(x => x.SessionId == sessionId).FirstOrDefault().SessionId && (k.RoleId == 4 || k.RoleId == 5)).AnyAsync())
                return NotFound(MessageConverter.GetResponse("Access denied to this session", MethodResult.Failed));

            List<ParticipantsAPI> participants = _context.SessionParticipant.Include(k => k.Participant).Where(k => k.SessionId == sessionId)
                .Select(k => new ParticipantsAPI
                {
                    ParticipantId = k.Participant.ParticipantId,
                    FirstName = k.Participant.FirstName,
                    LastName = k.Participant.LastName,
                    Email = k.Participant.Email,
                    Registered = k.Session.Registered,
                }).ToList();


            return participants;
        }
        [HttpPost("ParticipantResult")]
        public async Task<ActionResult<ResponseDTO>> PostParticipantResult(string _authToken, ParticipantResultAPI _participantResultAPI)
        {
            try
            {

                //FIND the IDs from the hashes
                int participantId = _context.Participant.ToList().Where(k => IdentityConversions.MD5Hash(k.ParticipantId.ToString()) == _participantResultAPI.ParticipantMD5).FirstOrDefault().ParticipantId;
                int sessionId = _context.Session.ToList().Where(k => IdentityConversions.MD5Hash(k.SessionId.ToString()) == _participantResultAPI.SessionMD5).FirstOrDefault().SessionId;
                int researchId = _context.Research.ToList().Where(k => IdentityConversions.MD5Hash(k.ResearchId.ToString()) == _participantResultAPI.ResearchMD5).FirstOrDefault().ResearchId;

                if (!await _context.UserAuthToken.AnyAsync(k => k.AuthToken == _authToken))
                    return NotFound(MessageConverter.GetResponse("AuthToken is incorrect", MethodResult.Failed));

                /*if (_participantResultAPI.ParticipantId == null || _participantResultAPI.SessionId == null || _participantResultAPI.ResearchId == null || _participantResultAPI.NoOfMistakes == null || _participantResultAPI.Time == null)
                    return NotFound(MessageConverter.GetResponse("Invalid Data", MethodResult.Failed));*/
                if (!await _context.SessionParticipant.Where(k => k.ParticipantId == participantId  &&
                k.SessionId == sessionId).AnyAsync())
                    return NotFound(MessageConverter.GetResponse("Participant session combination incorrect", MethodResult.Failed));

                //check if i am allowed to write in this session project and if research is of this project {maybe check type}
                //Get userId PI or RA
                int userId = _context.UserAuthToken.Where(k => k.AuthToken == _authToken).FirstOrDefault().UserId;
                //Get projectId
                int projectId = _context.ProjectSession.Where(k => k.SessionId == sessionId).FirstOrDefault().ProjectId;
                if (!await _context.ProjectUser.Where(k => k.ProjectId == projectId && k.UserId == userId && ( k.RoleId == (int)RoleType.PrincipleInvestigator || k.RoleId == (int)RoleType.ResearchAssistant)).AnyAsync())
                    return NotFound(MessageConverter.GetResponse("No access to this session", MethodResult.Failed));

                //check researchId
                if (!await _context.ProjectResearch.Where(k => k.ProjectId == projectId && k.ResearchId == researchId).AnyAsync())
                    return NotFound(MessageConverter.GetResponse("No access to this research", MethodResult.Failed));

                //check research type
                if (!await _context.Research.Where(k => k.ResearchId == researchId && k.ResearchMethodId == (int)ResearchType.VirtualAugmentedReality).AnyAsync())
                    return NotFound(MessageConverter.GetResponse("No Virtual or Augmented reality research with this pass token", MethodResult.Failed));


                //check if today have sent more than 50 results
                if (_context.ParticipantResult.Where(k => k.SessionId == sessionId && k.Registered > DateTime.Today).Count() >= 50)
                    return NotFound(MessageConverter.GetResponse("Too many post request for this session today", MethodResult.Failed));

                ParticipantResult participantResult = new ParticipantResult
                {
                    ParticipantId = participantId,
                    SessionId = sessionId,
                    ResearchId = researchId,
                    Time = _participantResultAPI.Time,
                    NoOfMistakes = _participantResultAPI.NoOfMistakes,
                    Registered = DateTime.Now,
                    Notes = _participantResultAPI.Notes
                };

                _context.ParticipantResult.Add(participantResult);
                _context.SaveChanges();

                return new ActionResult<ResponseDTO>(MessageConverter.GetResponse("Saved Data", MethodResult.Succeeded));
            }
            catch (Exception ex)
            {
                return NotFound(MessageConverter.GetResponse("Failed to save the data.", MethodResult.Failed));
            }

        }
        [HttpPost("QualtricsResult")]
        public async Task<ActionResult<ResponseDTO>> PostQualtricsResult(string _authToken, QualtricsResultAPI _qualtricsResultAPI)
        {
            try
            {

                //FIND the IDs from the hashes
                int participantId = _context.Participant.ToList().Where(k => IdentityConversions.MD5Hash(k.ParticipantId.ToString()) == _qualtricsResultAPI.ParticipantMD5).FirstOrDefault().ParticipantId;
                int sessionId = _context.Session.ToList().Where(k => IdentityConversions.MD5Hash(k.SessionId.ToString()) == _qualtricsResultAPI.SessionMD5).FirstOrDefault().SessionId;
                int researchId = _context.Research.ToList().Where(k => IdentityConversions.MD5Hash(k.ResearchId.ToString()) == _qualtricsResultAPI.ResearchMD5).FirstOrDefault().ResearchId;

                if (!await _context.UserAuthToken.AnyAsync(k => k.AuthToken == _authToken))
                    return NotFound(MessageConverter.GetResponse("AuthToken is incorrect", MethodResult.Failed));

                /*if (_participantResultAPI.ParticipantId == null || _participantResultAPI.SessionId == null || _participantResultAPI.ResearchId == null || _participantResultAPI.NoOfMistakes == null || _participantResultAPI.Time == null)
                    return NotFound(MessageConverter.GetResponse("Invalid Data", MethodResult.Failed));*/
                if (!await _context.SessionParticipant.Where(k => k.ParticipantId == participantId &&
                k.SessionId == sessionId).AnyAsync())
                    return NotFound(MessageConverter.GetResponse("Participant session combination incorrect", MethodResult.Failed));

                //check if i am allowed to write in this session project and if research is of this project {maybe check type}
                //Get userId PI or RA
                int userId = _context.UserAuthToken.Where(k => k.AuthToken == _authToken).FirstOrDefault().UserId;
                //Get projectId
                int projectId = _context.ProjectSession.Where(k => k.SessionId == sessionId).FirstOrDefault().ProjectId;
                if (!await _context.ProjectUser.Where(k => k.ProjectId == projectId && k.UserId == userId && (k.RoleId == (int)RoleType.PrincipleInvestigator || k.RoleId == (int)RoleType.ResearchAssistant)).AnyAsync())
                    return NotFound(MessageConverter.GetResponse("No access to this session", MethodResult.Failed));

                //check researchId
                if (!await _context.ProjectResearch.Where(k => k.ProjectId == projectId && k.ResearchId == researchId).AnyAsync())
                    return NotFound(MessageConverter.GetResponse("No access to this research", MethodResult.Failed));

                //check research type
                if (!await _context.Research.Where(k => k.ResearchId == researchId && k.ResearchMethodId == (int)ResearchType.QualtricsQuestionnaire).AnyAsync())
                    return NotFound(MessageConverter.GetResponse("No Virtual or Augmented reality research with this pass token", MethodResult.Failed));


                //check if today have sent more than 50 results
                if (_context.ParticipantResult.Where(k => k.SessionId == sessionId && k.Registered > DateTime.Today).Count() >= 50)
                    return NotFound(MessageConverter.GetResponse("Too many post request for this session today", MethodResult.Failed));

                QualtricsResult qualtricsResult = new QualtricsResult
                {
                    ParticipantId = participantId,
                    SessionId = sessionId,
                    ResearchId = researchId,
                    DocumentUrl = _qualtricsResultAPI.DocumentUrl,
                    Notes = _qualtricsResultAPI.Notes
                };

                _context.QualtricsResult.Add(qualtricsResult);
                _context.SaveChanges();

                return new ActionResult<ResponseDTO>(MessageConverter.GetResponse("Saved Data", MethodResult.Succeeded));
            }
            catch (Exception ex)
            {
                return NotFound(MessageConverter.GetResponse("Failed to save the data.", MethodResult.Failed));
            }

        }
    }
}

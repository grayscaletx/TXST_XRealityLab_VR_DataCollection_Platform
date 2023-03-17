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

        public ProjectController(XRealityLabdbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // GET: api/Project/MyProjects
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
        // GET: api/Project/DataCollections
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
        // GET: api/Project/Participants
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
                if (!await _context.UserAuthToken.AnyAsync(k => k.AuthToken == _authToken))
                    return NotFound(MessageConverter.GetResponse("AuthToken is incorrect", MethodResult.Failed));

                if (!await _context.SessionParticipant.Where(k => k.ParticipantId == _participantResultAPI.ParticipantId && k.SessionId == _participantResultAPI.SessionId).AnyAsync())
                    return NotFound(MessageConverter.GetResponse("Participant session combination incorrect", MethodResult.Failed));

                ParticipantResult participantResult = new ParticipantResult
                {
                    ParticipantId = _participantResultAPI.ParticipantId,
                    SessionId = _participantResultAPI.SessionId,
                    Time = _participantResultAPI.Time,
                    NoOfMistakes = _participantResultAPI.NoOfMistakes,
                    Registered = _participantResultAPI.Registered,
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
    }
}

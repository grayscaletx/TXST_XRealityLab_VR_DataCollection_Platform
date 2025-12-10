using Application.Security;
using Common;
using Common.Constants;
using Common.Helpers;
using Infrastructure.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using XRealityLab.Common;
using XRealityLab.Models;

namespace XRealityLab
{
    public class Migration
    {
        public static async Task<User> ToUser(XRealityLabdbContext _context, UserDTO _userDTO)
        {
            try
            {
                return new User
                {
                    IdentityId = Guid.NewGuid().ToString(),
                    Rank = _userDTO.Rank,
                    Firstname = _userDTO.Firstname.Trim(),
                    Lastname = _userDTO.Lastname.Trim(),
                    PasswordHash = IdentityConversions.MD5Hash(_userDTO.Password),
                    Email = _userDTO.Email.Trim(),
                    RoleId = _userDTO.RoleId,
                    Telephone = _userDTO.Telephone,
                    HasChangedPassword = (byte)(_userDTO.IsActive ? 1 : 0),
                    PhotoUrl = GetUserPhotoUrl((RoleType)_userDTO.RoleId),
                    RegistrationDateTime = DateTime.Now,
                };
            }
            catch
            {
                await _context.SaveChangesAsync();
                return new User();
            }
        }
        public static string GetUserPhotoUrl(RoleType roleType)
        {
            string photoUrl = string.Empty;
            switch (roleType)
            {
                case RoleType.DirectorOfIT:
                    photoUrl = PhotoUrl.DIRECTOR_OF_IT_ICON_PNG;
                    break;
                case RoleType.LabDirector:
                    photoUrl = PhotoUrl.LAB_DIRECTOR_ICON_PNG;
                    break;
                case RoleType.PrincipleInvestigator:
                    photoUrl = PhotoUrl.PRINCIPLE_INVESTIGATOR_ICON_PNG;
                    break;
                default:
                    photoUrl = PhotoUrl.RESEARCH_ASSISTANT_ICON_PNG;
                    break;
            }
            return photoUrl;
        }

        public static async Task<ResponseDTO> InsertUserAsync(XRealityLabdbContext _context,User _user)
        {
            try
            {
                await _context.AddAsync(_user);
                await _context.SaveChangesAsync();
                return MessageConverter.GetResponse(string.Empty, MethodResult.Succeeded);
            }
            catch (Exception ex)
            {
                return MessageConverter.GetResponse(string.Empty, MethodResult.Failed);
            }
        }

        public static async Task<ResponseDTO> RemoveOrganizationDirectorAsync(
    XRealityLabdbContext _context,
    UserManager<IdentityUser> _userManager,
    Organization _organization)
        {
            try
            {
                Organization organization = _context.Organization
                    .FirstOrDefault(k => k.OrganizationId == _organization.OrganizationId);
                User user = _context.User
                    .FirstOrDefault(k => k.UserId == _organization.ItdirectorId);

                var identityUser = await _userManager.FindByEmailAsync(user.Email);
                if (identityUser != null)
                {
                    var userManagerResult = await _userManager.DeleteAsync(identityUser);
                    if (!userManagerResult.Succeeded)
                        return MessageConverter.GetResponse(
                            "Failed to delete identity user.",
                            MethodResult.Failed);
                }

                var labIds = await _context.OrganizationLab
                    .Where(k => k.OrganizationId == organization.OrganizationId)
                    .Select(k => k.LabId)
                    .ToListAsync();

                foreach (int id in labIds)
                {
                    Lab lab = _context.Lab.Find(id);
                    await RemoveLabDirectorAsync(_context, _userManager, lab);
                }

                _context.Remove(organization);
                _context.Remove(user);

                await _context.SaveChangesAsync();
                return MessageConverter.GetResponse(string.Empty, MethodResult.Succeeded);
            }
            catch (Exception ex)
            {
                return new ResponseDTO
                {
                    Result = MethodResult.Failed.ToString(),
                    Message = $"RemoveOrganizationDirectorAsync ERROR → {ex.Message} | INNER → {ex.InnerException?.Message}"
                };
            }
        }



        public static async Task<ResponseDTO> RemoveLabDirectorAsync(XRealityLabdbContext _context, UserManager<IdentityUser> _userManager, Lab _lab)
        {
            try
            {
                Lab lab = _context.Lab.Where(k => k.LabId == _lab.LabId).FirstOrDefault();
                OrganizationLab organizationLab = _context.OrganizationLab.Where(k => k.LabId == _lab.LabId).FirstOrDefault();
                User user = _context.User.Where(k => k.UserId == _lab.LabDirectorId).FirstOrDefault();
                LabUser labUser = _context.LabUser.Where(k => k.LabId == lab.LabId && k.UserId == user.UserId).FirstOrDefault();


                var identityUser = await _userManager.FindByEmailAsync(user.Email);
                if (identityUser != null)
                {
                    var userManagerResult = await _userManager.DeleteAsync(identityUser);
                    if (!userManagerResult.Succeeded)
                        return MessageConverter.GetResponse(string.Empty, MethodResult.Failed);
                }
                List<int> projectIds = new List<int>();
                projectIds = _context.ProjectUser.Where(k => (k.UserId == user.UserId && k.RoleId == 3)).Select(k => k.ProjectId).ToList();
                foreach (var projectId in projectIds)
                {
                    Project project = _context.Project.Where(k => k.ProjectId == projectId).FirstOrDefault();
                    await RemoveProjectAsync(_context, project);
                }
                List<int> labUserIds = await _context.LabUser.Where(k => k.LabId == lab.LabId && (k.RoleId == 4 || k.RoleId == 5)).Select(k => k.UserId).ToListAsync();
                foreach (int labUserId in labUserIds)
                {
                    User labPerson = _context.User.Where(k => k.UserId == labUserId).FirstOrDefault();
                    if (labPerson.RoleId == 4)
                    {
                        await RemovePrincipleInvestigator(_context, _userManager, labPerson);
                    }
                    else if (labPerson.RoleId == 5)
                    {
                        await RemoveResearchAssistant(_context, _userManager, labPerson);
                    }
                }
                _context.Remove(organizationLab);
                _context.Remove(labUser);
                _context.Remove(lab);
                _context.Remove(user);

                await _context.SaveChangesAsync();
                return MessageConverter.GetResponse(string.Empty, MethodResult.Succeeded);
            }
            catch (Exception ex)
            {
                return MessageConverter.GetResponse(string.Empty, MethodResult.Failed);
            }
        }

        public static async Task<ResponseDTO> RemoveProjectAsync(XRealityLabdbContext _context, Project _project)
        {
            try
            {
                Project project = _context.Project.Where(k => k.ProjectId == _project.ProjectId).FirstOrDefault();
                List<ProjectUser> projectUsers = new List<ProjectUser>();
                projectUsers = await _context.ProjectUser.Where(k => k.ProjectId == project.ProjectId).ToListAsync();
                List<ProjectResearch> projectResearches = await _context.ProjectResearch.Where(k => k.ProjectId == project.ProjectId).ToListAsync();
                List<Research> researches = new List<Research>();
                List<ProjectSession> projectSessions = await _context.ProjectSession.Where(k => k.ProjectId == project.ProjectId).ToListAsync();
                List<Session> sessions = new List<Session>();
                foreach (var projectResearch in projectResearches)
                {
                    researches.Add(_context.Research.Where(k => k.ResearchId == projectResearch.ResearchId).FirstOrDefault());
                }
                foreach (var projectSession in projectSessions)
                {
                    sessions.Add(_context.Session.Where(k => k.SessionId == projectSession.SessionId).FirstOrDefault());
                }
                _context.ProjectSession.RemoveRange(projectSessions);
                foreach (var session in sessions)
                {
                    ResponseDTO responseDTO = await RemoveSessionAsync(_context, session);
                }
                _context.ProjectResearch.RemoveRange(projectResearches);
                _context.Research.RemoveRange(researches);
                _context.ProjectUser.RemoveRange(projectUsers);
                _context.Project.Remove(project);
                await _context.SaveChangesAsync();
                return MessageConverter.GetResponse(string.Empty, MethodResult.Succeeded);
            }
            catch (Exception ex)
            {
                return MessageConverter.GetResponse(string.Empty, MethodResult.Failed);
            }
        }
        public static async Task<ResponseDTO> RemovePrincipleInvestigator(XRealityLabdbContext _context, UserManager<IdentityUser> _userManager, User _user)
        {
            try
            {
                User user = _context.User.Where(k => k.UserId == _user.UserId).FirstOrDefault();

                //This is mandatory
                //START
                var identityUser = await _userManager.FindByEmailAsync(user.Email);
                if (identityUser != null)
                {
                    var userManagerResult = await _userManager.DeleteAsync(identityUser);
                    if (!userManagerResult.Succeeded)
                        return MessageConverter.GetResponse(string.Empty, MethodResult.Failed);
                }
                //END

                List<ProjectUser> projectUsers = new List<ProjectUser>();
                projectUsers = await _context.ProjectUser.Where(k => k.UserId == user.UserId && k.RoleId == 4).ToListAsync();
                LabUser labUser = _context.LabUser.Where(k => k.UserId == user.UserId && k.RoleId == 4).FirstOrDefault();
                _context.LabUser.Remove(labUser);
                _context.ProjectUser.RemoveRange(projectUsers);
                _context.User.Remove(user);
                await _context.SaveChangesAsync();
                return MessageConverter.GetResponse(string.Empty, MethodResult.Succeeded);
            }
            catch (Exception ex)
            {
                return MessageConverter.GetResponse(string.Empty, MethodResult.Failed);
            }
        }
        public static async Task<ResponseDTO> RemoveResearchAssistant(XRealityLabdbContext _context, UserManager<IdentityUser> _userManager, User _user)
        {
            try
            {
                User user = _context.User.Where(k => k.UserId == _user.UserId).FirstOrDefault();

                //This is mandatory
                //START
                var identityUser = await _userManager.FindByEmailAsync(user.Email);
                if (identityUser != null)
                {
                    var userManagerResult = await _userManager.DeleteAsync(identityUser);
                    if (!userManagerResult.Succeeded)
                        return MessageConverter.GetResponse(string.Empty, MethodResult.Failed);
                }
                //END

                List<ProjectUser> projectUsers = new List<ProjectUser>();
                projectUsers = await _context.ProjectUser.Where(k => k.UserId == user.UserId && k.RoleId == 5).ToListAsync();
                LabUser labUser = _context.LabUser.Where(k => k.UserId == user.UserId && k.RoleId == 5).FirstOrDefault();
                _context.LabUser.Remove(labUser);
                _context.ProjectUser.RemoveRange(projectUsers);
                _context.User.Remove(user);
                await _context.SaveChangesAsync();
                return MessageConverter.GetResponse(string.Empty, MethodResult.Succeeded);
            }
            catch (Exception ex)
            {
                return MessageConverter.GetResponse(string.Empty, MethodResult.Failed);
            }
        }
        public static async Task<ResponseDTO> RemoveSessionAsync(
    XRealityLabdbContext _context, Session _session)
        {
            try
            {
                var session = _context.Session
                    .FirstOrDefault(k => k.SessionId == _session.SessionId);

                // ΠΡΕΠΕΙ ΝΑ ΠΑΡΟΥΜΕ ΤΟΥΣ PARTICIPANT IDs
                var participantIds = _context.SessionParticipant
                    .Where(k => k.SessionId == _session.SessionId)
                    .Select(k => k.ParticipantId)
                    .ToList();

                foreach (var participantId in participantIds)
                {
                    var participant = _context.Participant
                        .FirstOrDefault(k => k.ParticipantId == participantId);

                    await RemoveParticipantAsync(_context, _session, participant);
                }

                var projectSession = _context.ProjectSession
                    .FirstOrDefault(k => k.SessionId == _session.SessionId);
                if (projectSession != null)
                    _context.ProjectSession.Remove(projectSession);

                _context.Session.Remove(session);

                await _context.SaveChangesAsync();
                return MessageConverter.GetResponse("", MethodResult.Succeeded);
            }
            catch (Exception ex)
            {
                return MessageConverter.GetResponse(
                    $"RemoveSessionAsync ERROR → {ex.Message} | INNER → {ex.InnerException?.Message}",
                    MethodResult.Failed);
            }
        }

        public static async Task<ResponseDTO> RemoveParticipantAsync(XRealityLabdbContext _context, Session _session, Participant _participant)
        {
            try
            {
                SessionParticipant sessionParticipant = _context.SessionParticipant.Where(k => k.ParticipantId == _participant.ParticipantId && k.SessionId == _session.SessionId).FirstOrDefault();

                //DELETE participant results for this session.
                // table ParticipantResult
                List<ParticipantResult> participantResults = _context.ParticipantResult.Where(k => k.ParticipantId == _participant.ParticipantId && k.SessionId == _session.SessionId).ToList();
                // table OfflineResult
                List<OfflineResult> offlineResults = _context.OfflineResult.Where(k => k.ParticipantId == _participant.ParticipantId && k.SessionId == _session.SessionId).ToList();
                //QualtricsResult
                List<QualtricsResult> qualtricsResults = _context.QualtricsResult.Where(k => k.ParticipantId == _participant.ParticipantId && k.SessionId == _session.SessionId).ToList();

                _context.ParticipantResult.RemoveRange(participantResults);
                _context.OfflineResult.RemoveRange(offlineResults);
                _context.QualtricsResult.RemoveRange(qualtricsResults);
                _context.SessionParticipant.Remove(sessionParticipant);

                //Check if Participant doesnt exist in other Data Collections and delete
                if (!_context.SessionParticipant.Where(k => k.ParticipantId == _participant.ParticipantId && k.SessionId != _session.SessionId).Any())
                {
                    Participant participant = _context.Participant.Where(k => k.ParticipantId == _participant.ParticipantId).FirstOrDefault();
                    _context.Participant.Remove(participant);
                }

                await _context.SaveChangesAsync();
                return MessageConverter.GetResponse(string.Empty, MethodResult.Succeeded);
            }
            catch (Exception ex)
            {
                return MessageConverter.GetResponse(string.Empty, MethodResult.Failed);
            }
        }
    }

    public class IdentityMigration
    {
        public static async Task<IdentityResult> CreateIdentityUserAsync(UserManager<IdentityUser> _userManager, string email, string password)
        {
            var identityUser = new IdentityUser
            {
                Email = email,
                UserName = email
            };

            var code = _userManager.GenerateEmailConfirmationTokenAsync(identityUser).Result;
            var result = await _userManager.ConfirmEmailAsync(identityUser, code);

            return await _userManager.CreateAsync(identityUser, password);
        }
    }
}

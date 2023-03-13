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
                    Firstname = _userDTO.Firstname.Trim(),
                    Lastname = _userDTO.Lastname.Trim(),
                    PasswordHash = IdentityConversions.MD5Hash(_userDTO.Password),
                    Email = _userDTO.Email.Trim(),
                    RoleId = _userDTO.RoleId,
                    Telephone = _userDTO.Telephone,
                    IsActive = (byte)(_userDTO.IsActive ? 1 : 0),
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
                    photoUrl = PhotoUrl.PRINCIPLE_INVESTIGATOR_OF_IT_ICON_PNG;
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

        public static async Task<ResponseDTO> RemoveOrganizationDirectorAsync(XRealityLabdbContext _context, UserManager<IdentityUser> _userManager, Organization _organization)
        {
            try
            {
                Organization organization = _context.Organization.Where(k => k.OrganizationId == _organization.OrganizationId).FirstOrDefault();
                User user = _context.User.Where(k => k.UserId == _organization.ItdirectorId).FirstOrDefault();

                var identityUser = await _userManager.FindByEmailAsync(user.Email);
                if (identityUser != null)
                {
                    var userManagerResult = await _userManager.DeleteAsync(identityUser);
                    if (!userManagerResult.Succeeded)
                        return MessageConverter.GetResponse(string.Empty, MethodResult.Failed);
                }

                List<int> labIds = new List<int>();
                labIds = await _context.OrganizationLab.Where(k => k.OrganizationId == organization.OrganizationId).Select(k => k.LabId).ToListAsync();
                foreach(int id in labIds)
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
                return MessageConverter.GetResponse(string.Empty, MethodResult.Failed);
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
                        await RemovePrincipleInvestigator(_context, labPerson);
                    }
                    else if (labPerson.RoleId == 5)
                    {
                        await RemoveResearchAssistant(_context, labPerson);
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
                _context.Session.RemoveRange(sessions);
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
        public static async Task<ResponseDTO> RemovePrincipleInvestigator(XRealityLabdbContext _context, User _user)
        {
            try
            {
                User user = _context.User.Where(k => k.UserId == _user.UserId).FirstOrDefault();
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
        public static async Task<ResponseDTO> RemoveResearchAssistant(XRealityLabdbContext _context, User _user)
        {
            try
            {
                User user = _context.User.Where(k => k.UserId == _user.UserId).FirstOrDefault();
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

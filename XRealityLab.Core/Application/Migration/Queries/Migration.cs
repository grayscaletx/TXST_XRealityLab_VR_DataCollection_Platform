using Application.Security;
using Common;
using Common.Constants;
using Common.Helpers;
using Infrastructure.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
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
                User user = _context.User.Where(k => k.UserId == _lab.LabDirectorId).FirstOrDefault();

                var identityUser = await _userManager.FindByEmailAsync(user.Email);
                if (identityUser != null)
                {
                    var userManagerResult = await _userManager.DeleteAsync(identityUser);
                    if (!userManagerResult.Succeeded)
                        return MessageConverter.GetResponse(string.Empty, MethodResult.Failed);
                }

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

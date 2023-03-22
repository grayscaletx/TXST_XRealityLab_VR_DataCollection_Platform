using Common.Helpers;
using Infrastructure.Entities;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using XRealityLab.Models;

namespace XRealityLab
{
    public class DataValidation
    {
        public async static Task<ResponseDTO> UserValidation(XRealityLabdbContext _context, UserDTO user)
        {
            try
            {
/*                if (user.Rank != "Dr." && user.Rank != "Mr." && user.Rank != "Ms." && user.Rank != "Mx.")
                    return MessageConverter.GetResponse("User validation failed", MethodResult.Failed);
*/
                if (user.Firstname.Length < 2 || user.Firstname.Length > 26)
                    return MessageConverter.GetResponse("User validation failed", MethodResult.Failed);

                if (user.Lastname.Length < 2 || user.Lastname.Length > 26)
                    return MessageConverter.GetResponse("User validation failed", MethodResult.Failed);

                if (user.Email.Length < 2 || user.Email.Length > 64)
                    return MessageConverter.GetResponse("User validation failed", MethodResult.Failed);

                //MailAddress m = new MailAddress(_data["email"]);

                /*if (_data["password"].ToString().Length < 2 || _data["password"].ToString().Length > 30)
                    return MessageConverter.GetResponse("User validation failed", MethodResult.Failed);*/

                if (user.RoleId == 1)
                    return MessageConverter.GetResponse("User validation failed", MethodResult.Failed);

                return MessageConverter.GetResponse(string.Empty, MethodResult.Succeeded);
            }
            catch (Exception ex)
            {
                //_context.Add(Tracing new ...failed);
                //await _context.SaveChangesAsync();
                return MessageConverter.GetResponse("User validation failed", MethodResult.Failed);
            }
        }
        public async static Task<ResponseDTO> OrganizationValidation(XRealityLabdbContext _context, Organization organization)
        {
            try
            {
                if (organization.FullName == null || organization.FullName.Length < 2 || organization.FullName.Length > 50)
                    return MessageConverter.GetResponse("Project validation failed", MethodResult.Failed);
               
                if (!Regex.IsMatch(organization.Zipcode, @"^\d+$"))
                    return MessageConverter.GetResponse("Project validation failed", MethodResult.Failed);

                if(organization.FullName == null || organization.Zipcode.Length < 5 || organization.Zipcode.Length > 5)
                    return MessageConverter.GetResponse("Project validation failed", MethodResult.Failed);

                if (organization.FullName == null || organization.Street.Length < 2 || organization.Street.Length > 50)
                    return MessageConverter.GetResponse("Project validation failed", MethodResult.Failed);

                if (organization.FullName == null || organization.State.Length < 2 || organization.State.Length > 50)
                    return MessageConverter.GetResponse("Project validation failed", MethodResult.Failed);

                if (organization.FullName == null || organization.Country.Length < 2 || organization.Country.Length > 50)
                    return MessageConverter.GetResponse("Project validation failed", MethodResult.Failed);

                if (organization.FullName == null || organization.NoAllowedLabs < 1)
                    return MessageConverter.GetResponse("Project validation failed", MethodResult.Failed);

                return MessageConverter.GetResponse(string.Empty, MethodResult.Succeeded);
            }
            catch (Exception ex)
            {
                //_context.Add(Tracing new ...failed);
                //await _context.SaveChangesAsync();                
                return MessageConverter.GetResponse("Organization validation failed", MethodResult.Failed);
            }
        }
        public async static Task<ResponseDTO> LabValidation(XRealityLabdbContext _context, Lab lab)
        {
            try
            {
                if (lab.FullName == null || lab.FullName.Length < 2 || lab.FullName.Length > 50)
                    return MessageConverter.GetResponse("Lab validation failed", MethodResult.Failed);
                
                if (lab.FullName == null || lab.Description.Length < 2 || lab.Description.Length > 250)
                    return MessageConverter.GetResponse("Lab validation failed", MethodResult.Failed);                

                return MessageConverter.GetResponse(string.Empty, MethodResult.Succeeded);
            }
            catch (Exception ex)
            {
                //_context.Add(Tracing new ...failed);
                //await _context.SaveChangesAsync();                
                return MessageConverter.GetResponse("Lab validation failed", MethodResult.Failed);
            }
        }
        public async static Task<ResponseDTO> ProjectValidation(XRealityLabdbContext _context, Project project)
        {
            try
            {
                if (project.FullName == null || project.FullName.Length < 2 || project.FullName.Length > 50)
                    return MessageConverter.GetResponse("Project validation failed", MethodResult.Failed);

                if (project.Description == null || project.Description.Length < 2 || project.Description.Length > 250)
                    return MessageConverter.GetResponse("Project validation failed", MethodResult.Failed);

                return MessageConverter.GetResponse(string.Empty, MethodResult.Succeeded);
            }
            catch (Exception ex)
            {
                //_context.Add(Tracing new ...failed);
                //await _context.SaveChangesAsync();                
                return MessageConverter.GetResponse("Project validation failed", MethodResult.Failed);
            }
        }
    }
}

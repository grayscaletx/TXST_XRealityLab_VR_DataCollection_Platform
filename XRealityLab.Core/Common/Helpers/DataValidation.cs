/*using Infrastructure.Entities;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using XRealityLab.Models;



namespace Common.Helpers
{
    public class DataValidation
    {
        public async static Task<ResponseDTO> UserValidation(XRealityLabdbContext _context, UserDTO _data)
        {
            try
            {
                if (_data["first-name"].ToString().Length < 2 || _data["first-name"].ToString().Length > 26)
                    return MessageConverter.GetResponse("User validation failed", MethodResult.Failed);

                if (_data["last-name"].ToString().Length < 2 || _data["last-name"].ToString().Length > 26)
                    return MessageConverter.GetResponse("User validation failed", MethodResult.Failed);

                if (_data["email"].ToString().Length < 2 || _data["email"].ToString().Length > 64)
                    return MessageConverter.GetResponse("User validation failed", MethodResult.Failed);

                MailAddress m = new MailAddress(_data["email"]);

                if (_data["password"].ToString().Length < 2 || _data["password"].ToString().Length > 30)
                    return MessageConverter.GetResponse("User validation failed", MethodResult.Failed);

                if (_data["role"] == "1" || _data["role"] == "2")
                    return MessageConverter.GetResponse("User validation failed", MethodResult.Failed);

                return MessageConverter.GetResponse(string.Empty, MethodResult.Succeeded);
            }
            catch (Exception ex)
            {
                //_context.Add(Tracing new ...failed);
                await _context.SaveChangesAsync();
                return MessageConverter.GetResponse("User validation failed", MethodResult.Failed);
            }
        }
    }
}
*/
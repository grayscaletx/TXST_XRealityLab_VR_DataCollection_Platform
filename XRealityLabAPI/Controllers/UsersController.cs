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
    public class UsersController : Controller
    {
        private readonly XRealityLabdbContext _context;
        private readonly IConfiguration _configuration;

        public UsersController(XRealityLabdbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // GET: api/Users/email/password
        [HttpGet("email/{email}/{password}")]
        public async Task<ActionResult<UserAPI>> GetUser(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password)) return NotFound(MessageConverter.GetResponse("Email not found or password is incorrect.", MethodResult.Failed));

            string hash = IdentityConversions.MD5Hash(password);
            var user = await _context.User.Where(k => k.Email == email && k.PasswordHash == hash).FirstOrDefaultAsync();

            if (user == null) return NotFound(MessageConverter.GetResponse("Email not found or password is incorrect.", MethodResult.Failed));
            if (!(user.HasChangedPassword == 1 ? true : false)) return NotFound(MessageConverter.GetResponse("The user has not completed email verification for the " +
                "platform or the user has been deactivated for another reason. If you have completed the verification process by email, contact the person " +
                "in charge of your organization.", MethodResult.Failed));

            string authTokenHash = Converters.CreatePassword(150);
            if (await _context.UserAuthToken.AnyAsync(k => k.UserId == user.UserId))
            {
                var authToken = await _context.UserAuthToken.Where(k => k.UserId == user.UserId).FirstOrDefaultAsync();
                authToken.AuthToken = authTokenHash;
                authToken.GeneratedAt = DateTime.UtcNow;
                _context.UserAuthToken.Update(authToken);
            }
            else
            {
                await _context.UserAuthToken.AddAsync(new UserAuthToken { UserId = user.UserId, AuthToken = authTokenHash, GeneratedAt = DateTime.UtcNow });
            }
            bool hasSucceeded = await _context.SaveChangesAsync() == 1 ? true : false;
            if (!hasSucceeded) return NotFound(MessageConverter.GetResponse("Something went wrong, please try again later.", MethodResult.Failed));

            UserAPI userAPI = new UserAPI
            {
                UserId = user.UserId,
                FirstName = user.Firstname,
                LastName = user.Lastname,
                Email = user.Email,
                AuthToken = authTokenHash,
                IsActive = user.HasChangedPassword,
                RoleId = user.RoleId,
                RegistrationDateTime = user.RegistrationDateTime
            };

            return userAPI;
        }
    }
}

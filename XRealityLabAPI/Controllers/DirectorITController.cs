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
    public class DirectorITController : ControllerBase
    {
        private readonly XRealityLabdbContext _context;
        private readonly IConfiguration _configuration;

        public DirectorITController(XRealityLabdbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // GET: /api/DirectorIT/GetLab?_authToken={authToken}
        [HttpGet("GetLab")]
        public async Task<ActionResult<LabAPI>> GetLab(string _authToken)
        {
            if (!await _context.UserAuthToken.AnyAsync(k => k.AuthToken == _authToken)) return NotFound(MessageConverter.GetResponse("AuthToken is incorrect", MethodResult.Failed));

            var authToken = await _context.UserAuthToken.Where(k => k.AuthToken == _authToken).FirstOrDefaultAsync();
            LabAPI lab = _context.LabUser.Include(k => k.Lab).Where(k => k.UserId == authToken.UserId)
                .Select(k => new LabAPI
                {
                    LabId = k.Lab.LabId,
                    FullName = k.Lab.FullName,
                    Description = k.Lab.Description,
                    Registered = k.Lab.Registered
                }).FirstOrDefault();

            if (lab == null) return NotFound(MessageConverter.GetResponse("No lab found for this user", MethodResult.Failed));
            return lab;
        }
    }
}

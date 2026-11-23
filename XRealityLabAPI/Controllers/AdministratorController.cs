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
    public class AdministratorController : ControllerBase
    {
        private readonly XRealityLabdbContext _context;
        private readonly IConfiguration _configuration;

        public AdministratorController(XRealityLabdbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // GET: /api/Administrator/GetOrganization?_authToken={authToken}
        [HttpGet("GetOrganization")]
        public async Task<ActionResult<OrganizationAPI>> GetOrganization(string _authToken)
        {
            if (!await _context.UserAuthToken.AnyAsync(k => k.AuthToken == _authToken)) return NotFound(MessageConverter.GetResponse("AuthToken is incorrect", MethodResult.Failed));

            var authToken = await _context.UserAuthToken.Where(k => k.AuthToken == _authToken).FirstOrDefaultAsync();
            Organization organization = await _context.Organization.Where(k => k.ItdirectorId == authToken.UserId).FirstOrDefaultAsync();
            if (organization == null)
            {
                var labUser = await _context.LabUser.Where(x => x.UserId == authToken.UserId).FirstOrDefaultAsync();
                var organizationLab = labUser == null ? null : await _context.OrganizationLab.Where(k => k.LabId == labUser.LabId).FirstOrDefaultAsync();
                if (organizationLab != null)
                    organization = await _context.Organization.Where(k => k.OrganizationId == organizationLab.OrganizationId).FirstOrDefaultAsync();
            }
            if (organization == null) return NotFound(MessageConverter.GetResponse("No organization found for this user", MethodResult.Failed));
            OrganizationAPI organizationAPI = new OrganizationAPI
            {
                OrganizationId = organization.OrganizationId,
                FullName = organization.FullName,
                SubscriptionStarted = organization.SubscriptionStarted,
                SubscriptionExpiration = organization.SubscriptionExpiration,
                Registered = organization.Registered
            };

            return organizationAPI;
        }
    }
}

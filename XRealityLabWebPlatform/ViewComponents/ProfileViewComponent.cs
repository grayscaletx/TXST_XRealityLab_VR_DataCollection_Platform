using Infrastructure.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using XRealityLab.Common;

namespace XRealityLab.ViewComponents
{
    public class ProfileViewComponent : ViewComponent
    {
        private readonly XRealityLabdbContext _context;

        public ProfileViewComponent(XRealityLabdbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var user = await _context.User.FirstOrDefaultAsync(k => k.Email == User.Identity.Name);

            ProfileNavDTO profileNavDTO = new ProfileNavDTO
            {
               FullName = user.Firstname + " " + user.Lastname,
               RoleName = ((RoleType)user.RoleId).GetEnumDescription(),
               PhotoUrl = user.PhotoUrl
            };
            return View("Default", profileNavDTO);
        }
    }

}

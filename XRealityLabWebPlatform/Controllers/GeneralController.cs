using Infrastructure.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace XRealityLab.Controllers
{
    [Authorize(Roles = "Administrator,DirectorIT")]
    public class GeneralController : Controller
    {
        private readonly XRealityLabdbContext _context;

        public GeneralController(XRealityLabdbContext context)
        {
            _context = context;

        }
        public IActionResult Index() { return View(); }
    }
}
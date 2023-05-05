using Infrastructure.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

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
        public async Task<IActionResult> Search(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return RedirectToAction("Index", "Auth");

            

            return View();
        }
    }
}
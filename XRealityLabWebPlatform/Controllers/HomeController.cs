using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace XRealityLab.Controllers
{
    public class HomeController : Controller
    {
        [Authorize]
        public IActionResult LogOut()
        {
            return RedirectToAction("Logout", "Auth");
        }
        [Authorize]
        public IActionResult LogOutAndChangePassword()
        {
            return RedirectToAction("LogOutAndChangePassword", "Auth");
        }
        public IActionResult Contact()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}

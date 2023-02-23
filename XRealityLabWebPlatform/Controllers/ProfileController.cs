/*using Application.Security;
using ARI.Application;
using ARI.Users.Queries;
using Infrastructure.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ARI.Controllers
{
    //[Authorize("Υπεύθυνος Ασφαλείας,Υπεύθυνος Ροών Εταιρίας")]
    public class ProfileController : Controller
    {
        private readonly ARIContext _context;
        private IUserRepository _userRepository;
        private readonly IHostingEnvironment _environment;
        private readonly CustomIDataProtection protector;
        public ProfileController(ARIContext context, IHostingEnvironment IHostingEnvironment, CustomIDataProtection customIDataProtection)
        {
            _context = context;
            _userRepository = new UserRepository(_context);
            _environment = IHostingEnvironment;
            protector = customIDataProtection;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> EditProfile()
        {
            var userDto = await _context.Users.Where(k => k.Email == User.Identity.Name).Select(k => new UserDTO
            {
                UserId = k.UserId,
                PassToken = protector.Decode(k.UserId.ToString()),
                Firstname = k.Firstname,
                Lastname = k.Lastname,
                Email = k.Email,
                Telephone = k.Telephone,
                Registered = k.RegistrationDateTime
            }).FirstOrDefaultAsync();

            return View(userDto);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile([Bind("Firstname", "Lastname", "Telephone")] UserDTO userDto)
        {
            if (userDto.Firstname == null || userDto.Lastname == null || userDto.Telephone == null)
                return View();

            if (userDto.Firstname.Length < 2 || userDto.Firstname.Length > 50 || userDto.Lastname.Length < 2 || userDto.Lastname.Length > 50
                || userDto.Telephone.Length < 2 || userDto.Telephone.Length > 50)
                return View();

            User user = await _context.Users.FirstOrDefaultAsync(k => k.Email == User.Identity.Name);

            if (user == null) { return NotFound(); }

            user.Firstname = userDto.Firstname;
            user.Lastname = userDto.Lastname;
            user.Telephone = userDto.Telephone;

            try
            {
                _context.Update(user);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return NotFound();
            }

            return RedirectToAction("Login", "Auth");
        }
    }
}
*/
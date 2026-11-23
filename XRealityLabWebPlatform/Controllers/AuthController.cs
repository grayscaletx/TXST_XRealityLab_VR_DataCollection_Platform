using XRealityLab.Application;
using XRealityLab.Common;
using XRealityLab.Security;
using Common;
using Infrastructure.Entities;
using Mail;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace XRealityLab.Controllers
{
    public class AuthController : Controller
    {
        private readonly XRealityLabdbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly GoogleReCAPTCHAService _googleReCAPTCHAService;
        private readonly IHostingEnvironment _environment;

        public AuthController(XRealityLabdbContext context,
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            GoogleReCAPTCHAService googleReCAPTCHAService,
            IHostingEnvironment IHostingEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _googleReCAPTCHAService = googleReCAPTCHAService;
            _environment = IHostingEnvironment;
        }

        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                var role = ((ClaimsIdentity)User.Identity).FindAll("http://schemas.microsoft.com/ws/2008/06/identity/claims/role").ToList().LastOrDefault().Value;
                if (role == RoleType.Administrator.GetEnumDescription())
                    return RedirectToAction("Organizations", RoleType.Administrator.GetEnumDescription());
                else if (role == RoleType.DirectorOfIT.GetEnumDescription())
                    return RedirectToAction("Labs", RoleType.DirectorOfIT.GetEnumDescription());
                else if (role == RoleType.LabDirector.GetEnumDescription())
                    return RedirectToAction("AllProjects", "Project");
                else if (role == RoleType.PrincipleInvestigator.GetEnumDescription())
                    return RedirectToAction("MyProjects", "Project");
                else if (role == RoleType.ResearchAssistant.GetEnumDescription())
                    return RedirectToAction("MyProjects", "Project");
                else
                    return RedirectToAction("Organizations", RoleType.Administrator.GetEnumDescription());
            }
            else
                return View(nameof(Login));
        }

        [AllowAnonymous]
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
            {
                var role = ((ClaimsIdentity)User.Identity).FindAll("http://schemas.microsoft.com/ws/2008/06/identity/claims/role").ToList().LastOrDefault().Value;
                if (role == RoleType.Administrator.GetEnumDescription())
                    return RedirectToAction("Organizations", RoleType.Administrator.GetEnumDescription());
                else if (role == RoleType.DirectorOfIT.GetEnumDescription())
                    return RedirectToAction("Labs", RoleType.DirectorOfIT.GetEnumDescription());
                else if (role == RoleType.LabDirector.GetEnumDescription())
                    return RedirectToAction("AllProjects", "Project");
                else if (role == RoleType.PrincipleInvestigator.GetEnumDescription())
                    return RedirectToAction("MyProjects", "Project");
                else if (role == RoleType.ResearchAssistant.GetEnumDescription())
                    return RedirectToAction("MyProjects", "Project");
                else
                    return RedirectToAction("MyProjects", "Project");
            }
            else
                return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login([Bind("Email,Password,Token")] IdentityLoginUserDTO identityUserDto)
        {
            //Google reCAPTCHA validation
            if (!await _googleReCAPTCHAService.IsHumanAsync(identityUserDto.Token))
                return AuthenticationError(View(), string.Empty, "Please try again later.");

            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(identityUserDto.Email) || string.IsNullOrEmpty(identityUserDto.Password))
                return AuthenticationError(View(), string.Empty, "Email, Password combination wrong");

            string hash = IdentityConversions.MD5Hash(identityUserDto.Password);
            var user = _context.User.Include(l => l.Role).Where(k => k.Email == identityUserDto.Email.Trim() && k.PasswordHash == hash).FirstOrDefault();
            var roles = _context.Role.ToListAsync();

            // User doesn't exist or invalid model
            if (user == null || !ModelState.IsValid)
                return AuthenticationError(View(), string.Empty, "Email, Password combination wrong");

            //Check if User has changed password for the first time 
            if (user.HasChangedPassword == 0 && DateTime.Now > user.RegistrationDateTime.AddDays(10))
                return AuthenticationError(View(), string.Empty, "Temporary password has expired please use forgot password to reactivate account");



            //Find if user already exist in the userManager
            var _identityUser = await _userManager.FindByEmailAsync(user.Email);
            if (_identityUser == null)
            {
                var userManagerResult = await _userManager.CreateAsync(new IdentityUser
                {
                    Id = user.IdentityId,
                    Email = identityUserDto.Email.Trim(),
                    UserName = identityUserDto.Email.Trim(),
                    EmailConfirmed = true
                }, identityUserDto.Password);

                //Couldn't create user manager
                if (!userManagerResult.Succeeded)
                    return AuthenticationError(View(), string.Empty, "Something went wrong. Please try again.");
            }
            else
            {
                if (_identityUser.PasswordHash == null)
                {
                    _identityUser.PasswordHash = identityUserDto.Password;
                    var userManagerResult = await _userManager.UpdateAsync(_identityUser);

                    //Couldn't update user manager
                    if (!userManagerResult.Succeeded)
                        return AuthenticationError(View(), string.Empty, "Something went wrong. Please try again.");
                }
            }

            var __identityUser = await _userManager.FindByEmailAsync(user.Email);
            await _userManager.AddClaimAsync(__identityUser, new Claim(ClaimTypes.Role, roles.Result.Where(k => k.RoleId == user.RoleId).FirstOrDefault().Type));

            var signInResult = await _signInManager.PasswordSignInAsync(identityUserDto.Email.Trim(), identityUserDto.Password, false, false);

            //Couldn't sign to signin manager
            if (!signInResult.Succeeded)
                return AuthenticationError(View(), string.Empty, "Something went wrong. Please try again.");

            _context.User.Update(user);
            _context.SaveChanges();

            if (user.RoleId == (int)RoleType.Administrator)
                return RedirectToAction("Organizations", RoleType.Administrator.GetEnumDescription());
            else if (user.RoleId == (int)RoleType.DirectorOfIT)
                return RedirectToAction("Labs", RoleType.DirectorOfIT.GetEnumDescription());
            else if (user.RoleId == (int)RoleType.LabDirector)
                return RedirectToAction("AllProjects", "Project");
            else if (user.RoleId == (int)RoleType.PrincipleInvestigator)
                return RedirectToAction("MyProjects", "Project");
            else if (user.RoleId == (int)RoleType.ResearchAssistant)
                return RedirectToAction("MyProjects", "Project");
            else
                return RedirectToAction("MyProjects", "Project");
        }

        public async Task<IActionResult> VerifyEmail(string userId, string code)
        {
            var identityUser = await _userManager.FindByIdAsync(userId);

            if (identityUser == null) return BadRequest();

            //verify account
            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var result = await _userManager.ConfirmEmailAsync(identityUser, code);
            if (result.Succeeded)
            {
                var user = _context.User.Where(k => k.Email == identityUser.Email).FirstOrDefault();
                user.HasChangedPassword = 1;
                _context.Update(user);
                _context.SaveChanges();
                return View();
            }

            return BadRequest();
        }
        public IActionResult EmailVerification() => View();

        [AllowAnonymous]
        public IActionResult EmailVerificationRequired()
        {
            if (User.Identity.IsAuthenticated)
                return RedirectToAction("Organizations", RoleType.Administrator.GetEnumDescription());
            else
                return View();
        }

        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            if (User.Identity.IsAuthenticated)
                return RedirectToAction("Organizations", RoleType.Administrator.GetEnumDescription());
            else
                return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword([Bind("Email,Token")] IdentityForgotPasswordDTO identityForgotPasswordDto)
        {
            //Google reCAPTCHA validation
            if (!await _googleReCAPTCHAService.IsHumanAsync(identityForgotPasswordDto.Token))
                return AuthenticationError(View(), string.Empty, "Please try again later.");

            if (string.IsNullOrWhiteSpace(identityForgotPasswordDto.Email))
                return AuthenticationError(View(), string.Empty, "Please enter your email address.");

            var user = _context.User.Where(k => k.Email == identityForgotPasswordDto.Email.Trim()).FirstOrDefault();

            ViewBag.Message = "We have just sent you a link to reset your password.";

            // User doesn't exist or invalid model
            if (user != null)
            {
                var identityUser = await _userManager.FindByNameAsync(identityForgotPasswordDto.Email.Trim());
                if (identityUser == null)
                {
                    var userManagerResult = await _userManager.CreateAsync(new IdentityUser
                    {
                        Id = user.IdentityId,
                        Email = identityForgotPasswordDto.Email.Trim(),
                        UserName = identityForgotPasswordDto.Email.Trim(),
                        EmailConfirmed = user.HasChangedPassword == 1 ? true : true
                    });

                    //Couldn't create user manager
                    if (!userManagerResult.Succeeded)
                        return AuthenticationError(View(), string.Empty, "Something went wrong. Please try again.");

                    identityUser = await _userManager.FindByNameAsync(identityForgotPasswordDto.Email.Trim());
                    if (identityUser == null)
                        return AuthenticationError(View(), string.Empty, "Something went wrong. Please try again.");
                }

                //generation for the new email token
                var code = await _userManager.GeneratePasswordResetTokenAsync(identityUser);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                var link = Url.Action(nameof(ChangePassword), "Auth", new { userId = identityUser.Id, code }, Request.Scheme, Request.Host.ToString());

                //Send email
                MailService mailService = new MailService();
                await mailService.SendResetPasswordAsync(link, user.Email);
            }

            return RedirectToAction(nameof(Login));
        }

        [AllowAnonymous]
        public async Task<IActionResult> ChangePassword(string userId, string code)
        {
            if (User.Identity.IsAuthenticated)
                return RedirectToAction("Organizations", RoleType.Administrator.GetEnumDescription());
            else
            {
                var identityUser = await _userManager.FindByIdAsync(userId);
                if (identityUser == null || code == null || userId == null) return BadRequest();

                IdentityChangePasswordDTO identityChangePasswordDto = new IdentityChangePasswordDTO();
                identityChangePasswordDto.IdentityID = userId;
                identityChangePasswordDto.Code = code;

                return View(identityChangePasswordDto);
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword([Bind("Password,ConfirmPassword,IdentityID,Code")] IdentityChangePasswordDTO identityChangePasswordDto)
        {
            //Main validation and check if password input fields are the same
            if (!ModelState.IsValid || identityChangePasswordDto.Password != identityChangePasswordDto.ConfirmPassword)
                return AuthenticationError(View(), string.Empty, "Please make sure your passwords match.");

            var identityUser = await _userManager.FindByIdAsync(identityChangePasswordDto.IdentityID);
            if (identityUser == null) return BadRequest();

            var user = await _context.User.Where(k => k.Email == identityUser.Email).FirstOrDefaultAsync();

            string code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(identityChangePasswordDto.Code));
            var result = await _userManager.ResetPasswordAsync(identityUser, code, identityChangePasswordDto.Password);
            if (result.Succeeded)
            {
                string hash = IdentityConversions.MD5Hash(identityChangePasswordDto.Password);
                user.PasswordHash = hash;
                user.HasChangedPassword = 1;
                _context.Update(user);
                _context.SaveChanges();
                return RedirectToAction(nameof(Login));
            }

            return BadRequest();
        }

        [AllowAnonymous]
        public IActionResult Error()
        {
            return Content("Something went wrong while handling your request. Please go back and try again. If the problem persists, contact the platform administrator.");
        }

        [Authorize]
        public async Task<IActionResult> LogOut()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        [Authorize]
        public async Task<IActionResult> LogOutAndChangePassword()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(ForgotPassword));
        }

        private ViewResult AuthenticationError(ViewResult viewResult, string key, string message)
        {
            ModelState.AddModelError(key, message);
            return viewResult;
        }
    }
}

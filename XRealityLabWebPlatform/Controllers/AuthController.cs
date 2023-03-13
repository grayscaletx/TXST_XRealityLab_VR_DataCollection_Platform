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
using NETCore.MailKit.Core;
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
        private readonly IEmailService _emailService;
        private readonly IHostingEnvironment _environment;

        public AuthController(XRealityLabdbContext context,
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            GoogleReCAPTCHAService googleReCAPTCHAService,
            IEmailService emailService,
            IHostingEnvironment IHostingEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _googleReCAPTCHAService = googleReCAPTCHAService;
            _environment = IHostingEnvironment;
            _emailService = emailService;
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
            var _GoogleReCAPTCHA = _googleReCAPTCHAService.VerifyReCAPTCHA(identityUserDto.Token);

            if (!_GoogleReCAPTCHA.Result.success && _GoogleReCAPTCHA.Result.score <= 0.5)
                return AuthenticationError(View(), string.Empty, "Please try again later.");

            string hash = IdentityConversions.MD5Hash(identityUserDto.Password);
            var user = _context.User.Include(l => l.Role).Where(k => k.Email == identityUserDto.Email.Trim() && k.PasswordHash == hash).FirstOrDefault();
            var roles = _context.Role.ToListAsync();

            // User doesn't exist or invalid model
            if (user == null || !ModelState.IsValid)
                return AuthenticationError(View(), string.Empty, "We have just sent you a link to reset your password.");

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

            //The user has not activated their account via email
            if (user.IsActive == 0)
            {
                //generation of the email token
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(__identityUser);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                var link = Url.Action(nameof(VerifyEmail), "Auth", new { userId = __identityUser.Id, code }, Request.Scheme, Request.Host.ToString());

                //Send email
                MailService mailService = new MailService();
                await mailService.SendAccountVerificationAsync(user.Firstname, link, user.Email);

                return RedirectToAction(nameof(EmailVerificationRequired));
            }

            //if (user.Role.RoleId == (int)RoleType.NoRole)
            //{
            //    return AuthenticationError(View(), string.Empty, "Το τεχνικό τμήμα της εταιρίας δεν έχει εγκρίνει το λογαριασμό σας ακόμα. Παρακαλώ επικοινωνήστε με το τεχνικό τμήμα και δοκιμάστε ξανά.");
            //}

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

        [AllowAnonymous]
        public IActionResult Register()
        {
            //if (User.Identity.IsAuthenticated)
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register([Bind("Email,Firstname,Lastname,Password,ConfirmPassword,PhoneNumber,Token")]
            IdentityUserDTO identityUserDto)
        {
            //Main validation
            if (!ModelState.IsValid)
                return AuthenticationError(View(), string.Empty, "Incorrect fields.");

            //Google reCAPTCHA validation
            var _GoogleReCAPTCHA = _googleReCAPTCHAService.VerifyReCAPTCHA(identityUserDto.Token);

            if (!_GoogleReCAPTCHA.Result.success && _GoogleReCAPTCHA.Result.score <= 0.5)
                return AuthenticationError(View(), string.Empty, "Please try again later.");

            //Check if password input fields are the same
            if (identityUserDto.Password != identityUserDto.ConfirmPassword)
                return AuthenticationError(View(), string.Empty, "Please make sure your passwords match.");

            //Check if this email already exist
            var emailExist = await _context.User.AnyAsync(k => k.Email == identityUserDto.Email.Trim());
            if (emailExist)
                return AuthenticationError(View(), string.Empty, "That email is already in user, please use a different email address.");

            var identityUser = new IdentityUser
            {
                Email = identityUserDto.Email.Trim(),
                UserName = identityUserDto.Email.Trim()
            };
            var userManager = await _userManager.CreateAsync(identityUser, identityUserDto.Password);

            //Create a new user
            string hash = IdentityConversions.MD5Hash(identityUserDto.Password);
            var user = new User
            {
                IdentityId = identityUser.Id,
                Firstname = identityUserDto.Firstname.Trim(),
                Lastname = identityUserDto.Lastname.Trim(),
                Email = identityUserDto.Email.Trim(),
                PasswordHash = hash,
                Telephone = identityUserDto.PhoneNumber,
                RoleId = (int)RoleType.Administrator,
                RegistrationDateTime = DateTime.Now,
                IsActive = 0
            };
            await _context.AddAsync(user);
            var hasUserCreated = await _context.SaveChangesAsync();

            //User couldn't be created to the database
            if (hasUserCreated != 1)
                return AuthenticationError(View(), string.Empty, "Something went wrong. Please try again.");

            if (userManager.Succeeded)
            {
                //generation of the email token
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(identityUser);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                var link = Url.Action(nameof(VerifyEmail), "Auth", new { userId = identityUser.Id, code }, Request.Scheme, Request.Host.ToString());

                //Send email
                MailService mailService = new MailService();
                await mailService.SendAccountVerificationAsync(user.Firstname, link, user.Email);

                return RedirectToAction(nameof(EmailVerification));
            }
            else
                return AuthenticationError(View(), string.Empty, "Something went wrong. Please try again.");
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
                user.IsActive = 1;
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
            var _GoogleReCAPTCHA = _googleReCAPTCHAService.VerifyReCAPTCHA(identityForgotPasswordDto.Token);

            if (!_GoogleReCAPTCHA.Result.success && _GoogleReCAPTCHA.Result.score <= 0.5)
                return AuthenticationError(View(), string.Empty, "Please try again later.");

            var user = _context.User.Where(k => k.Email == identityForgotPasswordDto.Email.Trim()).FirstOrDefault();

            ViewBag.Message = "We have just sent you a link to reset your password.";

            // User doesn't exist or invalid model
            if (user != null || !ModelState.IsValid)
            {
                var identityUser = await _userManager.FindByNameAsync(identityForgotPasswordDto.Email.Trim());
                if (identityUser == null)
                {
                    var userManagerResult = await _userManager.CreateAsync(new IdentityUser
                    {
                        Id = user.IdentityId,
                        Email = identityForgotPasswordDto.Email.Trim(),
                        UserName = identityForgotPasswordDto.Email.Trim(),
                        EmailConfirmed = user.IsActive == 1 ? true : false
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
                _context.Update(user);
                _context.SaveChanges();
                return RedirectToAction(nameof(Login));
            }

            return BadRequest();
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

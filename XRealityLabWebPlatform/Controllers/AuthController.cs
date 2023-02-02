using ARI.Application;
using ARI.Common;
using ARI.Models;
using ARI.Security;
using Common;
using EdiTools;
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
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ARI.Controllers
{
    public class AuthController : Controller
    {
        private readonly ARIContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly GoogleReCAPTCHAService _googleReCAPTCHAService;
        private readonly IEmailService _emailService;
        private readonly IHostingEnvironment _environment;

        public AuthController(ARIContext ariContext,
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            GoogleReCAPTCHAService googleReCAPTCHAService,
            IEmailService emailService, 
            IHostingEnvironment IHostingEnvironment)
        {
            _context = ariContext;
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
                if (role == "Υπεύθυνος Ασφαλείας")
                    return RedirectToAction("TruckList", "ACFacilities");
                if (role == "IT")
                    return RedirectToAction("UserList", "Staff");
                if (role == "Επόπτης Λογιστής")
                    return RedirectToAction("Orders", "Logistics");
                if (role == "Υπεύθυνος Συσκευαστηρίου")
                    return RedirectToAction("PackagingLineForms", "PackagingLine");
                if (role == "Υπεύθυνος Παραγωγής")
                    return RedirectToAction("PackagingLineForms", "PackagingLine");
                if (role == "Διαχειριστής")
                    return RedirectToAction("General", "Admin");
                if (role == "Τμήμα Διασφάλισης της Ποιότητας")
                    return RedirectToAction("Materials", "QualityAssurance");
                else
                    return RedirectToAction("UserList", "Staff");
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
                if (role == "Υπεύθυνος Ασφαλείας")
                    return RedirectToAction("TruckList", "ACFacilities");
                if (role == "IT")
                    return RedirectToAction("UserList", "Staff");
                if (role == "Επόπτης Λογιστής")
                    return RedirectToAction("Orders", "Logistics");
                if (role == "Υπεύθυνος Συσκευαστηρίου")
                    return RedirectToAction("PackagingLineForms", "PackagingLine");
                if (role == "Υπεύθυνος Παραγωγής")
                    return RedirectToAction("PackagingLineForms", "PackagingLine");
                if (role == "Διαχειριστής")
                    return RedirectToAction("General", "Admin");
                if (role == "Τμήμα Διασφάλισης της Ποιότητας")
                    return RedirectToAction("Materials", "QualityAssurance");
                else
                    return RedirectToAction("UserList", "Staff");
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
                return AuthenticationError(View(), string.Empty, "Προέκυψε κάποιο πρόβλημα, παρακαλώ προσπαθήστε αργότερα.");

            string hash = IdentityConversions.MD5Hash(identityUserDto.Password);
            var user = _context.Users.Include(l => l.Role).Where(k => k.Email == identityUserDto.Email.Trim() && k.PasswordHash == hash).FirstOrDefault();
            var roles = _context.Roles.ToListAsync();

            // User doesn't exist or invalid model
            if (user == null || !ModelState.IsValid)
                return AuthenticationError(View(), string.Empty, "Πληκτρολογήσατε λάθος ηλεκτρονική διεύθυνση ή κωδικό πρόσβασης, παρακαλώ προσπαθήστε ξανά.");

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
                    return AuthenticationError(View(), string.Empty, "Προέκυψε κάποιο πρόβλημα, παρακαλώ προσπαθήστε αργότερα. 135");
            }
            else
            {
                if (_identityUser.PasswordHash == null)
                {
                    _identityUser.PasswordHash = identityUserDto.Password;
                    var userManagerResult = await _userManager.UpdateAsync(_identityUser);

                    //Couldn't update user manager
                    if (!userManagerResult.Succeeded)
                        return AuthenticationError(View(), string.Empty, "Προέκυψε κάποιο πρόβλημα, παρακαλώ προσπαθήστε αργότερα. 122");
                }
            }

            var __identityUser = await _userManager.FindByEmailAsync(user.Email);
            await _userManager.AddClaimAsync(__identityUser, new Claim(ClaimTypes.Role, roles.Result.Where(k => k.RoleId == user.RoleId).FirstOrDefault().Type));

            //The user has not activated their account via email
            if (user.Active == 0)
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

            if (user.Role.RoleId == (int)RoleType.NoRole)
            {
                return AuthenticationError(View(), string.Empty, "Το τεχνικό τμήμα της εταιρίας δεν έχει εγκρίνει το λογαριασμό σας ακόμα. Παρακαλώ επικοινωνήστε με το τεχνικό τμήμα και δοκιμάστε ξανά.");
            }

            var signInResult = await _signInManager.PasswordSignInAsync(identityUserDto.Email.Trim(), identityUserDto.Password, false, false);

            //Couldn't sign to signin manager
            if (!signInResult.Succeeded)
                return AuthenticationError(View(), string.Empty, "Προέκυψε κάποιο πρόβλημα, παρακαλώ προσπαθήστε αργότερα.");

            user.LastConnectionDateTime = DateTime.Now;
            _context.Users.Update(user);
            _context.SaveChanges();

            if (user.RoleId == (int)RoleType.Security)
                return RedirectToAction("TruckList", "ACFacilities");
            if (user.RoleId == (int)RoleType.IT)
                return RedirectToAction("UserList", "Staff");
            if (user.RoleId == (int)RoleType.LogisticsSupervisor)
                return RedirectToAction("Orders", "Logistics");
            if (user.RoleId == (int)RoleType.PackagingManager)
                return RedirectToAction("PackagingLineForms", "PackagingLine");
            if (user.RoleId == (int)RoleType.ProductionManager)
                return RedirectToAction("PackagingLineForms", "PackagingLine");
            if (user.RoleId == (int)RoleType.Administrator)
                return RedirectToAction("General", "Admin");
            if (user.RoleId == (int)RoleType.QualityAssuranceDepartment)
                return RedirectToAction("Materials", "QualityAssurance");
            else
                return RedirectToAction("UserList", "Staff");
        }

        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity.IsAuthenticated)
                return RedirectToAction("UserList", "Staff");
            else
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
                return AuthenticationError(View(), string.Empty, "Προέκυψε κάποιο πρόβλημα, παρακαλώ προσπαθήστε αργότερα.");

            //Check if password input fields are the same
            if (identityUserDto.Password != identityUserDto.ConfirmPassword)
                return AuthenticationError(View(), string.Empty, "Ο κωδικοί δεν ταιριάζουν μεταξύ τους. Παρακαλώ προσπαθήστε ξανά.");

            //Check invalid characters
            //if (!Regex.IsMatch(identityUserDto.Password, @"^[a-zA-Z0-9_!@#%^&*]+$"))
            //    return AuthenticationError(View(), string.Empty, "Ο κωδικός περιέχει μη αποδεκτούς χαρακτήρες. Οι αποδεκτοί χαρακτήρες είναι γράμματα και αριθμοί και σύμβολα");

            //Check if this email already exist
            var emailExist = await _context.Users.AnyAsync(k => k.Email == identityUserDto.Email.Trim());
            if (emailExist)
                return AuthenticationError(View(), string.Empty, "Υπάρχει λογαριασμός με αυτή την ηλεκτρονική διεύθυνση, παρακαλώ δοκιμάστε άλλη ηλεκτρονική διεύθυνση.");

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
                RoleId = (int)RoleType.NoRole,
                RegistrationDateTime = DateTime.Now,
                Active = 0,
                Blocked = 0
            };
            await _context.AddAsync(user);
            var hasUserCreated = await _context.SaveChangesAsync();

            //User couldn't be created to the database
            if (hasUserCreated != 1)
                return AuthenticationError(View(), string.Empty, "Προέκυψε κάποιο πρόβλημα, παρακαλώ προσπαθήστε αργότερα.");

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
                return AuthenticationError(View(), string.Empty, "Προέκυψε κάποιο πρόβλημα, παρακαλώ προσπαθήστε αργότερα.");
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
                var user = _context.Users.Where(k => k.Email == identityUser.Email).FirstOrDefault();
                user.Active = 1;
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
                return RedirectToAction("UserList", "Staff");
            else
                return View();
        }

        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            if (User.Identity.IsAuthenticated)
                return RedirectToAction("UserList", "Staff");
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
                return AuthenticationError(View(), string.Empty, "Προέκυψε κάποιο πρόβλημα, παρακαλώ προσπαθήστε αργότερα.");

            var user = _context.Users.Where(k => k.Email == identityForgotPasswordDto.Email.Trim()).FirstOrDefault();

            ViewBag.Message = "Στάλθηκαν στην ηλεκτρονική σου αλληλογραφία οδηγίες για την επαναφορά του κωδικού σου.";

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
                        EmailConfirmed = user.Active == 1 ? true : false
                    });

                    //Couldn't create user manager
                    if (!userManagerResult.Succeeded)
                        return AuthenticationError(View(), string.Empty, "Προέκυψε κάποιο πρόβλημα, παρακαλώ προσπαθήστε αργότερα.");

                    identityUser = await _userManager.FindByNameAsync(identityForgotPasswordDto.Email.Trim());
                    if (identityUser == null)
                        return AuthenticationError(View(), string.Empty, "Προέκυψε κάποιο πρόβλημα, παρακαλώ προσπαθήστε αργότερα.");
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
            {
                return RedirectToAction("UserList", "Staff");
            }
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
                return AuthenticationError(View(), string.Empty, "Ο κωδικοί δεν ταιριάζουν μεταξύ τους. Παρακαλώ προσπαθήστε ξανά.");

            var identityUser = await _userManager.FindByIdAsync(identityChangePasswordDto.IdentityID);
            if (identityUser == null) return BadRequest();

            var user = await _context.Users.Where(k => k.Email == identityUser.Email).FirstOrDefaultAsync();

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

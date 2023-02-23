/*using Application.Security;
using ARI.Application;
using ARI.Common;
using ARI.Models;
using ARI.Users.Queries;
using Common;
using Common.Constants;
using Common.Helpers;
using Infrastructure.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ARI.Controllers
{
    [Authorize(Roles = "IT,Υπεύθυνος Ροών Εταιρίας")]
    public class StaffController : Controller
    {
        private readonly ARIContext _context;
        private IUserRepository _userRepository;
        private readonly IHostingEnvironment _environment;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly CustomIDataProtection protector;

        public StaffController(ARIContext context, IHostingEnvironment IHostingEnvironment, CustomIDataProtection customIDataProtection,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userRepository = new UserRepository(_context);
            _userManager = userManager;
            _environment = IHostingEnvironment;
            protector = customIDataProtection;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(UserList));
        }
        public async Task<IActionResult> UserList()
        {
            var role = ((ClaimsIdentity)User.Identity).FindAll("http://schemas.microsoft.com/ws/2008/06/identity/claims/role").ToList().LastOrDefault().Value;
            List<UserDTO> userDto = await _context.Users.Include(k => k.Role).Select(x => new UserDTO
            {
                UserId = x.UserId,
                RoleId = x.RoleId,
                PassToken = protector.Decode(x.UserId.ToString()),
                Firstname = x.Firstname,
                Lastname = x.Lastname,
                Email = x.Email,
                Telephone = x.Telephone,
                LastConnectionDateTime = ((x.LastConnectionDateTime != null)? x.LastConnectionDateTime.Value.ToString("dd/MM/yyyy HH:mm") : "-"),
                Registered = x.RegistrationDateTime,
                RoleName = x.Role.Type
            }).ToListAsync();

            return View(userDto);
        }

        //Settings
        public async Task<IActionResult> EditUser(string passToken)
        {
            if (passToken == null || passToken == string.Empty) return NotFound();

            try
            {
                var userDto = await _context.Users.Where(k => k.UserId == int.Parse(protector.Encode(passToken))).Select(x => new UserDTO
                {
                    UserId = x.UserId,
                    PassToken = protector.Decode(x.UserId.ToString()),
                    SelectedRole = "-",
                    RoleId = x.RoleId,
                }).FirstOrDefaultAsync();
                userDto.RoleListItems = LoadRoles(userDto.RoleId).Result;

                return View(userDto);
            }
            catch (Exception ex)
            {
                return NotFound();
            }
        }
        [HttpPost,ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(string passToken, [Bind("SelectedRole")] UserDTO userDto)
        {
            if (passToken == null || passToken == string.Empty) return NotFound();

            if (userDto.SelectedRole == null)
            {
                userDto = await _context.Users.Where(k => k.UserId == int.Parse(protector.Encode(passToken))).Select(x => new UserDTO
                {
                    UserId = x.UserId,
                    PassToken = protector.Decode(x.UserId.ToString()),
                    SelectedRole = "-",
                    RoleId = x.RoleId,
                }).FirstOrDefaultAsync();
                userDto.RoleListItems = LoadRoles(userDto.RoleId).Result;


                return AuthenticationError(View(userDto), string.Empty, "Εσφαλμένα πεδία, συμπληρώστε τα πεδία ξανά");
            }

            if (userDto.SelectedRole == "-") userDto.SelectedRole = "10";

            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(k => k.UserId == int.Parse(protector.Encode(passToken)));
                user.RoleId = int.Parse(userDto.SelectedRole);
                _context.Users.Update(user);
                _context.SaveChanges();

                return RedirectToAction(nameof(UserList));
            }
            catch (Exception ex)
            {
                return NotFound();
            }
        }

        [HttpPost]
        public async Task<JsonResult> RemoveUser(IFormCollection _data)
        {
            if (!int.TryParse(_data["user-id"], out int userId))
                return Json(MessageConverter.GetResponse("Δεν βρέθηκε ο χρήστης", MethodResult.Failed));

            string userName = _context.Users.Where(k => k.UserId == userId).Select(k => k.Firstname + " " + k.Lastname).FirstOrDefault();

            return Json(await RemoveUserAsync(_context, userId, _userManager));
        }
        public static async Task<ResponseDTO> RemoveUserAsync(ARIContext _context, int _userId, UserManager<IdentityUser> _userManager)
        {
            try
            {
                User user = await _context.Users.Where(k => k.UserId == _userId).FirstOrDefaultAsync();

                var identityUser = await _userManager.FindByEmailAsync(user.Email);
                if (identityUser != null)
                {
                    var userManagerResult = await _userManager.DeleteAsync(identityUser);
                    if (!userManagerResult.Succeeded)
                        return MessageConverter.GetResponse(string.Empty, MethodResult.Failed);
                }

                List<CompanyCarReservation> companyCarReservations = await _context.CompanyCarReservations.Where(k => k.UserId == _userId).ToListAsync();
                _context.RemoveRange(companyCarReservations);
                _context.Remove(user);

                await _context.SaveChangesAsync();
                return MessageConverter.GetResponse(string.Empty, MethodResult.Succeeded);
            }
            catch (Exception ex)
            {
                return MessageConverter.GetResponse(string.Empty, MethodResult.Failed);
            }
        }

        [HttpPost]
        public async Task<JsonResult> AddUser(IFormCollection _data)
        {
            ResponseDTO responseDTO = await ClassValidation.UserValidation(_context, _data);
            if (responseDTO.Result == Enum.GetName(typeof(RoleType), MethodResult.Failed))
                return Json(responseDTO);

            if (!int.TryParse(_data["role"], out int roleId))
                return Json(MessageConverter.GetResponse("Δεν βρέθηκε ο ρόλος", MethodResult.Failed));

            if (roleId == 1 || roleId == 2)
                return Json(MessageConverter.GetResponse("Δεν ήταν εφικτή η δημιουργία χρήστη με τον επιλεγμένο ρόλο.", MethodResult.Failed));

            if (_context.Users.Where(k => k.RegistrationDateTime.AddDays(1) > DateTime.Now).Count() >= 50)
                return Json(MessageConverter.GetResponse("Δεν μπορούν να προστεθούν νέοι χρήστες σήμερα.", MethodResult.Failed));

            Regex regexCriteria = new Regex("^[a-zA-Zα-ωΑ-Ω]*$");
            if (regexCriteria.IsMatch(_data["first-name"]) && regexCriteria.IsMatch(_data["last-name"]))
                return Json(MessageConverter.GetResponse("Δεν ήταν εφικτή η δημιουργία χρήστη με το επιλεγμένο όνομα.", MethodResult.Failed));

            UserDTO userDTO = new UserDTO
            {
                Firstname = _data["first-name"],
                Lastname = _data["last-name"],
                Email = _data["email"],
                Password = _data["password"],
                RoleId = int.Parse(_data["role"]),
                Telephone = "-",
                IsActive = true
            };

            if (await _context.Users.AnyAsync(k => k.Email == userDTO.Email.Trim()))
                return Json(MessageConverter.GetResponse("Δεν μπορεί να χρησιμοποιηθεί το email που χρησιμοποίεται από άλλο χρήστη.", MethodResult.Failed));

            var hasCreated = await CreateIdentityUserAsync(_userManager, userDTO.Email.Trim(), userDTO.Password);
            if (!hasCreated.Succeeded)
                return Json(MessageConverter.GetResponse("Couldn't create the user identity", MethodResult.Failed));

            User user = await ToUser(_context, userDTO);

            if (user == null)
                return Json(MessageConverter.GetResponse("Failed to create the user", MethodResult.Failed));

            await _context.AddAsync(user);
            await _context.SaveChangesAsync();

            return Json(MessageConverter.GetResponse("Succeded", MethodResult.Succeeded));
        }
        public static async Task<IdentityResult> CreateIdentityUserAsync(UserManager<IdentityUser> _userManager, string email, string password)
        {
            var identityUser = new IdentityUser
            {
                Email = email,
                UserName = email
            };

            var code = _userManager.GenerateEmailConfirmationTokenAsync(identityUser).Result;
            var result = await _userManager.ConfirmEmailAsync(identityUser, code);

            return await _userManager.CreateAsync(identityUser, password);
        }
        public static async Task<User> ToUser(ARIContext _context, UserDTO _userDTO)
        {
            try
            {
                return new User
                {
                    IdentityId = Guid.NewGuid().ToString(),
                    Firstname = _userDTO.Firstname.Trim(),
                    Lastname = _userDTO.Lastname.Trim(),
                    PasswordHash = IdentityConversions.MD5Hash(_userDTO.Password),
                    Email = _userDTO.Email.Trim(),
                    Telephone = _userDTO.Telephone,
                    Active = (byte)(_userDTO.IsActive ? 1 : 0),
                    RegistrationDateTime = DateTime.Now,
                    RoleId = _userDTO.RoleId,
                    Blocked = 0
                };
            }
            catch
            {
                await _context.SaveChangesAsync();
                return new User();
            }
        }

        [HttpPost]
        public async Task<JsonResult> UpdatePassword(IFormCollection _data)
        {
            if (!await _context.Users.AnyAsync(k => k.Email == _data["email"].ToString().Trim()))
                return Json(MessageConverter.GetResponse("Couldn't find the user", MethodResult.Failed));

            User user = await _context.Users.Where(k => k.Email == _data["email"].ToString().Trim()).FirstOrDefaultAsync();
            user.PasswordHash = IdentityConversions.MD5Hash(_data["password"].ToString());

            return Json(await UpdateUserAsync(_context, user, _data["password"].ToString(), _userManager));
        }
        public static async Task<ResponseDTO> UpdateUserAsync(ARIContext _context, User _user, string _password, UserManager<IdentityUser> _userManager)
        {
            try
            {
                if (_password != string.Empty)
                {
                    var identityUser = await _userManager.FindByEmailAsync(_user.Email);
                    var token = await _userManager.GeneratePasswordResetTokenAsync(identityUser);
                    var result = await _userManager.ResetPasswordAsync(identityUser, token, _password);
                    if (!result.Succeeded)
                        return MessageConverter.GetResponse(string.Empty, MethodResult.Failed);
                }

                _context.Update(_user);
                await _context.SaveChangesAsync();
                return MessageConverter.GetResponse(string.Empty, MethodResult.Succeeded);
            }
            catch (Exception ex)
            {
                return MessageConverter.GetResponse(string.Empty, MethodResult.Failed);
            }
        }

        //Utils
        public async Task<List<SelectListItem>> LoadRoles(int ignoreRoleId)
        {
            var roles = await _context.Roles.Where(k => k.RoleId != 1 && k.RoleId != 2).ToListAsync();
            var rolesListItems = new List<SelectListItem>();
            rolesListItems.Add(new SelectListItem { Text = "-", Value = "-" });
            foreach (var item in roles)
                if (item.RoleId != ignoreRoleId)
                    rolesListItems.Add(new SelectListItem { Text = item.Type, Value = item.RoleId.ToString() });
            return rolesListItems;
        }
        public async Task<IActionResult> DownloadUsers()
        {
            try
            {
                var role = ((ClaimsIdentity)User.Identity).FindAll("http://schemas.microsoft.com/ws/2008/06/identity/claims/role").ToList().LastOrDefault().Value;
                List<UserDTO> userDto = await _context.Users.Include(k => k.Role).Select(x => new UserDTO
                {
                    UserId = x.UserId,
                    RoleId = x.RoleId,
                    PassToken = protector.Decode(x.UserId.ToString()),
                    Firstname = x.Firstname,
                    Lastname = x.Lastname,
                    Email = x.Email,
                    Telephone = x.Telephone,
                    LastConnectionDateTime = ((x.LastConnectionDateTime != null) ?
                        x.LastConnectionDateTime.Value.ToString("dd/MM/yyyy HH:mm") : "-"),
                    Registered = x.RegistrationDateTime,
                    RoleName = x.Role.Type
                }).ToListAsync();

                StringBuilder stringBuilder = new StringBuilder();

                stringBuilder.AppendLine("Α/Α,Ονοματεπώνυμο,Ηλεκτρονική διεύθυνση,Τηλέφωνο,Τελευταία σύνδεση,Ενεργοποιημένος,Ρόλος");
                foreach (var item in userDto.Where(k => k.RoleId != (int)RoleType.NoRole))
                {
                    stringBuilder.AppendLine(string.Format("{0},{1},{2},{3},{4},{5},{6}",
                        "ΧΡ " + item.UserId,
                        item.Firstname + " " + item.Lastname,
                        item.Email,
                        item.Telephone,
                        item.LastConnectionDateTime,
                        "ΝΑΙ",
                        item.RoleName));
                }

                var data = Encoding.UTF8.GetBytes(stringBuilder.ToString());
                var result = Encoding.UTF8.GetPreamble().Concat(data).ToArray();
                return File(result, "text/csv", string.Format("{0}_report_{1}.csv", "ari_users", DateTimeOffset.Now.ToUnixTimeSeconds()));


                //return View(warehouseSuppliesDTOs);
            }
            catch (Exception ex)
            {
                return RedirectToAction("ExportPalletData", "PackagingLine");
            }
        }
        private ViewResult AuthenticationError(ViewResult viewResult, string key, string message)
        {
            ModelState.AddModelError(key, message);
            return viewResult;
        }
    }
}
*/
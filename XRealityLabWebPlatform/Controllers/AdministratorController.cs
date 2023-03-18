using Application.Security;
using XRealityLab.Common;
using XRealityLab.Models;
using XRealityLab.PDFTemplates.Models;
using XRealityLab.Users.Queries;
using Common.Constants;
using Infrastructure.Entities;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using EdiTools;
using Microsoft.AspNetCore.Http;
using System.Web;
using System.Net.Http.Headers;
using DocumentFormat.OpenXml.EMMA;
using Newtonsoft.Json;
using EnumsNET;
using Microsoft.AspNetCore.Authorization;
using Common.Helpers;
using Microsoft.AspNetCore.Identity;
using Common;

namespace XRealityLab.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class AdministratorController : Controller
    {
        private readonly XRealityLabdbContext _context;
        private IUserRepository _userRepository;
        private readonly IHostingEnvironment _environment;
        private readonly CustomIDataProtection _protector;
        private readonly UserManager<IdentityUser> _userManager;

        public AdministratorController(XRealityLabdbContext context, IHostingEnvironment IHostingEnvironment, CustomIDataProtection customIDataProtection,
            UserManager<IdentityUser> userManager)
        {
             _context = context;
            _userRepository = new UserRepository(_context);
            _environment = IHostingEnvironment;
            _userManager = userManager;
            _protector = customIDataProtection;
        }

        public async Task<IActionResult> Organizations(string actionMessage)
        {
            List<OrganizationDTO> organizationDTOs = await _context.Organization.Include(k => k.Itdirector).Select(k => new OrganizationDTO
            {
                OrganizationId = k.OrganizationId,
                PassToken = _protector.Decode(k.OrganizationId.ToString()),
                FullName = k.FullName,
                ITDirectorId = k.ItdirectorId,
                Address = k.Street + ", " + k.City + ", " + k.State + ", " + k.Country + ", " + k.Zipcode,
                Color = k.Color,
                PhotoUrl = k.PhotoUrl,
                NoAllowedLabs = k.NoAllowedLabs,
                Registered = k.Registered,
                SubscriptionStarted = k.SubscriptionStarted,
                SubscriptionExpiration = k.SubscriptionExpiration,
                ITDirector = new UserDTO
                {
                    UserId = k.Itdirector.UserId,
                    Firstname = k.Itdirector.Firstname,
                    Lastname = k.Itdirector.Lastname,
                    Email = k.Itdirector.Email,
                    Telephone = k.Itdirector.Telephone,
                    PhotoUrl = k.Itdirector.PhotoUrl,
                    RoleId = k.Itdirector.RoleId,
                    IsActive = k.Itdirector.IsActive == 1 ? true : false,
                    RegistrationDateTime = k.Itdirector.RegistrationDateTime
                }
            }).ToListAsync();      

            return View(organizationDTOs);
        }
        public IActionResult AddOrganization() { return View(); }
        [HttpPost]
        public async Task<IActionResult> AddOrganization(OrganizationDirectorDTO organizationDirectorDTO)
        {
            //Data validation
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(AddOrganization), new { _actionMessage = "Δεν προσθέσατε τα πεδία σωστά. Παρακαλώ προσπαθήστε ξανά." });
            }

            //Upload icon
            if (!string.IsNullOrWhiteSpace(organizationDirectorDTO.PhotoBase64))
            {

            }

            UserDTO userDTO = new UserDTO
            {
                Firstname = organizationDirectorDTO.Firstname,
                Lastname = organizationDirectorDTO.Lastname,
                Email = organizationDirectorDTO.Email,
                Password ="test123",
                RoleId = (int)RoleType.DirectorOfIT,
                PhotoUrl = PhotoUrl.DIRECTOR_OF_IT_ICON_PNG,
                Telephone = "698132154",
                RegistrationDateTime = DateTime.Now,
                IsActive = true
            };
            ResponseDTO responseDTO = await DataValidation.UserValidation(_context, userDTO);
            if (responseDTO.Result == "Failed")
                return Json(responseDTO);

            if (await _context.User.AnyAsync(k => k.Email == userDTO.Email.Trim()))
                return Json(MessageConverter.GetResponse("Failed to create the user", MethodResult.Failed));

            var hasCreated = await IdentityMigration.CreateIdentityUserAsync(_userManager, userDTO.Email.Trim(), userDTO.Password);
            if (!hasCreated.Succeeded)
                return Json(MessageConverter.GetResponse("Couldn't create the user identity", MethodResult.Failed));

            User user = await Migration.ToUser(_context, userDTO);

            if (user == null)
                return Json(MessageConverter.GetResponse("Failed to create the user", MethodResult.Failed));

            await Migration.InsertUserAsync(_context, user);
            if (_context.Organization.Where(k => k.Registered.AddDays(1) > DateTime.Now).Count() >= 10)
                return Json(MessageConverter.GetResponse("No new organizations can be added today", MethodResult.Failed));

            //Get director id
            var directorIT = _context.User.Where(k => k.Email == organizationDirectorDTO.Email).FirstOrDefault();

            //Save data
            Organization organization = new Organization
            {
                FullName = organizationDirectorDTO.FullName,
                ItdirectorId = directorIT.UserId,
                Street = organizationDirectorDTO.Street,
                City = organizationDirectorDTO.City,
                State = organizationDirectorDTO.State,
                Zipcode = organizationDirectorDTO.ZIPCode,
                Color = "#6a5738",
                PhotoUrl = PhotoUrl.ORGANIZATION_ICON_PNG,
                NoAllowedLabs = organizationDirectorDTO.NoAllowedLabs,
                SubscriptionStarted = organizationDirectorDTO.SubscriptionStarted,
                SubscriptionExpiration = organizationDirectorDTO.SubscriptionExpiration,
                Registered = DateTime.Now,
                Country = organizationDirectorDTO.Country
            };
            responseDTO = await DataValidation.OrganizationValidation(_context, organization);
            if (responseDTO.Result == Enum.GetName(typeof(RoleType), MethodResult.Failed))
            {
                _context.Remove(directorIT);
                await _context.SaveChangesAsync();
                return Json(responseDTO);
            }
                

            await _context.Organization.AddAsync(organization);
            await _context.SaveChangesAsync();


            return RedirectToAction("Organizations", "Administrator");
        }
        public IActionResult EditOrganization(string passToken)
        {
            OrganizationDirectorDTO organizationDirectorDTO = _context.Organization
                .Where(k=>k.OrganizationId == int.Parse(_protector.Encode(passToken))).Select(k => new OrganizationDirectorDTO
            {
                FullName = k.FullName,
                PassToken = _protector.Decode(k.OrganizationId.ToString()),
                UserId = k.ItdirectorId,
                Street = k.Street,
                City = k.City,
                State = k.State,
                Country = k.Country,
                ZIPCode = k.Zipcode,
                Color = k.Color,
                PhotoUrl = k.PhotoUrl,
                NoAllowedLabs = k.NoAllowedLabs,
                SubscriptionStarted = k.SubscriptionStarted,
                SubscriptionExpiration = k.SubscriptionExpiration,
                Registered = DateTime.Now,
            }).FirstOrDefault();

            var directorIT = _context.User.Where(k => k.UserId == organizationDirectorDTO.UserId).FirstOrDefault();

            organizationDirectorDTO.Rank = "Rank";
            organizationDirectorDTO.Firstname = directorIT.Firstname;
            organizationDirectorDTO.Lastname = directorIT.Lastname;
            organizationDirectorDTO.Email = directorIT.Email;
            organizationDirectorDTO.Telephone = directorIT.Telephone;
            organizationDirectorDTO.UserPhotoUrl = directorIT.PhotoUrl;
            organizationDirectorDTO.IsActive = directorIT.IsActive == 1? true : false;
            organizationDirectorDTO.RegistrationDateTime = directorIT.RegistrationDateTime;

            return View(organizationDirectorDTO);
        }
        [HttpPost]
        public async Task<IActionResult> EditOrganization(OrganizationDirectorDTO organizationDirectorDTO)
        {
            //Data validation
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(AddOrganization), new { _actionMessage = "Δεν προσθέσατε τα πεδία σωστά. Παρακαλώ προσπαθήστε ξανά." });
            }

            //Get director id
            var directorIT = _context.User.Where(k => k.Email == organizationDirectorDTO.Email).FirstOrDefault();

            //Upload icon
            if (!string.IsNullOrWhiteSpace(organizationDirectorDTO.PhotoBase64))
            {
                //Check if the file is jpg or png
                //
                //if (!(FileExtension == ".jpg" || FileExtension == ".png") && file.Length > 220000)
                //    return RedirectToAction(nameof(EditProfile));


                //CONVERT base64 to Image <-Library

                //Reference GeneralController\UpdateProfilePhoto
                //byte[] bytes = Convert.FromBase64String("R0lGODlhAQABAIAAAAAAAAAAACH5BAAAAAAALAAAAAABAAEAAAICTAEAOw==");
                //..
                //var image = Converters.resizeImage(Image.FromStream(file.OpenReadStream()), new Size(150, 150));

                //newFileName = myUniqueFileName + FileExtension;
                /*fileName = Path.Combine(_environment.WebRootPath, "images", "profilephoto") + $@"\{newFileName}";
                image.Save(fileName);

                //SAVE LOCAL TO WWWROOT


                //UPLOAD

                bool isUploaded = await ContainerService.UploadOperationAsync(Path.Combine(_environment.WebRootPath, "images", "profilephoto"),
                    newFileName, AzureGPV2Credentials.CONTAINER_PROFILE_PHOTO_SHARE);

                if (isUploaded)
                {
                    var user = _context.User.Where(k => k.UserId == _profileId).FirstOrDefault();
                    user.PhotoUrl = Path.Combine(BaseLink.AZURE_BLOB_URL, PhotoUrl.TRAINING_WEB_PROFILE_PHOTOS_DIRECTORY, newFileName);
                    await Migration.UpdateUserAsync(_context, _configuration, _protector, user, string.Empty, User, _userManager,
                        this.GetType().Name, nameof(EditProfile), ActionTypeID.UpdatePhoto,
                        string.Format("update profile photo icon [{0}]", user.FirstName + " " + user.LastName), HttpContext);
                    return RedirectToAction(nameof(EditProfile));
                
                //DELETE photo for update
                }
                */



            }

            //Update organization data
            Organization organization = _context.Organization.Where(k => k.OrganizationId == int.Parse(_protector.Encode(organizationDirectorDTO.PassToken))).FirstOrDefault();
            organization.OrganizationId = int.Parse(_protector.Encode(organizationDirectorDTO.PassToken));
            organization.FullName = organizationDirectorDTO.FullName;
            organization.ItdirectorId = directorIT.UserId;
            organization.Street = organizationDirectorDTO.Street;
            organization.City = organizationDirectorDTO.City;
            organization.State = organizationDirectorDTO.State;
            organization.Zipcode = organizationDirectorDTO.ZIPCode;
            organization.Color = organizationDirectorDTO.Color;
            organization.PhotoUrl = PhotoUrl.ORGANIZATION_ICON_PNG;
            organization.NoAllowedLabs = organizationDirectorDTO.NoAllowedLabs;
            organization.SubscriptionStarted = organizationDirectorDTO.SubscriptionStarted;
            organization.SubscriptionExpiration = organizationDirectorDTO.SubscriptionExpiration;
            organization.Registered = DateTime.Now;
            organization.Country = organizationDirectorDTO.Country;
            ResponseDTO responseDTO = await DataValidation.OrganizationValidation(_context, organization);
            if (responseDTO.Result == Enum.GetName(typeof(RoleType), MethodResult.Failed))
                return Json(responseDTO);

            _context.Organization.Update(organization);

            //Update user data
            directorIT.Firstname = organizationDirectorDTO.Firstname;
            directorIT.Lastname = organizationDirectorDTO.Lastname;
            directorIT.Telephone = organizationDirectorDTO.Telephone;

            _context.User.Update(directorIT);
             
            await _context.SaveChangesAsync();


            return RedirectToAction("Organizations", "Administrator");
        }
        public async Task<IActionResult> RemoveOrganization(string passToken)
        {
            Organization organization = await _context.Organization.Where(k => k.OrganizationId == int.Parse(_protector.Encode(passToken))).FirstOrDefaultAsync();
            await Migration.RemoveOrganizationDirectorAsync(_context, _userManager, organization);

            return RedirectToAction(nameof(Organizations));
        }

        public IActionResult ChangeITDirector(string passToken/*organizationPassToken*/) { return View(); }
        [HttpPost]
        public async Task<IActionResult> ChangeITDirector(OrganizationDirectorDTO organizationDirectorDTO)
        {
            //Data validation
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(AddOrganization), new { _actionMessage = "Δεν προσθέσατε τα πεδία σωστά. Παρακαλώ προσπαθήστε ξανά." });
            }

            ///Write your code
            ///

            //_context.Organization.Update(..);
            //_context.User.Update(directorIT);

            await _context.SaveChangesAsync();


            return RedirectToAction("Organizations", "Administrator");
        }
    }
}
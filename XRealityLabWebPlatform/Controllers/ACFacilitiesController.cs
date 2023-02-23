/*using Application.Security;
using ARI.Models;
using ARI.Application;
using ARI.Common;
using ARI.Users.Queries;
using Common.Constants;
using Infrastructure.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Wkhtmltopdf.NetCore;
using iTextSharp.text.pdf;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf.parser;
using ARI.PDFTemplates.Models;
using EdiTools;
using System.Xml.Linq;
using System.Text;
using Newtonsoft.Json;


namespace ARI.Controllers
{
    [Authorize(Roles = "Υπεύθυνος Ασφαλείας,Υπεύθυνος Ροών Εταιρίας")]
    public class ACFacilitiesController : Controller
    {
        private readonly ARIContext _context;
        private IUserRepository _userRepository;

        private readonly CustomIDataProtection protector;

        public ACFacilitiesController(ARIContext context, CustomIDataProtection customIDataProtection)
        {
            _context = context;
            _userRepository = new UserRepository(_context);
            protector = customIDataProtection;
        }

        //Truck
        public async Task<IActionResult> TruckList()
        {
            List<GuestDTO> userDto = await _context.Guests.Include(k => k.GuestCompany).Include(k => k.GuestUser).Where(k => k.GuestUser.Type == "Truck").Select(x => new GuestDTO
            {
                GuestId = x.GuestId,
                PassToken = protector.Decode(x.GuestId.ToString()),
                FirstName = x.GuestUser.Firstname,
                LastName = x.GuestUser.Lastname,
                CompanyName = x.GuestCompany.Name,
                Type = x.GuestUser.Type,
                LicensePlate = x.LicensePlate,
                Telephone = x.Telephone,
                VisitationPurpose = x.VisitationPurpose,
                ArrivalDateTime = x.ArrivalDateTime,
                DepartureDateTime = x.DepartureDateTime,
                IsIncorrect = x.IsIncorrect == 1 ? true : false,
                VisitingDuration = (x.DepartureDateTime == null) ? (int)(DateTime.Now.AddMinutes(180.0f) - x.ArrivalDateTime).TotalMinutes : (int)(x.DepartureDateTime - x.ArrivalDateTime).Value.TotalMinutes,
                Sign = x.Sign
            }).ToListAsync();

            return View(userDto);
        }

        public IActionResult AddTruck()
        {
            return View(new GuestDTO { CompanyListItems = LoadGuestCompanies().Result.OrderBy(k => k.Text), SelectedCompany = "-" });
        }
        [HttpPost]
        public async Task<IActionResult> AddTruck([Bind("FirstName,LastName,CompanyName,SelectedCompany,VisitationPurpose,Telephone,LicensePlate,ArrivalDateTime,DepartureDateTime,Sign")] GuestDTO guestDto)
        {
            if (!ModelState.IsValid) return RedirectToAction(nameof(AddTruck));

            if ((string.IsNullOrWhiteSpace(guestDto.CompanyName) || guestDto.CompanyName.Length <= 2 || guestDto.CompanyName.Length >= 20) && (guestDto.SelectedCompany == "-" || guestDto.SelectedCompany == null))
            {
                guestDto.CompanyListItems = LoadGuestCompanies().Result.OrderBy(k => k.Text);
                guestDto.SelectedCompany = "-";
                return AuthenticationError(View(guestDto), string.Empty, "Συμπληρώστε το πεδίο Εταιρία");
            }

            if (guestDto.LicensePlate == null) guestDto.LicensePlate = string.Empty;

            if(guestDto.LicensePlate.Length <= 3 && guestDto.LicensePlate.Length >= 10)
            {
                guestDto.CompanyListItems = LoadGuestCompanies().Result.OrderBy(k => k.Text);
                guestDto.SelectedCompany = "-";
                return AuthenticationError(View(guestDto), string.Empty, "Συμπληρώστε το πεδίο Πινακίδα");
            }

            if (guestDto.DepartureDateTime != null)
            {
                if (guestDto.DepartureDateTime.Value <= guestDto.ArrivalDateTime)
                {
                    guestDto.CompanyListItems = LoadGuestCompanies().Result.OrderBy(k => k.Text);
                    guestDto.SelectedCompany = "-";
                    return AuthenticationError(View(guestDto), string.Empty, "Η ημερομηνία αναχώρησης είναι μικρότερη από την ημερομηνία άφιξης.");
                }
            }

            int guestUserId = 0;
            var guestUser = _context.GuestUsers.Where(k => k.Firstname == guestDto.FirstName.Trim() && k.Lastname == guestDto.LastName.Trim()).FirstOrDefault();

            if (guestUser == null)
            {
                var guestUserToAdd = new GuestUser { Firstname = guestDto.FirstName.Trim(), Lastname = guestDto.LastName.Trim(), Type = "Truck" };
                var _guestUser = _context.GuestUsers.OrderByDescending(k => k.GuestUserId).FirstOrDefault();
                if (_guestUser != null)
                    guestUserId = _context.GuestUsers.OrderByDescending(k => k.GuestUserId).FirstOrDefault().GuestUserId + 1;
                else
                    guestUserId = 1;
            }
            else
                guestUserId = guestUser.GuestUserId;

            string guestCompanyName = string.Empty;
            guestCompanyName = (string.IsNullOrEmpty(guestDto.CompanyName)) ? _context.GuestCompanies.FirstOrDefault(k => k.GuestCompanyId == int.Parse(guestDto.SelectedCompany)).Name : guestDto.CompanyName.Trim();

            int guestCompanyId = 0;
            var guestCompany = _context.GuestCompanies.Where(k => k.Name == guestCompanyName).FirstOrDefault();

            if (guestCompany == null)
            {
                //TO DO
                var guestCompanyToAdd = new GuestCompany { Name = guestCompanyName };
                if (_context.GuestCompanies.OrderByDescending(k => k.GuestCompanyId).Count() != 0)
                {
                    guestCompanyId = _context.GuestCompanies.OrderByDescending(k => k.GuestCompanyId).FirstOrDefault().GuestCompanyId + 1;
                }
                else
                    guestCompanyId = 1;
            }
            else
                guestCompanyId = guestCompany.GuestCompanyId;

            Guest guest = new Guest
            {
                GuestUser = (guestUser != null) ? (guestUser.Type == "Truck" ? guestUser : new GuestUser { Firstname = guestDto.FirstName.Trim(), Lastname = guestDto.LastName.Trim(), Type = "Truck" } )
                    : new GuestUser { Firstname = guestDto.FirstName.Trim(), Lastname = guestDto.LastName.Trim(), Type = "Truck" },
                GuestUserId = guestUserId,
                GuestCompany = (guestCompany != null)? guestCompany : new GuestCompany { Name = guestCompanyName },
                GuestCompanyId = guestCompanyId,
                VisitationPurpose = guestDto.VisitationPurpose,
                LicensePlate = guestDto.LicensePlate,
                Telephone = guestDto.Telephone,
                ArrivalDateTime = guestDto.ArrivalDateTime,
                IsIncorrect = 0,
                Sign = string.IsNullOrEmpty(guestDto.Sign) ? null : guestDto.Sign,

                DepartureDateTime = guestDto.DepartureDateTime
            };
            _context.Add(guest);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(TruckList));
        }

        public async Task<IActionResult> EditTruck(string passToken)
        {
            if (passToken == null || passToken == string.Empty) return NotFound();

            try
            {
                var guestDto = await _context.Guests.Where(k => k.GuestId == int.Parse(protector.Encode(passToken))).Select(x => new GuestDTO
                {
                    GuestId = x.GuestId,
                    PassToken = protector.Decode(x.GuestId.ToString())
                }).FirstOrDefaultAsync();

                return View(guestDto);
            }
            catch
            {
                return NotFound();
            }
        }
        [HttpPost]
        public async Task<IActionResult> EditTruck(string passToken, [Bind("DepartureDateTime,Sign")] GuestDTO guestDto)
        {
            if (passToken == null || passToken == string.Empty || guestDto.DepartureDateTime == null) return RedirectToAction(nameof(EditTruck));

            try
            {
                var guest = await _context.Guests.FirstOrDefaultAsync(k => k.GuestId == int.Parse(protector.Encode(passToken)));

                if (guestDto.DepartureDateTime.Value <= guest.ArrivalDateTime)
                {
                    guestDto.GuestId = guest.GuestId;
                    guestDto.PassToken = protector.Decode(guest.GuestId.ToString());
                    return AuthenticationError(View(guestDto), string.Empty, "Η ημερομηνία αναχώρησης είναι μικρότερη από την ημερομηνία άφιξης.");
                }

                guest.DepartureDateTime = guestDto.DepartureDateTime;

                _context.Guests.Update(guest);
                _context.SaveChanges();

                return RedirectToAction(nameof(TruckList));
            }
            catch
            {
                return NotFound();
            }
        }


        //Visitor
        public async Task<IActionResult> VisitorList()
        {
            List<GuestDTO> userDto = await _context.Guests.Include(k => k.GuestCompany).Include(k => k.GuestUser).Where(k => k.GuestUser.Type == "Visitor").Select(x => new GuestDTO
            {
                GuestId = x.GuestId,
                PassToken = protector.Decode(x.GuestId.ToString()),
                FirstName = x.GuestUser.Firstname,
                LastName = x.GuestUser.Lastname,
                CompanyName = x.GuestCompany.Name,
                Type = x.GuestUser.Type,
                LicensePlate = x.LicensePlate,
                VisitationPurpose = x.VisitationPurpose,
                Telephone = x.Telephone,
                ArrivalDateTime = x.ArrivalDateTime,
                DepartureDateTime = x.DepartureDateTime,
                IsIncorrect = x.IsIncorrect == 1 ? true : false,
                VisitingDuration = (x.DepartureDateTime == null) ? (int)(DateTime.Now.AddMinutes(180.0f) - x.ArrivalDateTime).TotalMinutes : (int)(x.DepartureDateTime - x.ArrivalDateTime).Value.TotalMinutes,
                Sign = x.Sign
            }).ToListAsync();

            return View(userDto);
        }

        public IActionResult AddVisitor()
        {
            return View(new GuestDTO { CompanyListItems = LoadGuestCompanies().Result.OrderBy(k => k.Text), SelectedCompany = "-" });
        }
        [HttpPost]
        public async Task<IActionResult> AddVisitor([Bind("FirstName,LastName,CompanyName,SelectedCompany,VisitationPurpose,Telephone,ArrivalDateTime,DepartureDateTime,Sign")] GuestDTO guestDto)
        {
            if (!ModelState.IsValid) RedirectToAction(nameof(AddVisitor));

            if ((string.IsNullOrWhiteSpace(guestDto.CompanyName) || guestDto.CompanyName.Length <= 2 || guestDto.CompanyName.Length >= 20) && (guestDto.SelectedCompany == "-" || guestDto.SelectedCompany == null))
            {
                guestDto.CompanyListItems = LoadGuestCompanies().Result.OrderBy(k => k.Text);
                guestDto.SelectedCompany = "-";
                return AuthenticationError(View(guestDto), string.Empty, "Συμπληρώστε το πεδίο Εταιρία");
            }

            if (guestDto.DepartureDateTime != null)
            {
                if (guestDto.DepartureDateTime.Value <= guestDto.ArrivalDateTime)
                {
                    guestDto.CompanyListItems = LoadGuestCompanies().Result.OrderBy(k => k.Text);
                    guestDto.SelectedCompany = "-";
                    return AuthenticationError(View(guestDto), string.Empty, "Η ημερομηνία αναχώρησης είναι μικρότερη από την ημερομηνία άφιξης.");
                }
            }

            //GuestUser
            int guestUserId = 0;
            var guestUser = _context.GuestUsers.Where(k => k.Firstname == guestDto.FirstName.Trim() && k.Lastname == guestDto.LastName.Trim()).FirstOrDefault();

            if (guestUser == null)
            {
                var guestUserToAdd = new GuestUser { Firstname = guestDto.FirstName.Trim(), Lastname = guestDto.LastName.Trim(), Type = "Visitor" };
                if (_context.GuestUsers.Count() == 0)
                    guestUserId = 1;
                else
                    guestUserId = _context.GuestUsers.OrderByDescending(k => k.GuestUserId).FirstOrDefault().GuestUserId + 1;
            }
            else
                guestUserId = guestUser.GuestUserId;

            //GuestCompany
            string guestCompanyName = string.Empty;
            guestCompanyName = (guestDto.SelectedCompany != "-" && guestDto.SelectedCompany != null) ? _context.GuestCompanies.FirstOrDefault(k => k.GuestCompanyId == int.Parse(guestDto.SelectedCompany)).Name : guestDto.CompanyName.Trim();

            int guestCompanyId = 0;
            var guestCompany = _context.GuestCompanies.Where(k => k.Name == guestCompanyName).FirstOrDefault();

            if (guestCompany == null)
            {
                var guestCompanyToAdd = new GuestCompany { Name = guestCompanyName };
                if (_context.GuestCompanies.OrderByDescending(k => k.GuestCompanyId).Count() != 0)
                {
                    guestCompanyId = _context.GuestCompanies.OrderByDescending(k => k.GuestCompanyId).FirstOrDefault().GuestCompanyId + 1;
                }
                else
                    guestCompanyId = 1;
            }
            else
                guestCompanyId = guestCompany.GuestCompanyId;

            //Guest
            Guest guest = new Guest
            {
                GuestUser = (guestUser != null) ? (guestUser.Type == "Visitor" ? guestUser : new GuestUser { Firstname = guestDto.FirstName.Trim(), Lastname = guestDto.LastName.Trim(), Type = "Visitor" })
                    : new GuestUser { Firstname = guestDto.FirstName.Trim(), Lastname = guestDto.LastName.Trim(), Type = "Visitor" },
                GuestUserId = guestUserId,
                GuestCompany = (guestCompany != null) ? guestCompany : new GuestCompany { Name = guestCompanyName },
                GuestCompanyId = guestCompanyId,
                VisitationPurpose = guestDto.VisitationPurpose,
                Telephone = guestDto.Telephone,
                ArrivalDateTime = guestDto.ArrivalDateTime,
                IsIncorrect = 0,
                Sign = string.IsNullOrEmpty(guestDto.Sign) ? null : guestDto.Sign,
                DepartureDateTime = guestDto.DepartureDateTime
            };
            _context.Add(guest);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(VisitorList));
        }

        public async Task<IActionResult> EditVisitor(string passToken)
        {
            if (passToken == null || passToken == string.Empty) return NotFound();
            
            try
            {
                var guestDto = await _context.Guests.Where(k => k.GuestId == int.Parse(protector.Encode(passToken))).Select(x => new GuestDTO
                {
                    GuestId = x.GuestId,
                    PassToken = protector.Decode(x.GuestId.ToString())
                }).FirstOrDefaultAsync();

                return View(guestDto);
            }
            catch
            {
                return NotFound();
            }
        }
        [HttpPost]
        public async Task<IActionResult> EditVisitor(string passToken, [Bind("DepartureDateTime,Sign")] GuestDTO guestDto)
        {
            if (passToken == null || passToken == string.Empty || guestDto.DepartureDateTime == null) RedirectToAction(nameof(EditVisitor));

            try
            {
                var guest = await _context.Guests.FirstOrDefaultAsync(k => k.GuestId == int.Parse(protector.Encode(passToken)));

                if (guestDto.DepartureDateTime.Value <= guest.ArrivalDateTime)
                {
                    guestDto.GuestId = guest.GuestId;
                    guestDto.PassToken = protector.Decode(guest.GuestId.ToString());
                    return AuthenticationError(View(guestDto), string.Empty, "Η ημερομηνία αναχώρησης είναι μικρότερη από την ημερομηνία άφιξης.");
                }

                guest.DepartureDateTime = guestDto.DepartureDateTime;

                _context.Guests.Update(guest);
                _context.SaveChanges();

                return RedirectToAction(nameof(VisitorList));
            }
            catch
            {
                return NotFound();
            }
        }


        //Export
        public async Task<IActionResult> DownloadTruckList()
        {
            try
            {
                List<GuestDTO> userDto = await _context.Guests.Include(k => k.GuestUser).Where(k => k.GuestUser.Type == "Truck").Select(x => new GuestDTO
                {
                    GuestId = x.GuestId,
                    PassToken = protector.Decode(x.GuestId.ToString()),
                    FirstName = x.GuestUser.Firstname,
                    LastName = x.GuestUser.Lastname,
                    CompanyName = x.GuestCompany.Name,
                    Type = x.GuestUser.Type,
                    LicensePlate = x.LicensePlate,
                    Telephone = x.Telephone,
                    VisitationPurpose = x.VisitationPurpose,
                    ArrivalDateTime = x.ArrivalDateTime,
                    DepartureDateTime = x.DepartureDateTime,
                    IsIncorrect = x.IsIncorrect == 1 ? true : false,
                    VisitingDuration = (x.DepartureDateTime == null) ? (int)(DateTime.Now.AddMinutes(180.0f) - x.ArrivalDateTime).TotalMinutes : (int)(x.DepartureDateTime - x.ArrivalDateTime).Value.TotalMinutes,
                    Sign = x.Sign
                }).ToListAsync();

                StringBuilder stringBuilder = new StringBuilder();

                stringBuilder.AppendLine("Α/Α," +
                    "Ονοματεπώνυμο, " +
                    "Εταιρία, " +
                    "Σκοπός Επίσκεψης, " +
                    "Πινακίδα Οχήματος, " +
                    "Τηλέφωνο, " +
                    "Ημερομηνία Εισόδου, " +
                    "Ημερομηνία Εξόδου, " +
                    "Χρόνος Εντός της Εταιρίας, " +
                    "Υπέγραψε, " +
                    "Κατάσταση");

                List<GuestDTO> sortedList = userDto.OrderByDescending(x => x.ArrivalDateTime).ToList();
                foreach (var item in sortedList)
                {
                    string status = "";
                    if (item.IsIncorrect)
                        status = "Άκυρο";
                    else
                        status = ((item.ArrivalDateTime > DateTime.Now && DateTime.Now < item.DepartureDateTime) || item.DepartureDateTime == null) ? "Είναι εντός της εταιρίας" : "Έχει αποχωρήσει";
                    string visitingDurationString = "";

                    if (item.VisitingDuration > 60)
                        visitingDurationString = Convert.ToInt32(item.VisitingDuration / 60.0f).ToString() + " ώρες " + Convert.ToInt32(item.VisitingDuration % 60.0f).ToString() + " λεπτά";
                    else
                        visitingDurationString = Convert.ToInt32(item.VisitingDuration % 60.0f).ToString() + " λεπτά";

                    stringBuilder.AppendLine(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10}",
                        "ΚΦ " + item.GuestId,
                        item.FirstName + " " + item.LastName,
                        item.CompanyName,
                        item.VisitationPurpose,
                        item.LicensePlate,
                        item.Telephone,
                        item.ArrivalDateTime,
                        item.DepartureDateTime,
                        visitingDurationString,
                        item.Sign,
                        status));
                }

                var data = Encoding.UTF8.GetBytes(stringBuilder.ToString());
                var result = Encoding.UTF8.GetPreamble().Concat(data).ToArray();
                return File(result, "text/csv", string.Format("{0}_report_{1}.csv", "ari_truckList", DateTimeOffset.Now.ToUnixTimeSeconds()));
            }
            catch (Exception ex)
            {
                return RedirectToAction("ExportPalletData", "PackagingLine");
            }
        }
        public async Task<IActionResult> DownloadVisitorList()
        {
            try
            {
                List<GuestDTO> userDto = await _context.Guests.Include(k => k.GuestCompany).Include(k => k.GuestUser).Where(k => k.GuestUser.Type == "Visitor").Select(x => new GuestDTO
                {
                    GuestId = x.GuestId,
                    PassToken = protector.Decode(x.GuestId.ToString()),
                    FirstName = x.GuestUser.Firstname,
                    LastName = x.GuestUser.Lastname,
                    CompanyName = x.GuestCompany.Name,
                    Type = x.GuestUser.Type,
                    LicensePlate = x.LicensePlate,
                    Telephone = x.Telephone,
                    VisitationPurpose = x.VisitationPurpose,
                    ArrivalDateTime = x.ArrivalDateTime,
                    DepartureDateTime = x.DepartureDateTime,
                    IsIncorrect = x.IsIncorrect == 1 ? true : false,
                    VisitingDuration = (x.DepartureDateTime == null) ? (int)(DateTime.Now.AddMinutes(180.0f) - x.ArrivalDateTime).TotalMinutes : (int)(x.DepartureDateTime - x.ArrivalDateTime).Value.TotalMinutes,
                    Sign = x.Sign
                }).ToListAsync();

                StringBuilder stringBuilder = new StringBuilder();

                stringBuilder.AppendLine("Α/Α," +
                "Ονοματεπώνυμο, " +
                "Εταιρία, " +
                "Σκοπός Επίσκεψης, " +
                "Τηλέφωνο, " +
                "Ημερομηνία Εισόδου, " +
                "Ημερομηνία Εξόδου, " +
                "Χρόνος Εντός της Εταιρίας, " +
                "Υπέγραψε, " +
                "Κατάσταση");

                List<GuestDTO> sortedList = userDto.OrderByDescending(x => x.ArrivalDateTime).ToList();
                foreach (var item in sortedList)
                {
                    string status = "";
                    if (item.IsIncorrect)
                        status = "Άκυρο";
                    else
                        status = ((item.ArrivalDateTime > DateTime.Now && DateTime.Now < item.DepartureDateTime) || item.DepartureDateTime == null) ? "Είναι εντός της εταιρίας" : "Έχει αποχωρήσει";
                    string visitingDurationString = "";

                    if (item.VisitingDuration > 60)
                        visitingDurationString = Convert.ToInt32(item.VisitingDuration / 60.0f).ToString() + " ώρες " + Convert.ToInt32(item.VisitingDuration % 60.0f).ToString() + " λεπτά";
                    else
                        visitingDurationString = Convert.ToInt32(item.VisitingDuration % 60.0f).ToString() + " λεπτά";

                    stringBuilder.AppendLine(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9}",
                        "ΚΦ " + item.GuestId,
                        item.FirstName + " " + item.LastName,
                        item.CompanyName,
                        item.VisitationPurpose,
                        item.Telephone,
                        item.ArrivalDateTime,
                        item.DepartureDateTime,
                        visitingDurationString,
                        item.Sign,
                        status));
                }

                var data = Encoding.UTF8.GetBytes(stringBuilder.ToString());
                var result = Encoding.UTF8.GetPreamble().Concat(data).ToArray();
                return File(result, "text/csv", string.Format("{0}_report_{1}.csv", "ari_visitorsList", DateTimeOffset.Now.ToUnixTimeSeconds()));
            }
            catch (Exception ex)
            {
                return RedirectToAction("ExportPalletData", "PackagingLine");
            }
        }


        //Helpers
        public async Task<List<SelectListItem>> LoadGuestCompanies()
        {
            var guestCompanies = await _context.GuestCompanies.ToListAsync();
            var companiesListItems = new List<SelectListItem>();
            companiesListItems.Add(new SelectListItem { Text = "-", Value = "-" });
            foreach (var item in guestCompanies)
                if (!companiesListItems.Select(k => k.Text).Contains(item.Name))
                    companiesListItems.Add(new SelectListItem { Text = item.Name, Value = item.GuestCompanyId.ToString() });
            return companiesListItems;
        }
        public async Task<IActionResult> CleanGuest(string passToken)
        {
            if (passToken == null || passToken == string.Empty) return NotFound();

            var guest = await _context.Guests.Include(k => k.GuestUser).FirstOrDefaultAsync(k => k.GuestId == int.Parse(protector.Encode(passToken)));
            if (guest.IsIncorrect == 0)
                guest.IsIncorrect = 1;
            else
                guest.IsIncorrect = 0;

            _context.Guests.Update(guest);
            _context.SaveChanges();
            return RedirectToAction(guest.GuestUser.Type == "Truck" ? nameof(TruckList) : nameof(VisitorList));
        }
        private ViewResult AuthenticationError(ViewResult viewResult, string key, string message)
        {
            ModelState.AddModelError(key, message);
            return viewResult;
        }
    }
}
*/
using Application.Security;
using ARI.Application;
using ARI.Common;
using ARI.Models;
using ARI.Users.Queries;
using Common.Constants;
using Infrastructure.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Sap.Data.SQLAnywhere;
using System.Text;

namespace ARI.Controllers
{
    [Authorize(Roles = "IT,Υπεύθυνος Ροών Εταιρίας")]
    public class EquipmentController : Controller
    {
        private readonly ARIContext _context;
        private IUserRepository _userRepository;
        private readonly IHostingEnvironment _environment;
        private readonly CustomIDataProtection protector;

        public EquipmentController(ARIContext context, IHostingEnvironment IHostingEnvironment, CustomIDataProtection customIDataProtection)
        {
            _context = context;
            _userRepository = new UserRepository(_context);
            _environment = IHostingEnvironment;
            protector = customIDataProtection;
        }

        public IActionResult Index()
        {
            //string pwd = "test";
            ////ex SAConnection conn = new SAConnection("Host=sqla-host:2638;Server=sqla-server;UserID=JSmith;Password=secret");
            //SAConnection conn = new SAConnection("Data Source=SAP IQ 16 Demo;Password=" + pwd);

            //try
            //{
            //    conn.Open();

            //    conn.Close();
            //}
            //catch (SAException ex)
            //{
            //    Console.WriteLine(ex.Errors[0].Source + " : " +
            //         ex.Errors[0].Message + " (" +
            //         ex.Errors[0].NativeError.ToString() + ")",
            //         "Failed to connect");
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex.Message, "Failed to connect");
            //}

            return RedirectToAction(nameof(CompanyCarsList));
        }

        public async Task<IActionResult> CompanyCarsList()
        {
            List<CompanyCarReservationDTO> userDto = await _context.CompanyCarReservations.Include(k => k.User).Include(k => k.CompanyCar).Select(x => new CompanyCarReservationDTO
            {
                CompanyCarReservationId = x.CompanyCarReservationId,
                PassToken = protector.Decode(x.CompanyCarReservationId.ToString()),
                Firstname = x.User.Firstname,
                Lastname = x.User.Lastname,
                CarModel = x.CompanyCar.Model,
                CarLicensePlate = x.CompanyCar.LicensePlate,
                CompanyCarId = x.CompanyCarId,
                InKilometers = x.InKilometers.Value,
                OutKilometers = (x.OutKilometers != null) ? x.OutKilometers.Value : 0,
                FuelLitre = (x.FuelLitre != null) ? x.FuelLitre.Value : 0,
                FuelCost = (x.FuelCost != null) ? x.FuelCost.Value : 0,
                Observations = (x.Observations != null) ? x.Observations : "-",
                MaintenanceType = (x.MaintenanceType != null) ? x.MaintenanceType : "-",
                MaintenanceCost = (x.MaintenanceCost != null) ? x.MaintenanceCost.Value : 0,
                ArrivalDateTime = x.ArrivalDateTime,
                DepartureDateTime = x.DepartureDateTime,
                VisitingDuration = x.DepartureDateTime == null ? (int)(DateTime.Now.AddMinutes(180.0f) - x.ArrivalDateTime).TotalMinutes : (int)(x.DepartureDateTime - x.ArrivalDateTime).Value.TotalMinutes,
                Sign = x.Sign,
                IsIncorrect = x.IsIncorrect == 1 ? true : false
            }).ToListAsync();

            return View(userDto);
        }

        public async Task<IActionResult> DownloadCarList()
        {
            try
            {
                List<CompanyCarReservationDTO> companyCarsList = await _context.CompanyCarReservations.Include(k => k.User).Include(k => k.CompanyCar).Select(x => new CompanyCarReservationDTO
                {
                    CompanyCarReservationId = x.CompanyCarReservationId,
                    PassToken = protector.Decode(x.CompanyCarReservationId.ToString()),
                    Firstname = x.User.Firstname,
                    Lastname = x.User.Lastname,
                    CarModel = x.CompanyCar.Model,
                    CarLicensePlate = x.CompanyCar.LicensePlate,
                    CompanyCarId = x.CompanyCarId,
                    InKilometers = x.InKilometers.Value,
                    OutKilometers = (x.OutKilometers != null) ? x.OutKilometers.Value : 0,
                    FuelLitre = (x.FuelLitre != null) ? x.FuelLitre.Value : 0,
                    FuelCost = (x.FuelCost != null) ? x.FuelCost.Value : 0,
                    Observations = (x.Observations != null) ? x.Observations : "-",
                    MaintenanceType = (x.MaintenanceType != null) ? x.MaintenanceType : "-",
                    MaintenanceCost = (x.MaintenanceCost != null) ? x.MaintenanceCost.Value : 0,
                    ArrivalDateTime = x.ArrivalDateTime,
                    DepartureDateTime = x.DepartureDateTime,
                    VisitingDuration = x.DepartureDateTime == null ? (int)(DateTime.Now.AddMinutes(180.0f) - x.ArrivalDateTime).TotalMinutes : (int)(x.DepartureDateTime - x.ArrivalDateTime).Value.TotalMinutes,
                    Sign = x.Sign,
                    IsIncorrect = x.IsIncorrect == 1 ? true : false
                }).ToListAsync();

                companyCarsList = companyCarsList.OrderByDescending(x => x.CompanyCarReservationId).ToList();
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine("A/A ,Ονοματεπώνυμο, Αυτοκίνητο,Ημερομηνία χρέωσης,Ημερομηνία παράδοσης,Χρόνος κράτησης,Χιλιόμετρα,Λίτρα καυσίμου,Κόστος καυσίμου,Είδος συντήρησης,Κόστος συντήρησης,Γενικές παρατηρήσεις,Υπέγραψε,Κατάσταση");

                foreach (var item in companyCarsList)
                {
                    string fuelLitre = string.Empty;
                    if (item.FuelLitre != 0)
                        fuelLitre = item.FuelLitre.ToString() + " λτ";
                    else
                        fuelLitre = "-";

                    string fuelCost = string.Empty;
                    if (item.FuelCost != 0)
                        fuelCost = item.FuelCost.ToString() + " €";
                    else
                        fuelCost = "-";

                    string status = "";

                    if(item.IsIncorrect == false)
                            {
                        if(item.ArrivalDateTime < DateTime.Now)
                                {
                            if(item.DepartureDateTime != null)
                                    {
                                if(item.DepartureDateTime > DateTime.Now)
                                        {
                                            status = "Σε χρήση";
                                        }
                                        else
                                        {
                                            status = "Παραδόθηκε";  
                                        }
                                    }
                                    else
                                    {
                                        status = "Σε χρήση";
                                    }
                                }
                                else
                                {
                                    status = "Παραδόθηκε";
                                }
                            }
                            else
                            {
                                status = "Άκυρο";
                            }

                    string visitingDurationString = "";
                    if (item.VisitingDuration > 60)
                        visitingDurationString = Convert.ToInt32(item.VisitingDuration / 60.0f).ToString() + " ώρες " + Convert.ToInt32(item.VisitingDuration % 60.0f).ToString() + " λεπτά";
                    else
                        visitingDurationString = Convert.ToInt32(item.VisitingDuration % 60.0f).ToString() + " λεπτά";

                    string kilometers = "";
                    kilometers = item.InKilometers + " - " + item.OutKilometers;

                    stringBuilder.AppendLine(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13}", 
                        item.CompanyCarReservationId, 
                        item.Firstname + " " + item.Lastname, 
                        item.CarModel,
                        item.ArrivalDateTime, 
                        item.DepartureDateTime,
                        visitingDurationString,
                        kilometers,
                        fuelLitre,
                        fuelCost, 
                        item.MaintenanceType, 
                        item.MaintenanceCost, 
                        item.Observations,
                        item.Sign,
                        status));
                }

                var data = Encoding.UTF8.GetBytes(stringBuilder.ToString());
                var result = Encoding.UTF8.GetPreamble().Concat(data).ToArray();
                return File(result, "text/csv", string.Format("{0}_report_{1}.csv", "ari_companiesCars", DateTimeOffset.Now.ToUnixTimeSeconds()));
            }
            catch (Exception ex)
            {
                return RedirectToAction("ExportPalletData", "PackagingLine");
            }
        }

        public IActionResult AddCompanyCar()
        {
            return View(new CompanyCarReservationDTO
            {
                UserListItems = LoadUsers().Result.OrderBy(k => k.Text),
                SelectedUser = "-",
                CarListItems = LoadCars().Result.OrderBy(k => k.Text),
                SelectedCar = "-",
            });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCompanyCar([Bind("SelectedUser,SelectedCar,ArrivalDateTime,InKilometers")] CompanyCarReservationDTO companyCarReservationDto)
        {
            if (companyCarReservationDto.SelectedCar == "-" || companyCarReservationDto.SelectedCar == null || companyCarReservationDto.SelectedUser == "-" || companyCarReservationDto.SelectedUser == null || companyCarReservationDto.ArrivalDateTime.Equals(DateTime.MinValue))
            {
                companyCarReservationDto.UserListItems = LoadUsers().Result.OrderBy(k => k.Text);
                companyCarReservationDto.SelectedUser = "-";
                companyCarReservationDto.CarListItems = LoadCars().Result.OrderBy(k => k.Text);
                companyCarReservationDto.SelectedCar = "-";
                return AuthenticationError(View(companyCarReservationDto), string.Empty, "Εσφαλμένα πεδία, συμπληρώστε τα πεδία ξανά");
            }

            if (!ModelState.IsValid) return BadRequest();

            CompanyCarReservation companyCarReservation = new CompanyCarReservation
            {
                UserId = int.Parse(companyCarReservationDto.SelectedUser),
                CompanyCarId = int.Parse(companyCarReservationDto.SelectedCar),
                ArrivalDateTime = companyCarReservationDto.ArrivalDateTime,
                IsIncorrect = 0,
                InKilometers = companyCarReservationDto.InKilometers,
                RegistrationDateTime = DateTime.Now.AddMinutes(180.0f)
            };
            _context.Add(companyCarReservation);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(CompanyCarsList));
        }

        public async Task<IActionResult> EditCompanyCar(string passToken)
        {
            if (passToken == null || passToken == string.Empty) return NotFound();

            try
            {
                var companyCarReservationDto = await _context.CompanyCarReservations.Where(k => k.CompanyCarReservationId == int.Parse(protector.Encode(passToken))).Select(x => new CompanyCarReservationDTO
                {
                    CompanyCarReservationId = x.CompanyCarReservationId,
                    PassToken = protector.Decode(x.CompanyCarReservationId.ToString()),
                    InKilometers = x.InKilometers.Value
                }).FirstOrDefaultAsync();

                return View(companyCarReservationDto);
            }
            catch
            {
                return NotFound();
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCompanyCar(string passToken, [Bind("InKilometers,OutKilometers,FuelLitre,FuelCost,Observations,ΜaintenanceType,MaintenanceCost,DepartureDateTime,Sign")] CompanyCarReservationDTO companyCarReservationDto)
        {
            if (passToken == null || passToken == string.Empty || companyCarReservationDto.DepartureDateTime == null)
            {
                var _companyCarReservationDto = _context.CompanyCarReservations.Where(k => k.CompanyCarReservationId == int.Parse(protector.Encode(passToken))).Select(x => new CompanyCarReservationDTO
                {
                    CompanyCarReservationId = x.CompanyCarReservationId,
                    PassToken = protector.Decode(x.CompanyCarReservationId.ToString())
                }).FirstOrDefault();

                return View(_companyCarReservationDto);
            }

            try
            {
                var companyCarReservation = await _context.CompanyCarReservations.FirstOrDefaultAsync(k => k.CompanyCarReservationId == int.Parse(protector.Encode(passToken)));

                if (companyCarReservationDto.OutKilometers == 0)
                {
                    companyCarReservationDto = LoadCompanyCarReservationDto(companyCarReservation, companyCarReservationDto);
                    return AuthenticationError(View(companyCarReservationDto), string.Empty, "Συμπληρώστε τα χιλιόμετρα παράδοσης");
                }
                if (companyCarReservationDto.DepartureDateTime.Value <= companyCarReservation.ArrivalDateTime)
                {
                    companyCarReservationDto = LoadCompanyCarReservationDto(companyCarReservation, companyCarReservationDto);
                    return AuthenticationError(View(companyCarReservationDto), string.Empty, "Η ημερομηνία παράδοσης είναι μικρότερη από την ημερομηνία χρέωσης");
                }
                if (companyCarReservationDto.OutKilometers <= companyCarReservation.InKilometers)
                {
                    companyCarReservationDto = LoadCompanyCarReservationDto(companyCarReservation, companyCarReservationDto);
                    return AuthenticationError(View(companyCarReservationDto), string.Empty, "Τα χιλιόμετρα παράδοσης είναι μικρότερα από τα χιλιόμετρα παράδοσης.");
                }

                companyCarReservation.DepartureDateTime = companyCarReservationDto.DepartureDateTime;
                companyCarReservation.OutKilometers = companyCarReservationDto.OutKilometers;
                if (companyCarReservationDto.FuelLitre != 0.0f) companyCarReservation.FuelLitre = companyCarReservationDto.FuelLitre;
                if (companyCarReservationDto.FuelCost != 0.0f) companyCarReservation.FuelCost = companyCarReservationDto.FuelCost;
                companyCarReservation.Observations = companyCarReservationDto.Observations;
                companyCarReservation.MaintenanceType = companyCarReservationDto.MaintenanceType;
                if (companyCarReservationDto.MaintenanceCost != 0.0f) companyCarReservation.MaintenanceCost = companyCarReservationDto.MaintenanceCost;

                _context.CompanyCarReservations.Update(companyCarReservation);
                _context.SaveChanges();

                return RedirectToAction(nameof(CompanyCarsList));
            }
            catch
            {
                return NotFound();
            }
        }

        public async Task<IActionResult> CleanGuest(string passToken)
        {
            if (passToken == null || passToken == string.Empty) return NotFound();

            var companyCarReservation = await _context.CompanyCarReservations.FirstOrDefaultAsync(k => k.CompanyCarReservationId == int.Parse(protector.Encode(passToken)));
            if (companyCarReservation.IsIncorrect == 0)
                companyCarReservation.IsIncorrect = 1;
            else
                companyCarReservation.IsIncorrect = 0;

            _context.CompanyCarReservations.Update(companyCarReservation);
            _context.SaveChanges();
            return RedirectToAction(nameof(CompanyCarsList));
        }


        public async Task<List<SelectListItem>> LoadUsers()
        {
            var users = await _context.Users.Include(k => k.Role).Where(k => k.RoleId != (int)RoleType.NoRole).ToListAsync();
            var usersListItems = new List<SelectListItem>();
            usersListItems.Add(new SelectListItem { Text = "-", Value = "-" });
            foreach (var item in users)
                if (!usersListItems.Select(k => k.Text).Contains(item.Firstname + " " + item.Lastname))
                    usersListItems.Add(new SelectListItem { Text = item.Firstname + " " + item.Lastname, Value = item.UserId.ToString() });
            return usersListItems;
        }

        public async Task<List<SelectListItem>> LoadCars()
        {
            ////FIX IT
            
            var allCompanyCars = await _context.CompanyCars.ToListAsync();
            //var userdCompanyCars = await _context.CompanyCarReservations.Include(l => l.CompanyCar).Where(k => k.DepartureDateTime != null || k.DepartureDateTime < DateTime.Now.AddMinutes(180.0f)).Select(k => k.CompanyCar).Distinct().ToListAsync();
            //var availableCompanyCars = allCompanyCars.Except(userdCompanyCars);
            var companyCarListItems = new List<SelectListItem>();
            companyCarListItems.Add(new SelectListItem { Text = "-", Value = "-" });
            foreach (var item in allCompanyCars)
                if (!companyCarListItems.Select(k => k.Text).Contains(item.Model + " " + item.LicensePlate))
                    companyCarListItems.Add(new SelectListItem { Text = item.Model + " " + item.LicensePlate, Value = item.CompanyCarId.ToString() });
            return companyCarListItems;
        }

        private CompanyCarReservationDTO LoadCompanyCarReservationDto(CompanyCarReservation companyCarReservation, CompanyCarReservationDTO companyCarReservationDto)
        {
            companyCarReservationDto.CompanyCarReservationId = companyCarReservation.CompanyCarReservationId;
            companyCarReservationDto.PassToken = protector.Decode(companyCarReservation.CompanyCarReservationId.ToString());
            return companyCarReservationDto;
        }

        private ViewResult AuthenticationError(ViewResult viewResult, string key, string message)
        {
            ModelState.AddModelError(key, message);
            return viewResult;
        }

    }
}

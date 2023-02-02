using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Security;
using ARI.Models;
using ARI.Users.Queries;
using Infrastructure.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ARI.Controllers
{
    //[Authorize("Διαχειριστής")]
    public class AdminController : Controller
    {
        private readonly ARIContext _context;
        private IUserRepository _userRepository;
        private readonly CustomIDataProtection protector;

        public AdminController(ARIContext context, CustomIDataProtection customIDataProtection)
        {
            _context = context;
            _userRepository = new UserRepository(_context);
            protector = customIDataProtection;
        }

        public async Task<IActionResult> General()
        {
            return View(new GeneralInfoDTO
            {
                Users = await _context.Users.CountAsync(),
                Visitors = await _context.Guests.CountAsync(),
                Trucks = await _context.Trucks.CountAsync(),
                Companies = await _context.GuestCompanies.CountAsync(),
                Applications = await _context.Complaints.CountAsync(),
                CompanyCarReservations = await _context.CompanyCarReservations.CountAsync()
            });
        }

        public async Task<IActionResult> ManufacturingOperations()
        {
            return View(new ManufacturingOperationDTO
            {
                OrdersLidl = await _context.Orders.CountAsync(),
                OrdersAri = await _context.Orders.CountAsync(),
                PackagingLine = await _context.PackagingLineForms.CountAsync(),
                Pallets = await _context.Pallets.CountAsync(),
                ReservedPallets = await _context.Warehouses.Where(k=> k.PalletId != null).CountAsync()
            });
        }

        //Queries
        public async Task<IActionResult> DeleteAllUsers()
        {
            var users = await _context.Users.Where(k => k.RoleId != 1 && k.RoleId != 9).ToListAsync();
            var companyCarReservations = await _context.CompanyCarReservations.Where(k=>k.UserId.HasValue).Where(k => users.Select(l => l.UserId).ToList().Contains(k.UserId.Value)).ToListAsync();
            _context.RemoveRange(companyCarReservations);
            _context.RemoveRange(users);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(General));
        }

        public async Task<IActionResult> DeleteAllVisitors()
        {
            var trucks = await _context.Trucks.ToListAsync();

            var visitors = new List<Guest>();
            if (trucks.Count == 0)
                visitors = await _context.Guests.Include(k=> k.GuestCompany).Include(k=>k.GuestUser).Where(k=>k.GuestUser.Type == "Visitor").ToListAsync();
            else 
                visitors = await _context.Guests.ToListAsync();

            _context.RemoveRange(visitors);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(General));
        }
        public async Task<IActionResult> DeleteVisitorsByYear(int year)
        {
            var trucks = await _context.Trucks.ToListAsync();

            var visitors = new List<Guest>();
            if (trucks.Count == 0)
                visitors = await _context.Guests.Include(k => k.GuestCompany).Include(k => k.GuestUser).Where(k => k.GuestUser.Type == "Visitor" && k.ArrivalDateTime.Year == year).ToListAsync();
            else
                visitors = await _context.Guests.Where(k=> k.ArrivalDateTime.Year == year).ToListAsync();

            _context.RemoveRange(visitors);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(General));
        }

        public async Task<IActionResult> DeleteAllTrucks()
        {
            var visitors = await _context.Guests.ToListAsync();

            var trucks = new List<Guest>();
            if (visitors.Count == 0)
                trucks = await _context.Guests.Include(k => k.GuestCompany).Include(k => k.GuestUser).Where(k => k.GuestUser.Type == "Truck").ToListAsync();
            else
                trucks = await _context.Guests.ToListAsync();

            _context.RemoveRange(trucks);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(General));
        }
        public async Task<IActionResult> DeleteTrucksByYear(int year)
        {
            var visitors = await _context.Guests.ToListAsync();

            var trucks = new List<Guest>();
            if (visitors.Count == 0)
                trucks = await _context.Guests.Include(k => k.GuestCompany).Include(k => k.GuestUser).Where(k => k.GuestUser.Type == "Truck" && k.ArrivalDateTime.Year == year).ToListAsync();
            else
                trucks = await _context.Guests.Where(k => k.ArrivalDateTime.Year == year).ToListAsync();

            _context.RemoveRange(trucks);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(General));
        }

        public async Task<IActionResult> DeleteAllCompanyCarReservations()
        {
            var companyCarReservations = await _context.CompanyCarReservations.ToListAsync();
            _context.RemoveRange(companyCarReservations);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(General));
        }
        public async Task<IActionResult> DeleteCompanyCarReservationsByYear(int year)
        {
            var companyCarReservations = await _context.CompanyCarReservations.Where(k => k.ArrivalDateTime.Year == year).ToListAsync();
            _context.RemoveRange(companyCarReservations);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(General));
        }

        public async Task<IActionResult> DeleteAllApplications()
        {
            var complaints = await _context.Complaints.ToListAsync();
            _context.RemoveRange(complaints);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(General));
        }
        public async Task<IActionResult> DeleteApplicationsByYear(int year)
        {
            var complaints = await _context.Complaints.Where(k => k.IssuedDateTime.Year == year).ToListAsync();
            _context.RemoveRange(complaints);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(General));
        }

        public IActionResult DeleteAllProductionData()
        {
            //Update warehouses
            var warehouses = _context.Warehouses.ToList();
            foreach (var item in warehouses)
            {
                item.PalletId = null;
                item.ModificationDateTime = null;
            }
            _context.Warehouses.UpdateRange(warehouses);

            //Delete truckOrderPallets
            var truckOrderPallets = _context.TruckOrderPallets.ToList();
            _context.TruckOrderPallets.RemoveRange(truckOrderPallets);

            //Delete trucks
            var trucks = _context.Trucks.ToList();
            _context.Trucks.RemoveRange(trucks);

            //Delete orders
            var orders = _context.Orders.ToList();
            _context.Orders.RemoveRange(orders);

            //Delete PackagingLineFormPallets
            var packagingLineFormPallets = _context.PackagingLineFormPallets.ToList();
            _context.PackagingLineFormPallets.RemoveRange(packagingLineFormPallets);

            //Delete packagingLineForms
            var packagingLineForms = _context.PackagingLineForms.ToList();
            _context.PackagingLineForms.RemoveRange(packagingLineForms);

            //Delete pallets
            var pallets = _context.Pallets.ToList();
            _context.Pallets.RemoveRange(pallets);

            _context.SaveChangesAsync();

            return RedirectToAction(nameof(General));
        }
    }
}

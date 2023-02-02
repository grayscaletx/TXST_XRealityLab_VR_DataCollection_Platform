using Application.Security;
using ARI.Application;
using ARI.Models;
using ARI.Users.Queries;
using Common.Helpers;
using Infrastructure.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;


namespace ARI.Controllers
{
    [Authorize(Roles = "Τμήμα Διασφάλισης της Ποιότητας,Υπεύθυνος Ροών Εταιρίας")]
    public class QualityAssuranceController : Controller
    {
        private readonly ARIContext _context;
        private IUserRepository _userRepository;
        private readonly IHostingEnvironment _environment;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly CustomIDataProtection protector;

        public QualityAssuranceController(ARIContext context, IHostingEnvironment IHostingEnvironment, CustomIDataProtection customIDataProtection,
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
            return View();
        }
        public async Task<IActionResult> Materials()
        {
            List<MaterialDTO> materials = await _context.Materials1.Include(k=> k.Country).Select(k=> new MaterialDTO 
            {
                MaterialId = k.MaterialId,
                MaterialSAPID = k.MaterialSapid.TrimStart('0'),
                MaterialDescription = k.MaterialDescription,
                LanguageCode = k.Country.Code,
                GTIN = k.Gtin,
                LagerDays = k.LagerDays,
                ExpirationDays = k.ExpirationDays
            }).ToListAsync();

            materials = materials.GroupBy(x => x.MaterialSAPID).Select(x => x.FirstOrDefault()).ToList();

            return View(materials);
        }

        [HttpPost]
        public async Task<JsonResult> EditMaterial(IFormCollection _data)
        {
            ResponseDTO responseDTO = new ResponseDTO();

            if (!int.TryParse(_data["lager-days"],out int _lager) || !int.TryParse(_data["expiration-days"], out int _expiration)
                || !int.TryParse(_data["material-id"], out int _materialId))
                return Json(MessageConverter.GetResponse("Το συγκεκριμένο πεδίο δέχεται μόνο ακέραιους αριθμούς.", MethodResult.Failed));

            var material = await _context.Materials.Where(k => k.MaterialId == int.Parse(_data["material-id"])).FirstOrDefaultAsync();
            if (int.Parse(_data["lager-days"])>=0 && int.Parse(_data["expiration-days"])>=0)
            {
                if (int.Parse(_data["lager-days"]) != 0)
                    material.LagerDays = int.Parse(_data["lager-days"]);
                if (int.Parse(_data["expiration-days"]) != 0)
                    material.ExpirationDays = int.Parse(_data["expiration-days"]);
                _context.Update(material);
                await _context.SaveChangesAsync();

                return Json(MessageConverter.GetResponse("Succeded", MethodResult.Succeeded));
            }else
                return Json(MessageConverter.GetResponse("Το συγκεκριμένο πεδίο δέχεται μόνο θετικους ακέραιους αριθμούς.", MethodResult.Failed));


        }

        //Utils
        public async Task<IActionResult> DownloadMaterials()
        {
            try
            {
                List<MaterialDTO> materialsDto = await _context.Materials1.Include(k => k.Country).Include(k=> k.ProductFamily).Select(k => new MaterialDTO
                {
                    MaterialId = k.MaterialId,
                    MaterialSAPID = k.MaterialSapid.TrimStart('0'),
                    MaterialDescription = k.MaterialDescription,
                    LanguageCode = k.Country.Code,
                    GTIN = k.Gtin,
                    LagerDays = k.LagerDays,
                    ExpirationDays = k.ExpirationDays,
                    PalletFactor = k.PalletFactor,
                    GrossWeight = k.GrossWeight,
                    NetWeight = k.NetWeight,
                    Height = k.Height,
                    PalletCost = k.PalletCost,
                    FinalPrice = k.FinalPrice.Value,
                    Name = k.Name1,
                    PiecesPerCarton = k.PiecesPerCarton.Value,
                    EANUPC = k.Eanupc,
                    BUn = k.Bun,
                    ProductFamilyName = k.ProductFamily.Name,
                    CustomerMaterialNumber = k.CustomerMaterialNumber
                }).ToListAsync();

                StringBuilder stringBuilder = new StringBuilder();

                stringBuilder.AppendLine("Α/Α,MaterialSAPID,MaterialDescription,CountryCode,GTIN,LagerDays,ExpirationDays,PalletFactor,GrossWeight,NetWeight,Height,PalletCost," +
                    "FinalPrice,Name1,PiecesPerCarton,EANUPC,BUn,ProductFamilyName,CustomerMaterialNumber");

                foreach (var item in materialsDto)
                {
                    stringBuilder.AppendLine(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18}",
                        "ΠΡ " + item.MaterialId,
                        item.MaterialSAPID.TrimStart('0'),
                        item.MaterialDescription,
                        item.LanguageCode,
                        item.GTIN,
                        item.LagerDays,
                        item.ExpirationDays,
                        item.PalletFactor,
                        item.GrossWeight,
                        item.NetWeight,
                        item.Height,
                        item.PalletCost,
                        item.FinalPrice,
                        item.Name,
                        item.PiecesPerCarton,
                        item.EANUPC,
                        item.BUn,
                        item.ProductFamilyName,
                        item.CustomerMaterialNumber));
                }

                var data = Encoding.UTF8.GetBytes(stringBuilder.ToString());
                var result = Encoding.UTF8.GetPreamble().Concat(data).ToArray();
                return File(result, "text/csv", string.Format("{0}_report_{1}.csv", "ari_materials", DateTimeOffset.Now.ToUnixTimeSeconds()));
            }
            catch (Exception ex)
            {
                return RedirectToAction("Materials", "QualityAssurance");
            }
        }

    }
}

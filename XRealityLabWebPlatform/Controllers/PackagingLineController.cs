using Application.Security;
using ARI.Common;
using ARI.Helpers;
using ARI.Models;
using ARI.Users.Queries;
using ARIWebPlatform;
using AzureStorage;
using Common.Constants;
using Common.Helpers;
using EnumsNET;
using Infrastructure.Entities;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZXing;
using ZXing.QrCode;

namespace ARI.Controllers
{
    [Authorize]
    public class PackagingLineController : Controller
    {
        private readonly ARIContext _context;
        private IUserRepository _userRepository;
        private readonly IHostingEnvironment _environment;
        private readonly CustomIDataProtection protector;
        public PackagingLineController(ARIContext context, IHostingEnvironment IHostingEnvironment, CustomIDataProtection customIDataProtection)
        {
            _context = context;
            _userRepository = new UserRepository(_context);
            _environment = IHostingEnvironment;
            protector = customIDataProtection;
        }

        //Actions
        public IActionResult PackagingLineForms(string _actionMessage)
        {
            ViewBag.actionMessage = _actionMessage;
            return View(LoadPackagingLineForms());
        }
        public IActionResult AddPackagingLineForm(string _actionMessage)
        {
            ViewBag.actionMessage = _actionMessage;
            return View(LoadMaterialItems());
        }
        [HttpPost,ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPackagingLineForm([Bind("SelectedMaterial,FormDate,NoPalletToCreated,Notes")] PackagingLineFormDTO packagingLineFormDTO)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(AddPackagingLineForm), new { _actionMessage = "Δεν προσθέσατε τα πεδία σωστά. Παρακαλώ προσπαθήστε ξανά." });
            }

            if (packagingLineFormDTO.FormDate <= new DateTime(2021, 1, 1) || packagingLineFormDTO.FormDate == null
                || packagingLineFormDTO.NoPalletToCreated <= 0 || packagingLineFormDTO.NoPalletToCreated > 100
                || packagingLineFormDTO.SelectedMaterial == null)
                return RedirectToAction(nameof(AddPackagingLineForm), new { _actionMessage = "Δεν προσθέσατε τα πεδία σωστά.\n -Υποχρεωτικά πεδία ημερομηνία εντύπου,κωδικοποίηση παλέτας, αριθμός παλετών. \n -Παλέτες [0-100]. \nΠαρακαλώ προσπαθήστε ξανά." });

            Material1 material = await _context.Materials1.FirstOrDefaultAsync(k => k.MaterialId == int.Parse(packagingLineFormDTO.SelectedMaterial));
            PackagingLineForm packagingLineForm = new PackagingLineForm
            {
                MaterialId = material.MaterialId,
                CountryId = material.CountryId,
                PalletsLot = ClassConverters.GetLot(packagingLineFormDTO.FormDate),
                FormCreationDate = packagingLineFormDTO.FormDate,
                Notes = packagingLineFormDTO.Notes,
                RegistrationDateTime = DateTime.Now,
                NoPalletToCreated = packagingLineFormDTO.NoPalletToCreated,
                Status = 0
            };
            await _context.AddAsync(packagingLineForm);
            await _context.SaveChangesAsync();

            var _packagingLineForm = _context.PackagingLineForms.OrderByDescending(k => k.PackagingLineFormId).First();
            var lastPalletId = GetPalletId();

            var pallet = _context.Pallets.OrderByDescending(i => i.PalletId).FirstOrDefault();
            string lastSscc = (pallet == null)? "1520697200040396" : pallet.Sscc.Remove(pallet.Sscc.Length - 1);

            //Generate QRCode, Barcode, Palletcard foreach pallet
            List < PackagingLineFormPallet > packagingLineFormPallets = new List<PackagingLineFormPallet>();
            for (int i = 0; i < packagingLineForm.NoPalletToCreated; i++)
            {
                string randomIndex = ClassConverters.RandomString(15);
                
                ClassConverters.GenerateQRCode(_environment, this.Request.Scheme, this.Request.Host, this.Request.PathBase, protector.Decode((lastPalletId + i).ToString()), randomIndex);
                ClassConverters.GenerateBarCode(_environment, protector, protector.Decode((lastPalletId + i).ToString()), randomIndex);
                ClassConverters.GeneratePalletCardARI(_context, _environment, protector, protector.Decode(_packagingLineForm.PackagingLineFormId.ToString()), 
                    protector.Decode((lastPalletId + i).ToString()), randomIndex, i + 1);
                
                var serialNumberParts = lastSscc.Split("15206972000");
                var currentSerialNumber = Convert.ToInt64(serialNumberParts[1]) + 1 + i;

                Random rnd = new Random();
                packagingLineFormPallets.Add(new PackagingLineFormPallet()
                {
                    PackagingLineFormId = _packagingLineForm.PackagingLineFormId,
                    Pallet = new Pallet
                    {
                        MaterialId = material.MaterialId,
                        CountryId = material.CountryId,
                        PalletLot = ClassConverters.GetLot(packagingLineForm.FormCreationDate),
                        Qrcode = System.IO.Path.Combine("https://arisa.blob.core.windows.net", AzureGPV2Credentials.CONTAINER_QR_CODES, "ari_qrcode_" + randomIndex + ".jpg"),
                        BarCode = System.IO.Path.Combine("https://arisa.blob.core.windows.net", AzureGPV2Credentials.CONTAINER_BAR_CODES, "ari_barcode_" + randomIndex + ".jpg"),
                        PalletCard = System.IO.Path.Combine("https://arisa.blob.core.windows.net", AzureGPV2Credentials.CONTAINER_PDF_FILES, "pallet_card_ari_" + randomIndex + ".pdf"),
                        Sscc = "15206972000" + currentSerialNumber + rnd.Next(1, 9).ToString(),
                        PackagingLinePalletNo = i,
                        Status = 0
                    }
                });
            }

            await _context.PackagingLineFormPallets.AddRangeAsync(packagingLineFormPallets);
            await _context.SaveChangesAsync();

            return RedirectToAction("PackagingLineForms", "PackagingLine");
        }
        public async Task<IActionResult> EditPackagingLineForm(string passToken, string _actionMessage)
        {
            if (passToken == null || passToken == string.Empty) return NotFound();

            try
            {
                var packagingLineFormDTO = await _context.PackagingLineForms
                    .Include(k => k.Material).ThenInclude(k => k.Country).Include(k => k.PackagingLineFormPallets).ThenInclude(k => k.Pallet)
                    .Where(k => k.PackagingLineFormId == int.Parse(protector.Encode(passToken))).Select(k => new PackagingLineFormDTO
                {
                    PackagingLineId = k.PackagingLineFormId,
                    PassToken = protector.Decode(k.PackagingLineFormId.ToString()),
                    Material = new MaterialDTO 
                    {
                        PalletFactor = k.Material.PalletFactor, 
                        MaterialSAPID = k.Material.MaterialSapid.TrimStart('0'), 
                        ExpirationDate = ClassConverters.GetExpirationDate(k.FormCreationDate, k.Material.ExpirationDays) 
                    },
                    MaterialDescription = k.Material.MaterialDescription,
                    PackagingLineDescription = k.Material.PackagingLineDescription,
                    MaterialLOT = k.PalletsLot,
                    CountryCode = k.Material.Country.Code,
                    NoPalletBox = k.PackagingLineFormPallets.Count(l => l.PalletId == k.PackagingLineFormId),
                    NoPalletToCreated = k.NoPalletToCreated,
                    NoCompletedPallets = k.PackagingLineFormPallets.Where(l => l.Pallet.PalletCreationDateTime != null).Count(),
                    FormDate = k.FormCreationDate,
                    RegistrationDateTime = k.RegistrationDateTime,
                    CompletionDateTime = k.CompletionDateTime,
                    Notes = k.Notes,
                    StatusId = k.Status,
                    SelectedStatus = ((PackagingLineStatus)k.Status).AsString(EnumFormat.Description),
                    palletDTOs = k.PackagingLineFormPallets.Select(x => new PalletDTO
                    {
                        PalletId = x.Pallet.PalletId,
                        CountryCode = k.Material.Country.Code,
                        PackagingLinePalletNo = x.Pallet.PackagingLinePalletNo,
                        QRCodeLink = x.Pallet.Qrcode,
                        BarCodeLink = x.Pallet.BarCode,
                        PalletCardLink = x.Pallet.PalletCard,
                        PalletCardLIDLLink = x.Pallet.PalletCardLidl,
                        NoPalletBox = k.Material.PalletFactor,
                        StatusId = x.Pallet.Status,
                        IsVirtual = x.Pallet.IsVirtual == 1? true : false,
                        SelectedStatus = ((PalletStatus)k.Status).AsString(EnumFormat.Description),
                        CreationDateTime = x.Pallet.PalletCreationDateTime.Value.ToString("HH:mm")
                    }).ToList()
                }).FirstOrDefaultAsync();

                ViewBag.actionMessage = _actionMessage;
                return View(packagingLineFormDTO);
            }
            catch
            {
                return NotFound();
            }
        }
        public async Task<IActionResult> PalletList(bool? succeededPastAction, string _actionMessage)
        {
            List<PalletDTO> palletDTOs = new List<PalletDTO>();

            var packagingLines = await _context.PackagingLineForms.Where(k => k.Status != (int)PackagingLineStatus.Cancel).ToListAsync();
            foreach (var packagingLineItem in packagingLines)
            {
                palletDTOs.AddRange(_context.PackagingLineFormPallets.Include(k => k.Pallet).ThenInclude(k=> k.Material).ThenInclude(k=>k.Country).Where(k => k.PackagingLineForm == packagingLineItem && (k.Pallet.PalletCreationDateTime != null || k.Pallet.IsVirtual==1) && k.Pallet.Status != -1).Select(k => new PalletDTO
                {
                    PalletId = k.PalletId,
                    Material = new MaterialDTO
                    {
                        MaterialSAPID = k.Pallet.Material.MaterialSapid.TrimStart('0'),
                        MaterialLOT = ClassConverters.GetLot(packagingLineItem.FormCreationDate),
                        MaterialDescription = k.Pallet.Material.MaterialDescription,
                        PackagingLineDescription = k.Pallet.Material.PackagingLineDescription,
                        MaxDeliveryDate = ClassConverters.GetMaxDeliveryDate(packagingLineItem.FormCreationDate, k.Pallet.Material.LagerDays),
                        ExpirationDate = ClassConverters.GetExpirationDate(packagingLineItem.FormCreationDate, k.Pallet.Material.ExpirationDays)
                    },
                    PalletLOT = _context.Pallets.Where(x => x.PalletId == k.PalletId).FirstOrDefault().PalletLot,
                    CountryCode = k.Pallet.Country.Code,
                    PackagingLinePalletNo = k.Pallet.PackagingLinePalletNo,
                    QRCodeLink = k.Pallet.Qrcode,
                    BarCodeLink = k.Pallet.BarCode,
                    PalletCardLink = k.Pallet.PalletCard,
                    PalletCardLIDLLink = k.Pallet.PalletCardLidl,
                    NoPalletBox = k.Pallet.Material.PalletFactor,
                    StatusId = k.Pallet.Status,
                    IsVirtual = k.Pallet.IsVirtual == 1 ? true : false,
                    SelectedStatus = ((PalletStatus)k.Pallet.Status).AsString(EnumFormat.Description),
                    PackagingLineId = packagingLineItem.PackagingLineFormId,
                    LoadingAreaID = 0,
                    LoadingLine = 0,
                    LoadingPosition = 0,
                    CreationDateTime = k.Pallet.PalletCreationDateTime.Value.ToString("HH:mm")
                }).ToList());
            }


            var warehousePallets = await _context.Warehouses.Where(k => k.PalletId != null).ToListAsync();
            foreach (var warehousePalletItem in warehousePallets)
            {
                palletDTOs.Where(k => k.PalletId == warehousePalletItem.PalletId).FirstOrDefault().LoadingAreaID = warehousePalletItem.LoadingAreaId;
                palletDTOs.Where(k => k.PalletId == warehousePalletItem.PalletId).FirstOrDefault().LoadingLine = warehousePalletItem.LoadingLine;
                palletDTOs.Where(k => k.PalletId == warehousePalletItem.PalletId).FirstOrDefault().LoadingPosition = warehousePalletItem.LoadingPosition;
            }

            var palletListItems = new List<SelectListItem>();
            palletListItems.Add(new SelectListItem { Text = "-", Value = "-" });
            foreach (var palletItem in palletDTOs.Where(k => k.StatusId == (int)PalletStatus.Created || k.IsVirtual ))
                palletListItems.Add(new SelectListItem
                {
                    Text = "Παραγωγή: " + palletItem.PackagingLineId + " | " + "Παλέτα: " + palletItem.PalletId + " | " + "Σειρά: " + palletItem.PackagingLinePalletNo + " | " + "Χώρα: " + palletItem.CountryCode + " | " + "SAP: " + palletItem.Material.MaterialSAPID.TrimStart('0') + " | " + "LOT: " + palletItem.Material.MaterialLOT
                    + " | " + "Προϊόν: " + palletItem.Material.MaterialDescription,
                    Value = palletItem.PalletId.ToString()
                });

            var WarehouseAreaListItems = new List<SelectListItem>();
            WarehouseAreaListItems.Add(new SelectListItem { Text = "-", Value = "-" });

            var positionListItems = new List<SelectListItem>();
            positionListItems.Add(new SelectListItem { Text = "-", Value = "-" });

            for (int i = 1; i < 9; i++)
                WarehouseAreaListItems.Add(new SelectListItem { Text = ((Common.WarehouseArea)i).AsString(EnumFormat.Description), Value = i.ToString() });

            ViewBag.actionMessage = _actionMessage;
            return View(new WarehouseDTO { WarehouseAreaListItems = WarehouseAreaListItems, PositionListItems = positionListItems, PalletListItems = palletListItems, PalletDTOs = palletDTOs, SucceededPastAction = succeededPastAction == true ? true : false });
        }
        
        //Events
        public async Task<IActionResult> ChangeFormStatus(string passToken, int formStatus)
        {
            if (passToken == null || passToken == string.Empty) return NotFound();

            var pallet = await _context.Pallets.Include(k => k.PackagingLineFormPallets).ThenInclude(k => k.PackagingLineForm).FirstOrDefaultAsync(k => k.PalletId == int.Parse(protector.Encode(passToken)));
            if (formStatus == -1)
            {
                if (pallet.Status == 4)
                    return RedirectToAction("EditPackagingLineForm", "PackagingLine", new { passToken = protector.Decode(pallet.PackagingLineFormPallets.FirstOrDefault().PackagingLineFormId.ToString()), _actionMessage = "Δεν επιτρέπεται η ακύρωση παλετας που εχει επιβ. για φόρτ." });
                if (pallet.Status == 3)
                {
                    var loadPallets = await _context.LoadPallets.Where(k => k.PalletId == pallet.PalletId).FirstOrDefaultAsync();
                    _context.Loads.Where(k => k.LoadId == loadPallets.LoadId).FirstOrDefault().IsMatch = 0;
                    _context.LoadPallets.RemoveRange(loadPallets);
                }
                var warehouses = await _context.Warehouses.FirstOrDefaultAsync(k => k.PalletId == pallet.PalletId);
                if (warehouses != null)
                {
                    warehouses.PalletId = null;
                    warehouses.ModificationDateTime = null;
                    _context.Update(warehouses);
                }
            }
            pallet.Status = formStatus == -1 ? -1 : pallet.PalletCreationDateTime != null ? 1 : 0;

            _context.Update(pallet);
            await _context.SaveChangesAsync();

            string actionMessage = formStatus == -1 ? "Ακυρώθηκε η παλέτα με Α/Α ΚΠ" + pallet.PalletId.ToString() :
                "Ενεργοποίηθηκε η παλέτα με Α/Α ΚΠ" + pallet.PalletId.ToString();

            return RedirectToAction("EditPackagingLineForm", "PackagingLine", new { passToken = protector.Decode(pallet.PackagingLineFormPallets.FirstOrDefault().PackagingLineFormId.ToString()), _actionMessage = actionMessage });
        }
        public async Task<IActionResult> ChangeFormAllStatus(string passToken, int formStatus, int isEditView)
        {
            if (passToken == null || passToken == string.Empty) return NotFound();

            var packagingLineForm = await _context.PackagingLineForms.Include(k => k.PackagingLineFormPallets).ThenInclude(k => k.Pallet).FirstOrDefaultAsync(k => k.PackagingLineFormId == int.Parse(protector.Encode(passToken)));
            packagingLineForm.Status = formStatus;

            if (formStatus == -1)
            {
                foreach (var packagingLineFormPalletItem in packagingLineForm.PackagingLineFormPallets)
                {
                    if (packagingLineFormPalletItem.Pallet.Status == 4)
                        return RedirectToAction("PackagingLineForms", "PackagingLine", new { _actionMessage = "Δεν επιτρέπεται η ακυρωση εντύπου με επιβ. για φόρτ. παλέτες" });
                }
                foreach (var packagingLineFormPalletItem in packagingLineForm.PackagingLineFormPallets)
                {
                    if (packagingLineFormPalletItem.Pallet.Status == 3)
                    {
                        var loadPallets = await _context.LoadPallets.Where(k => k.PalletId == packagingLineFormPalletItem.Pallet.PalletId).FirstOrDefaultAsync();
                        _context.Loads.Where(k => k.LoadId == loadPallets.LoadId).FirstOrDefault().IsMatch = 0;
                        _context.LoadPallets.RemoveRange(loadPallets);
                    }
                    var warehouses = await _context.Warehouses.FirstOrDefaultAsync(k => k.PalletId == packagingLineFormPalletItem.PalletId);
                    if(warehouses != null)
                    {
                        warehouses.PalletId = null;
                        warehouses.ModificationDateTime = null;
                        _context.Update(warehouses);
                    }
                }
            }

            foreach (var packagingLineFormPalletItem in packagingLineForm.PackagingLineFormPallets)
                    packagingLineFormPalletItem.Pallet.Status = formStatus == -1 ? -1 : packagingLineFormPalletItem.Pallet.PalletCreationDateTime != null ? packagingLineFormPalletItem.Pallet.Status >= 2? packagingLineFormPalletItem.Pallet.Status : 1 : packagingLineFormPalletItem.Pallet.IsVirtual == 1? 1 : 0;

            _context.Update(packagingLineForm);
            await _context.SaveChangesAsync();

            string actionMessage = formStatus == -1 ? "Ακυρώθηκε το έντυπο με Α/Α ΚΕΣ" + packagingLineForm.PackagingLineFormId.ToString() :
                formStatus == 0 ? "Πραγματοποιήθηκε αναίρεση επικαιροποίησης του εντύπου με Α/Α ΚΕΣ" + packagingLineForm.PackagingLineFormId.ToString() :
                formStatus == 1 ? "Πραγματοποιήθηκε επικαιροποίηση του εντύπου με Α/Α ΚΕΣ" + packagingLineForm.PackagingLineFormId.ToString() : "";

            return isEditView == 0 ? RedirectToAction("PackagingLineForms", "PackagingLine", new { _actionMessage = actionMessage }) :
                RedirectToAction("EditPackagingLineForm", "PackagingLine", new { passToken = protector.Decode(packagingLineForm.PackagingLineFormId.ToString()), _actionMessage = actionMessage });
        }
        public async Task<IActionResult> ChangeFormVirtualStatus(string passToken, int isVirtual)
        {
            if (passToken == null || passToken == string.Empty) return NotFound();

            var pallet = await _context.Pallets.Include(k=>k.PackagingLineFormPallets).ThenInclude(k=>k.PackagingLineForm).FirstOrDefaultAsync(k => k.PalletId == int.Parse(protector.Encode(passToken)));
            pallet.IsVirtual = isVirtual==1? pallet.PalletCreationDateTime==null? (byte)isVirtual : pallet.IsVirtual : (byte)isVirtual;
            _context.Update(pallet);
            await _context.SaveChangesAsync();

            string actionMessage = isVirtual == 1 ? "Μετατράπηκε η παλέτα σε εικονική" : "Μετατράπηκε η παλέτα σε πραγματική";

            return RedirectToAction("EditPackagingLineForm", "PackagingLine", new { passToken = protector.Decode(pallet.PackagingLineFormPallets.FirstOrDefault().PackagingLineFormId.ToString()), _actionMessage = actionMessage });
        }
        public async Task<IActionResult> ChangeFormAllVirtualStatus(string passToken, int isVirtual)
        {
            if (passToken == null || passToken == string.Empty) return NotFound();

            var packagingLineForm = await _context.PackagingLineForms.Include(k=>k.PackagingLineFormPallets).ThenInclude(k=>k.Pallet).FirstOrDefaultAsync(k => k.PackagingLineFormId == int.Parse(protector.Encode(passToken)));
            foreach (var packagingLineFormPalletItem in packagingLineForm.PackagingLineFormPallets)
                packagingLineFormPalletItem.Pallet.IsVirtual = isVirtual == 1 ? packagingLineFormPalletItem.Pallet.PalletCreationDateTime == null ? (byte)isVirtual : packagingLineFormPalletItem.Pallet.IsVirtual : (byte)isVirtual;

            _context.UpdateRange(packagingLineForm);
            await _context.SaveChangesAsync();

            string actionMessage = isVirtual == 1 ? "Μετατράπηκαν όσες παλέτες δεν ήταν κλείδωμένες σε εικονικές" : "Μετατράπηκαν όσες παλέτες δεν ήταν κλείδωμένες σε πραγματικές";

            return RedirectToAction("EditPackagingLineForm", "PackagingLine", new { passToken = protector.Decode(packagingLineForm.PackagingLineFormId.ToString()), _actionMessage = actionMessage });
        }
        public async Task<IActionResult> RevertPalletToLobby(string passToken)
        {
            if (passToken == null || passToken == string.Empty) return NotFound();

            var pallet = await _context.Pallets.Where(k=> k.PalletId == int.Parse(protector.Encode(passToken))).FirstOrDefaultAsync();
            pallet.Status = 1;

            var warehouse = await _context.Warehouses.Where(k => k.PalletId == pallet.PalletId).FirstOrDefaultAsync();
            warehouse.PalletId = null;
            warehouse.ModificationDateTime = null;

            _context.Update(pallet);
            _context.Update(warehouse);
            await _context.SaveChangesAsync();

            return RedirectToAction("PalletList", "PackagingLine", new { _actionMessage = "Έγινε επαναφορά της παλέτας στον χώρο της αναμονής." });
        }
        [HttpPost]
        public JsonResult ChangeAllPalletLot(string passToken, string selectedItem)
        {
            if (passToken == null || passToken == string.Empty) return new JsonResult("error");
            if (!int.TryParse(selectedItem, out int newLot)) return new JsonResult("error");
            if (newLot > 2000 || newLot < 1) return new JsonResult("error");


            var packagingLineForm = _context.PackagingLineForms.Include(k => k.PackagingLineFormPallets).ThenInclude(k => k.Pallet).FirstOrDefault(k => k.PackagingLineFormId == int.Parse(protector.Encode(passToken)));
            foreach (var packagingLineFormPalletItem in packagingLineForm.PackagingLineFormPallets)
            {
                if (packagingLineFormPalletItem.Pallet.Status == 4)
                    return new JsonResult("error");
            }
            foreach (var packagingLineFormPalletItem in packagingLineForm.PackagingLineFormPallets)
                packagingLineFormPalletItem.Pallet.PalletLot = newLot;
            packagingLineForm.PalletsLot = newLot;
            _context.Update(packagingLineForm);
            _context.SaveChangesAsync();
            return new JsonResult("ok");
        }
        public async Task<JsonResult> ChangePalletLot(string passToken, string palletId, string selectedItem)
        {
            if (passToken == null || passToken == string.Empty) return new JsonResult("error");
            if (!int.TryParse(selectedItem, out int newLot)) return new JsonResult("error");
            if (newLot > 2000 || newLot < 1) return new JsonResult("error");

            var packagingLineForm = _context.PackagingLineForms.Include(k => k.PackagingLineFormPallets).ThenInclude(k => k.Pallet).FirstOrDefault(k => k.PackagingLineFormId == int.Parse(protector.Encode(passToken)));
            var pallet = await _context.Pallets.Where(k => k.PalletId == int.Parse(palletId)).FirstOrDefaultAsync();

            if (pallet.Status == 4)
                return new JsonResult("error");

            pallet.PalletLot = newLot;
            _context.Pallets.Update(pallet);
            await _context.SaveChangesAsync();
            return new JsonResult("ok");
        }
        [HttpPost]
        public async Task<int> SetPalletHour(int packagingLineId, int palleteNo, string time)
        {
            if (packagingLineId == 0 || palleteNo == 0 || time.Length <= 3 || time.Length >= 6 || time == "00:00")
                return 0;
            try
            {
                var packagingLine = await _context.PackagingLineForms.FirstOrDefaultAsync(k => k.PackagingLineFormId == packagingLineId);
                int palletId = _context.PackagingLineFormPallets.Include(k => k.Pallet).FirstOrDefault(k => k.PackagingLineFormId == packagingLineId && k.Pallet.PackagingLinePalletNo == palleteNo - 1).PalletId;
                var pallet = _context.Pallets.FirstOrDefault(k => k.PalletId == palletId);
                pallet.Status = 1;
                pallet.PalletCreationDateTime = packagingLine.FormCreationDate.Date + new TimeSpan(int.Parse(time.Split(':')[0]), int.Parse(time.Split(':')[1]), 0);
                pallet.IsVirtual = 0;
                _context.Update(pallet);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return 0;
            }
            return _context.PackagingLineFormPallets.Include(k => k.Pallet).Count(k => k.PackagingLineFormId == packagingLineId && k.Pallet.PalletCreationDateTime != null);
        }
        [HttpGet]
        public JsonResult GetPalletInfo(int palletId)
        {
            return (palletId == 0) ? new JsonResult(new PalletDTO()) : Json(ClassConverters.GetPalletInfo(_context, protector, palletId));
        }
        [HttpGet]
        public JsonResult GetPalletWarehouseInfo(string warehouseId, string loadinglineId, string itemId)
        {
            if (warehouseId == "-" || itemId == "-" || (loadinglineId == "-" && warehouseId != "2" && warehouseId != "7"))return Json("Error");
            if (loadinglineId == "-") loadinglineId = "0";

            int palletId = _context.Warehouses.Where(k => k.LoadingAreaId == int.Parse(warehouseId) && k.LoadingLine == int.Parse(loadinglineId) && k.LoadingPosition == int.Parse(itemId)).FirstOrDefault().PalletId.Value;
            return (palletId == 0) ? new JsonResult(new PalletDTO()) : Json(ClassConverters.GetPalletInfo(_context, protector, palletId));
        }


        //-active-
        [HttpGet]
        public async Task<JsonResult> LoadMaterialItems(int materialId)
        {
            var materialDto = await _context.Materials1.Include(k => k.Country).Where(k => k.MaterialId == materialId).Select(k => new MaterialDTO
            {
                MaterialDescription = k.MaterialDescription,
                PackagingLineDescription = k.PackagingLineDescription,
                LanguageCode = k.Country.Code,
                PalletFactor = k.PalletFactor,
                ExpirationDays = k.ExpirationDays
            }).FirstOrDefaultAsync();

            return Json(materialDto);
        }
        [HttpPost]
        public JsonResult GetFilterMaterials(IFormCollection data)
        {
            string search_text = data["search-text"].ToString().ToLower();
            var materialListItem = _context.Materials1.Where(k => (k.MaterialDescription.Contains(search_text) || k.MaterialSapid.Contains(search_text) || k.Gtin.Contains(search_text)) && k.LagerDays != 0 && k.ExpirationDays != 0).OrderBy(k => k.MaterialSapid).Select(k => new SelectListItem
            {
                Text = k.MaterialSapid + " " + k.MaterialDescription,
                Value = k.MaterialId.ToString()
            }).ToList();

            return Json(materialListItem);
        }

        //Utils
        private PackagingLineFormDTO LoadMaterialItems()
        {
            var materialListItem = new List<SelectListItem>();
            materialListItem.Add(new SelectListItem { Text = "-", Value = "-" });
            materialListItem.AddRange(_context.Materials1.Where(k => k.ExpirationDays != 0 && k.LagerDays != 0).Select(k => new SelectListItem { Text = k.MaterialSapid.TrimStart('0') + " " + k.MaterialDescription, Value = k.MaterialId.ToString() }));
            materialListItem = materialListItem.OrderBy(k => k.Text).ToList();
            return new PackagingLineFormDTO { MaterialListItems = materialListItem };
        }
        private List<PackagingLineFormDTO> LoadPackagingLineForms()
        {
            return _context.PackagingLineForms.Include(k => k.Material).Include(k => k.PackagingLineFormPallets).ThenInclude(k => k.Pallet).Select(k => new PackagingLineFormDTO
            {
                PackagingLineId = k.PackagingLineFormId,
                PassToken = protector.Decode(k.PackagingLineFormId.ToString()),
                MaterialId = k.Material.MaterialId,
                MaterialDescription = k.Material.MaterialDescription,
                PackagingLineDescription = k.Material.PackagingLineDescription,
                MaterialLOT = ClassConverters.GetLot(k.FormCreationDate),
                CountryCode = _context.Countries.FirstOrDefault(l => l.CountryId == k.CountryId).Code,
                NoPalletToCreated = k.NoPalletToCreated,
                NoCompletedPallets = k.PackagingLineFormPallets.Where(l => l.Pallet.PalletCreationDateTime != null).Count(),
                FormDate = k.FormCreationDate,
                RegistrationDateTime = k.RegistrationDateTime,
                CompletionDateTime = k.CompletionDateTime,
                StatusId = k.Status,
                SelectedStatus = ((PackagingLineStatus)k.Status).AsString(EnumFormat.Description)
            }).ToList();
        }
        private int GetPalletId()
        {
            var lastpallet = _context.Pallets.OrderByDescending(k => k.PalletId).FirstOrDefault();
            int lastpalletId = 0;
            if (lastpallet != null)
            {
                lastpalletId = lastpallet.PalletId + 1;
            }
            else
            {
                _context.Pallets.Add(new Pallet { MaterialId = _context.Materials1.FirstOrDefault().MaterialId, CountryId = _context.Countries.FirstOrDefault().CountryId, PalletLot = 0, PackagingLinePalletNo = 0, Qrcode = "-", BarCode = "-", PalletCard = "-", Sscc = "-", Status = 0 });
                _context.SaveChanges();
                var tempPallet = _context.Pallets.FirstOrDefault();
                lastpalletId = _context.Pallets.FirstOrDefault().PalletId + 1;
                _context.Pallets.Remove(tempPallet);
                _context.SaveChanges();
            }
            return lastpalletId;
        }//NOT GOOD PRACTICE

        //Dev
        public IActionResult DeletePackagingLineData()
        {
            var packagingLineFormPallets = _context.PackagingLineFormPallets.ToList();
            var packagingLineForms = _context.PackagingLineForms.ToList();
            var pallets = _context.Pallets.ToList();
            _context.RemoveRange(packagingLineFormPallets);
            _context.RemoveRange(packagingLineForms);
            _context.RemoveRange(pallets);
            _context.SaveChanges();
            return RedirectToAction("PackagingLineForms", "PackagingLine");
        }








        public async Task<IActionResult> MovePalletPosition(bool? succeededPastAction)
        {
            List<PalletDTO> palletDTOs = new List<PalletDTO>();

            var packagingLines = await _context.PackagingLineForms.Where(k => /*k.Status == (int)PackagingLineStatus.LoadPallets ||*/ k.Status != (int)PackagingLineStatus.Cancel).ToListAsync();
            foreach (var packagingLineItem in packagingLines)
            {
                var tempPalletDTOs = _context.PackagingLineFormPallets.Include(k => k.Pallet).Where(k => k.PackagingLineForm == packagingLineItem && k.Pallet.PalletCreationDateTime != null).Select(k => new PalletDTO
                {
                    PalletId = k.PalletId,
                    Material = _context.Materials1.Where(l => l.MaterialId == k.Pallet.MaterialId).Select(x => new MaterialDTO
                    {
                        MaterialSAPID = x.MaterialSapid.TrimStart('0'),
                        MaterialLOT = ClassConverters.GetLot(packagingLineItem.FormCreationDate),
                        MaterialDescription = x.MaterialDescription,
                        PackagingLineDescription = x.PackagingLineDescription,
                        MaxDeliveryDate = ClassConverters.GetMaxDeliveryDate(packagingLineItem.FormCreationDate, x.LagerDays),
                        ExpirationDate = ClassConverters.GetExpirationDate(packagingLineItem.FormCreationDate, x.ExpirationDays)
                    }).FirstOrDefault(),
                    CountryCode = _context.Materials1.Include(j => j.Country).FirstOrDefault(j => j.MaterialId == k.Pallet.MaterialId).Country.Code,
                    PackagingLinePalletNo = k.Pallet.PackagingLinePalletNo,
                    QRCodeLink = k.Pallet.Qrcode,
                    BarCodeLink = k.Pallet.BarCode,
                    PalletCardLink = k.Pallet.PalletCard,
                    PalletCardLIDLLink = k.Pallet.PalletCardLidl,
                    NoPalletBox = _context.Materials1.Include(j => j.Country).FirstOrDefault(k => k.MaterialId == k.MaterialId).PalletFactor,
                    StatusId = k.Pallet.Status,
                    SelectedStatus = ((PalletStatus)k.Pallet.Status).AsString(EnumFormat.Description),
                    PackagingLineId = packagingLineItem.PackagingLineFormId,
                    LoadingAreaID = 0,
                    LoadingLine = 0,
                    LoadingPosition = 0,
                    CreationDateTime = k.Pallet.PalletCreationDateTime.Value.ToString("HH:mm")
                }).ToList();
                palletDTOs.AddRange(tempPalletDTOs);
            }
            var warehousePallets = await _context.Warehouses.Where(k => k.PalletId != null).ToListAsync();

            //Add Area, Line, Position
            foreach (var warehousePalletItem in warehousePallets)
            {
                palletDTOs.Where(k => k.PalletId == warehousePalletItem.PalletId).FirstOrDefault().LoadingAreaID = warehousePalletItem.LoadingAreaId;
                palletDTOs.Where(k => k.PalletId == warehousePalletItem.PalletId).FirstOrDefault().LoadingLine = warehousePalletItem.LoadingLine;
                palletDTOs.Where(k => k.PalletId == warehousePalletItem.PalletId).FirstOrDefault().LoadingPosition = warehousePalletItem.LoadingPosition;
            }

            var palletListItems = new List<SelectListItem>();
            palletListItems.Add(new SelectListItem { Text = "-", Value = "-" });
            foreach (var palletItem in palletDTOs.Where(k => k.StatusId == (int)PalletStatus.Created))
                palletListItems.Add(new SelectListItem
                {
                    Text = "Παραγωγή: " + palletItem.PackagingLineId + " | " + "Παλέτα: " + palletItem.PalletId + " | " + "Σειρά: " + palletItem.PackagingLinePalletNo + " | " + "Χώρα: " + palletItem.CountryCode + " | " + "SAP: " + palletItem.Material.MaterialSAPID.TrimStart('0') + " | " + "LOT: " + palletItem.Material.MaterialLOT
                    + " | " + "Προϊόν: " + palletItem.Material.MaterialDescription,
                    Value = palletItem.PalletId.ToString()
                });

            var WarehouseAreaListItems = new List<SelectListItem>();
            WarehouseAreaListItems.Add(new SelectListItem { Text = "-", Value = "-" });

            var positionListItems = new List<SelectListItem>();
            positionListItems.Add(new SelectListItem { Text = "-", Value = "-" });

            for (int i = 1; i < 9; i++)
                WarehouseAreaListItems.Add(new SelectListItem { Text = ((Common.WarehouseArea)i).AsString(EnumFormat.Description), Value = i.ToString() });

            return View(new WarehouseDTO { WarehouseAreaListItems = WarehouseAreaListItems, PositionListItems = positionListItems, PalletListItems = palletListItems, PalletDTOs = palletDTOs, SucceededPastAction = succeededPastAction == true ? true : false });
        }
        public async Task<JsonResult> CaptureImage(string name)
        {
            try
            {
                var files = HttpContext.Request.Form.Files;
                if (files != null)
                {
                    foreach (var file in files)
                    {
                        if (file.Length > 0)
                        {
                            var fileName = file.FileName;
                            var fileNameToStore = string.Concat(Convert.ToString(Guid.NewGuid()), System.IO.Path.GetExtension(fileName));
                            //  Path to store the snapshot in local folder
                            var filepath = System.IO.Path.Combine(_environment.WebRootPath, "images", "temp-qr-codes") + $@"\{fileNameToStore}";

                            // Save image file in local folder
                            if (!string.IsNullOrEmpty(filepath))
                            {
                                using (System.IO.FileStream fileStream = System.IO.File.Create(filepath))
                                {
                                    file.CopyTo(fileStream);
                                    fileStream.Flush();
                                }
                            }

                            // Save image file in database
                            var imgBytes = System.IO.File.ReadAllBytes(filepath);
                            if (imgBytes != null)
                            {
                                if (imgBytes != null)
                                {
                                    string base64String = Convert.ToBase64String(imgBytes, 0, imgBytes.Length);
                                    string imageUrl = string.Concat("data:image/jpg;base64,", base64String);

                                    // Code to store into database
                                    // save filename and image url(base 64 string) to the database
                                }
                            }

                            var filepath1 = System.IO.Path.Combine(_environment.WebRootPath, "images", "temp-qr-codes", "20210903_160642052_iOS_1.jpg");
                            //QRCodeScanner qRCodeScanner = new QRCodeScanner();
                            //qRCodeScanner.IsQRCodeFound(filepath1);
                            var reader = new BarcodeReaderGeneric();
                            System.Drawing.Bitmap image = (System.Drawing.Bitmap)System.Drawing.Image.FromFile(filepath1);

                            using (image)
                            {
                                LuminanceSource source;
                                source = new ZXing.BitmapLuminanceSource(image);
                                //decode text from LuminanceSource
                                Result result = reader.Decode(source);

                                var packagingLineDTO = await _context.PackagingLineForms.Include(k => k.Material).ThenInclude(k => k.Country).Where(k => k.PackagingLineFormId == int.Parse(protector.Encode(result.Text.Substring(result.Text.LastIndexOf('=') + 1)))).Select(x => new PackagingLineFormDTO
                                {
                                    PackagingLineId = x.PackagingLineFormId,
                                    PassToken = protector.Decode(x.PackagingLineFormId.ToString()),
                                    Material = new MaterialDTO { PalletFactor = x.Material.PalletFactor, ExpirationDate = ClassConverters.GetExpirationDate(x.FormCreationDate, x.Material.ExpirationDays) },
                                    MaterialDescription = x.Material.MaterialDescription,
                                    PackagingLineDescription = x.Material.PackagingLineDescription,
                                    MaterialLOT = x.PalletsLot,
                                    CountryCode = x.Material.Country.Code,
                                    NoPalletBox = _context.PackagingLineFormPallets.Count(l => l.PalletId == x.PackagingLineFormId),
                                    FormDate = x.FormCreationDate,
                                    RegistrationDateTime = x.RegistrationDateTime,
                                    CompletionDateTime = x.CompletionDateTime,
                                    Notes = x.Notes
                                }).FirstOrDefaultAsync();

                                return Json(packagingLineDTO);
                            }
                        }
                    }
                    return Json(true);
                }
                else
                {
                    return Json(false);
                }
            }
            catch (Exception)
            {
                return Json(false);
            }
        }
        public async Task<IActionResult> DeleteForm(string passToken)
        {
            if (passToken == null) NotFound();

            PackagingLineForm packagingLineForm = await _context.PackagingLineForms.FirstOrDefaultAsync(k => k.PackagingLineFormId == int.Parse(protector.Encode(passToken)));
            List<PackagingLineFormPallet> packagingLineFormPallets = await _context.PackagingLineFormPallets.Where(k => k.PackagingLineFormId == packagingLineForm.PackagingLineFormId).ToListAsync();
            var pallets = _context.Pallets.Where(k => packagingLineFormPallets.Select(l => l.PalletId).Contains(k.PalletId));
            _context.PackagingLineFormPallets.RemoveRange(packagingLineFormPallets);
            _context.Pallets.RemoveRange(pallets);
            _context.PackagingLineForms.Remove(packagingLineForm);

            //Clear from warehouse
            var warehouseItems = _context.Warehouses.Where(k => pallets.Select(l => l.PalletId).ToList().Contains(k.PalletId.Value)).ToList();
            foreach (var item in warehouseItems)
            {
                item.PalletId = null;
                item.ModificationDateTime = null;
            }
            _context.Warehouses.UpdateRange(warehouseItems);

            await _context.SaveChangesAsync();

            return RedirectToAction("PackagingLineForms", "PackagingLine", new { actionMessage = "Διαγράφτηκε το έντυπο με Α/Α  ΚΕΣ" + packagingLineForm.PackagingLineFormId.ToString() });
        }
        public IActionResult ExportPalletData(string? actionMessage)
        {
            ViewBag.actionMessage = actionMessage;
            return View();
        }
        public async Task<IActionResult> DownloadPalletData(string minDateToString, string maxDateToString)
        {
            try
            {
                DateTime minDate = Convert.ToDateTime(minDateToString);
                DateTime maxDate = Convert.ToDateTime(maxDateToString);

                var packagingLineFormPallets = _context.PackagingLineFormPallets.Include(k => k.PackagingLineForm).ThenInclude(k => k.Material).Include(k => k.Pallet).Where(k => k.PackagingLineForm.FormCreationDate >= minDate && k.PackagingLineForm.FormCreationDate <= maxDate).ToList();

                var packagingLineForms = _context.PackagingLineForms.Include(k => k.Material).Where(k => k.FormCreationDate >= minDate && k.FormCreationDate <= maxDate).ToList();

                if (packagingLineForms.Count == 0 || packagingLineFormPallets.Count == 0)
                {
                    return RedirectToAction("ExportPalletData", "PackagingLine", new { actionMessage = "Δεν βρέθηκαν δεδομένα" });
                }

                List<ExportPalletDTO> exportPalletDTOs = new List<ExportPalletDTO>();
                foreach (var packagingLineItem in packagingLineForms)
                {
                    exportPalletDTOs.Add(new ExportPalletDTO
                    {
                        PackagingLineId = packagingLineItem.PackagingLineFormId,
                        PalletLOT = ClassConverters.GetLot(packagingLineItem.FormCreationDate),
                        MaterialDescription = packagingLineItem.Material.MaterialDescription,
                        MaterialSAPID = packagingLineItem.Material.MaterialSapid.TrimStart('0'),
                        PalletNo = _context.PackagingLineFormPallets.Include(k => k.Pallet).Where(k => k.PackagingLineFormId == packagingLineItem.PackagingLineFormId && k.Pallet.PalletCreationDateTime != null).Count(),
                        CreationDateTime = packagingLineItem.FormCreationDate,
                        ExpirationDate = ClassConverters.GetExpirationDate(packagingLineItem.FormCreationDate, packagingLineItem.Material.ExpirationDays)
                    });
                }

                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine("" +
                    "A/A, " +
                    "Product description, " +
                    "product LOT, " +
                    "Sap code, " +
                    "Creation date, " +
                    "Expiration date, " +
                    "Pallet count");
                foreach (var exportPalletItem in exportPalletDTOs)
                {
                    stringBuilder.AppendLine(string.Format("{0},{1},{2},{3},{4},{5},{6}", 
                        exportPalletItem.PackagingLineId, 
                        exportPalletItem.MaterialDescription, 
                        exportPalletItem.PalletLOT,
                        exportPalletItem.MaterialSAPID.TrimStart('0'), 
                        exportPalletItem.CreationDateTime.ToString("dd/MM/yyyy"), 
                        exportPalletItem.ExpirationDate.ToString("dd/MM/yyyy"), 
                        exportPalletItem.PalletNo));
                }
                return File(Encoding.UTF8.GetBytes(stringBuilder.ToString()), "text/csv", string.Format("{0}_report_{1}.csv", "ari_packagingline", DateTimeOffset.Now.ToUnixTimeSeconds()));
            }
            catch (Exception ex)
            {
                return RedirectToAction("ExportPalletData", "PackagingLine");
            }
        }
        public async Task<IActionResult> DownloaPackagingData(string minDateToString, string maxDateToString)
        {
            try
            {
                DateTime minDate = Convert.ToDateTime(minDateToString);
                DateTime maxDate = Convert.ToDateTime(maxDateToString);

                var packagingLineFormPallets = _context.PackagingLineFormPallets.Include(k => k.PackagingLineForm).ThenInclude(k => k.Material).Include(k => k.Pallet).Where(k => k.PackagingLineForm.FormCreationDate >= minDate && k.PackagingLineForm.FormCreationDate <= maxDate).ToList();

                var packagingLineForms = _context.PackagingLineForms.Include(k => k.Material).Where(k => k.FormCreationDate >= minDate && k.FormCreationDate <= maxDate).ToList();

                if (packagingLineForms.Count == 0 || packagingLineFormPallets.Count == 0)
                {
                    return RedirectToAction("ExportPalletData", "PackagingLine", new { actionMessage = "Δεν βρέθηκαν δεδομένα" });
                }

                List<PackagingLineFormDTO> exportPalletDTOs = new List<PackagingLineFormDTO>();
                foreach (var packagingLineItem in packagingLineForms)
                {
                    var _PackagingLineFormId = packagingLineItem.PackagingLineFormId;
                    var _MaterialLOT = ClassConverters.GetLot(packagingLineItem.FormCreationDate);
                    var _MaterialDescription = packagingLineItem.Material.MaterialDescription;
                    var _CountryCode = _context.Countries.First(x => x.CountryId == packagingLineItem.CountryId).Code;
                    var _NoCompletedPallets = _context.PackagingLineFormPallets.Include(k => k.Pallet).Where(k => k.PackagingLineFormId == packagingLineItem.PackagingLineFormId && k.Pallet.PalletCreationDateTime != null).Count();
                    var _NoPalletBox = _context.PackagingLineFormPallets.Include(k => k.Pallet).Where(k => k.PackagingLineFormId == packagingLineItem.PackagingLineFormId).Count();
                    var _RegistrationDateTime = packagingLineItem.RegistrationDateTime;
                    var _StatusId = packagingLineItem.Status;
                    var _FormatDate = packagingLineItem.FormCreationDate;
                    var itm = new PackagingLineFormDTO
                    {
                        PackagingLineId = _PackagingLineFormId,
                        MaterialLOT = _MaterialLOT,
                        MaterialDescription = _MaterialDescription,
                        CountryCode = _CountryCode,
                        NoCompletedPallets = _NoCompletedPallets,
                        NoPalletBox = _NoPalletBox,
                        RegistrationDateTime = _RegistrationDateTime,
                        StatusId = _StatusId,
                        FormDate = _FormatDate,
                        SelectedStatus = _StatusId.ToString()
                    };
                    exportPalletDTOs.Add(itm);
                }

                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine("" +
                    "A/A, " +
                     "Ημέρα εντύπου," +
                    "Κωδικοποίηση παλέτας," +
                    "Προϊόν," +
                    "LOT παραγωγής," +
                    "Χώρα," +
                    "Αριθμός ολοκλ.παλετών," +
                    "Ημερομηνία Δημιουργίας," +
                    "Κατάσταση");
                foreach (var exportPalletItem in exportPalletDTOs)
                {
                    var completedPalletsNo = exportPalletItem.NoCompletedPallets.ToString() + "/" + exportPalletItem.NoPalletBox.ToString();
                    stringBuilder.AppendLine(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}",
                        "ΚΕΣ " + exportPalletItem.PackagingLineId,
                        exportPalletItem.FormDate.ToString("dd/MM/yyyy HH:mm:ss"),
                        exportPalletItem.MaterialDescription,
                        exportPalletItem.PackagingLineDescription,
                        exportPalletItem.MaterialLOT,
                        exportPalletItem.CountryCode,
                        completedPalletsNo,
                        exportPalletItem.RegistrationDateTime.ToString("dd/MM/yyyy HH:mm:ss"),
                        exportPalletItem.SelectedStatus
                        ));
                }

                var data = Encoding.UTF8.GetBytes(stringBuilder.ToString());
                var result = Encoding.UTF8.GetPreamble().Concat(data).ToArray();
                return File(result, "text/csv", string.Format("{0}_report_{1}.csv", "ari_packagingline", DateTimeOffset.Now.ToUnixTimeSeconds()));
            }
            catch (Exception ex)
            {
                return RedirectToAction("ExportPalletData", "PackagingLine");
            }
        }
        [HttpGet(Name = "Trasfer the pallet from packaging line to warehouse")]
        public async Task<JsonResult> TransferPalletToWarehouse(string palletId, string warehouseId, string loadinglineId)
        {
            var pallet = _context.Pallets.Where(k => k.PalletId == int.Parse(palletId)).FirstOrDefault();
            if (pallet.IsVirtual == 1)
                return Json("Error");

            if (warehouseId == "-" || palletId == "-" || (loadinglineId == "-" && warehouseId != "2" && warehouseId != "7"))
                return Json("Error");

            if (loadinglineId == "-")
                loadinglineId = "0";

            var warehouseSpot = _context.Warehouses.Where(k => k.LoadingAreaId == int.Parse(warehouseId) && k.LoadingLine == int.Parse(loadinglineId) && k.PalletId == null).OrderBy(k => k.LoadingPosition).FirstOrDefault();
            warehouseSpot.PalletId = int.Parse(palletId);
            warehouseSpot.ModificationDateTime = DateTime.Now;
            _context.Update(warehouseSpot);

            
            pallet.Status = 2;
            _context.Update(pallet);
            await _context.SaveChangesAsync();

            if (warehouseSpot == null)
                return Json("Error1");

            return Json("Completed");
        }
        [HttpGet(Name = "Trasfer the pallet from packaging line to warehouse")]
        public async Task<JsonResult> TransferPositionPalletToWarehouse(string palletId, string warehouseId, string loadinglineId)
        {
            if (warehouseId == "-" || palletId == "-" || (loadinglineId == "-" && warehouseId != "2" && warehouseId != "7"))
                return Json("Error");

            if (loadinglineId == "-")
                loadinglineId = "0";

            //Find the pallet to Warehouse
            var warehouseInitialSpot = _context.Warehouses.Where(k => k.PalletId == int.Parse(palletId)).FirstOrDefault();
            warehouseInitialSpot.PalletId = null;
            warehouseInitialSpot.ModificationDateTime = null;
            _context.Update(warehouseInitialSpot);
            _context.SaveChanges();

            //Move the pallet to Warehouse
            var warehouseSpot = _context.Warehouses.Where(k => k.LoadingAreaId == int.Parse(warehouseId) && k.LoadingLine == int.Parse(loadinglineId) && k.PalletId == null).OrderBy(k => k.LoadingPosition).FirstOrDefault();
            warehouseSpot.PalletId = int.Parse(palletId);
            warehouseSpot.ModificationDateTime = DateTime.Now;
            _context.Update(warehouseSpot);

            var pallet = _context.Pallets.Where(k => k.PalletId == int.Parse(palletId)).FirstOrDefault();
            pallet.Status = 2;
            _context.Update(pallet);
            await _context.SaveChangesAsync();

            if (warehouseSpot == null)
                return Json("Error1");

            return Json("Completed");
        }
        [HttpGet(Name = "Get the Available lines")]
        public async Task<JsonResult> LoadingLines(int loadingAreaId)
        {
            var positionListItems = new List<SelectListItem>();
            //if has lines
            if (loadingAreaId == (int)WarehouseArea.Sala1 || loadingAreaId == (int)WarehouseArea.Proektasi || loadingAreaId == (int)WarehouseArea.Sala2DeksiaPteriga
                || loadingAreaId == (int)WarehouseArea.Sala2AristeriPteriga || loadingAreaId == (int)WarehouseArea.Sala2Domatio)
            {
                positionListItems.Add(new SelectListItem { Text = "-", Value = "-" });
                var lines = _context.Warehouses.Where(k => k.LoadingAreaId == loadingAreaId && k.PalletId == null).Select(k => k.LoadingLine).Distinct();
                foreach (var line in lines)
                    positionListItems.Add(new SelectListItem { Text = "L" + line.ToString(), Value = line.ToString() });
            }
            else
            {
                positionListItems.Add(new SelectListItem { Text = "ΧΩΡΙΣ ΣΕΙΡΑ", Value = "-" });
            }
            return Json(positionListItems);
        }
        public IActionResult PalleteScanner()
        {
            return View();
        }
        public IActionResult PalleteStatus()
        {
            return View();
        }
        public IActionResult PalletScanner()
        {
            return View();
        }
        [Authorize]
        public IActionResult PalletQRInfo(string passToken)
        {
            PalletQRInfoDTO palletQRInfoDTO = new PalletQRInfoDTO();

            if (int.TryParse(passToken, out int result))
                passToken = protector.Decode(passToken);

            var palletDTO = _context.Pallets.Where(k => k.PalletId == int.Parse(protector.Encode(passToken))).Select(k => new PalletDTO
            {

                PalletId = k.PalletId,
                Material = _context.Materials1.Where(l => l.MaterialId == k.MaterialId).Select(x => new MaterialDTO
                {
                    MaterialSAPID = x.MaterialSapid.TrimStart('0'),
                    MaterialLOT = (k.PalletCreationDateTime != null)? ClassConverters.GetLot(k.PalletCreationDateTime.Value) : 0,
                    MaterialDescription = x.MaterialDescription,
                    PackagingLineDescription = x.PackagingLineDescription,
                    MaxDeliveryDate = (k.PalletCreationDateTime != null) ? ClassConverters.GetMaxDeliveryDate(k.PalletCreationDateTime.Value, x.LagerDays) : new DateTime(1900,01,01),
                    ExpirationDate = (k.PalletCreationDateTime != null) ? ClassConverters.GetExpirationDate(k.PalletCreationDateTime.Value, x.ExpirationDays): new DateTime(1900, 01, 01),
                }).FirstOrDefault(),
                CountryCode = _context.Materials1.Include(j => j.Country).FirstOrDefault(j => j.MaterialId == k.MaterialId).Country.Code,
                PackagingLinePalletNo = k.PackagingLinePalletNo,
                QRCodeLink = k.Qrcode,
                BarCodeLink = k.BarCode,
                PalletCardLink = k.PalletCard,
                PalletCardLIDLLink = k.PalletCardLidl,
                NoPalletBox = _context.Materials1.Include(j => j.Country).FirstOrDefault(k => k.MaterialId == k.MaterialId).PalletFactor,
                StatusId = k.Status,
                SelectedStatus = k.Status.ToString(),
                CreationDateTime = k.PalletCreationDateTime.ToString()
            }).FirstOrDefault();
            palletQRInfoDTO.palletDTO = palletDTO;

            var order = _context.TruckOrderPallets.Where(k => k.PalletId == int.Parse(protector.Encode(passToken))).FirstOrDefault();
            if (order != null)
            {
                var orderDTO = _context.Orders.Where(k => k.OrderId == order.OrderId).Select(k => new EDIOrderDTO
                {
                    OrderId = k.OrderId,
                    PassToken = protector.Decode(k.OrderId.ToString()),
                    ReferenceNumber = k.ReferenceNumber,
                    ShipToParty = new CustomerDTO { Name1 = _context.Customers.Where(x => x.CustomerId == k.ShipToParty).FirstOrDefault().Name1 },
                    SoldToParty = new CustomerDTO { Name1 = _context.Customers.Where(x => x.CustomerId == k.SoldToParty).FirstOrDefault().Name1 },
                    CountryCode = _context.Materials1.Include(j => j.Country).FirstOrDefault(j => j.MaterialId == k.MaterialId).Country.Code,
                    DocumentDate = k.DocumentDate,
                    PurchaseOrderDate = k.PurchaseOrderDate,
                    RequestedDeliveryDate = k.RequestedDeliveryDate
                }).FirstOrDefault();
                palletQRInfoDTO.eDIOrderDTO = orderDTO;
            }

            return View(palletQRInfoDTO);
        }
        public IActionResult PalletBarcodeInfo(string palletId)
        {
            if (palletId.Contains(')'))
                palletId = palletId.Split(')')[1];
            else
                return RedirectToAction(nameof(PalletScanner));
            
            PalletQRInfoDTO palletQRInfoDTO = new PalletQRInfoDTO();

            var palletDTO = _context.Pallets.Where(k => k.PalletId == int.Parse(palletId)).Select(k => new PalletDTO
            {
                PalletId = k.PalletId,
                Material = _context.Materials1.Where(l => l.MaterialId == k.MaterialId).Select(x => new MaterialDTO
                {
                    MaterialSAPID = x.MaterialSapid.TrimStart('0'),
                    MaterialLOT = (k.PalletCreationDateTime != null) ? ClassConverters.GetLot(k.PalletCreationDateTime.Value) : 0,
                    MaterialDescription = x.MaterialDescription,
                    PackagingLineDescription = x.PackagingLineDescription,
                    MaxDeliveryDate = (k.PalletCreationDateTime != null) ? ClassConverters.GetMaxDeliveryDate(k.PalletCreationDateTime.Value, x.LagerDays) : new DateTime(1900, 01, 01),
                    ExpirationDate = (k.PalletCreationDateTime != null) ? ClassConverters.GetExpirationDate(k.PalletCreationDateTime.Value, x.ExpirationDays) : new DateTime(1900, 01, 01),
                }).FirstOrDefault(),
                CountryCode = _context.Materials1.Include(j => j.Country).FirstOrDefault(j => j.MaterialId == k.MaterialId).Country.Code,
                PackagingLinePalletNo = k.PackagingLinePalletNo,
                QRCodeLink = k.Qrcode,
                BarCodeLink = k.BarCode,
                PalletCardLink = k.PalletCard,
                PalletCardLIDLLink = k.PalletCardLidl,
                NoPalletBox = _context.Materials1.Include(j => j.Country).FirstOrDefault(k => k.MaterialId == k.MaterialId).PalletFactor,
                StatusId = k.Status,
                SelectedStatus = k.Status.ToString(),
                CreationDateTime = k.PalletCreationDateTime.ToString()
            }).FirstOrDefault();
            palletQRInfoDTO.palletDTO = palletDTO;

            var order = _context.TruckOrderPallets.Where(k => k.PalletId == int.Parse(palletId)).FirstOrDefault();
            if (order != null)
            {
                var orderDTO = _context.Orders.Where(k => k.OrderId == order.OrderId).Select(k => new EDIOrderDTO
                {
                    OrderId = k.OrderId,
                    PassToken = protector.Decode(k.OrderId.ToString()),
                    ReferenceNumber = k.ReferenceNumber,
                    ShipToParty = new CustomerDTO { Name1 = _context.Customers.Where(x => x.CustomerId == k.ShipToParty).FirstOrDefault().Name1 },
                    SoldToParty = new CustomerDTO { Name1 = _context.Customers.Where(x => x.CustomerId == k.SoldToParty).FirstOrDefault().Name1 },
                    CountryCode = _context.Materials1.Include(j => j.Country).FirstOrDefault(j => j.MaterialId == k.MaterialId).Country.Code,
                    DocumentDate = k.DocumentDate,
                    PurchaseOrderDate = k.PurchaseOrderDate,
                    RequestedDeliveryDate = k.RequestedDeliveryDate
                }).FirstOrDefault();
                palletQRInfoDTO.eDIOrderDTO = orderDTO;
            }

            return View(palletQRInfoDTO);
        }
        [HttpGet(Name = "Get Pallet info")]
        public async Task<JsonResult> GetCurrPalletWarehouseInfo(string warehouseName, string loadinglineId, string itemId)
        {
            if (loadinglineId == "-")
                loadinglineId = "0";

            string warehouseId = string.Empty;
            if (warehouseName.Contains("sala-1-warehouse"))
                warehouseId = "1";
            else if (warehouseName.Contains("sala-1-psigeio-warehouse"))
                warehouseId = "2";
            else if (warehouseName.Contains("proektasi-warehouse"))
                warehouseId = "3";
            else if (warehouseName.Contains("sala-2-deksia-warehouse"))
                warehouseId = "4";
            else if (warehouseName.Contains("sala-2-aristeri-warehouse"))
                warehouseId = "5";
            else if (warehouseName.Contains("sala-2-domatio-warehouse"))
                warehouseId = "6";
            else if (warehouseName.Contains("makio-warehouse"))
                warehouseId = "7";

            PalletDTO palletDTO = new PalletDTO();
            int palletId = _context.Warehouses.Where(k => k.LoadingAreaId == int.Parse(warehouseId) && k.LoadingLine == int.Parse(loadinglineId) && k.LoadingPosition == int.Parse(itemId)).FirstOrDefault().PalletId.Value;
            var packagingLineFormId = _context.PackagingLineFormPallets.Include(k => k.PackagingLineForm).FirstOrDefault(l => l.PalletId == palletId).PackagingLineFormId;
            var formCreationDate = _context.PackagingLineForms.FirstOrDefault(k => k.PackagingLineFormId == packagingLineFormId).FormCreationDate;

            palletDTO = _context.Pallets.Where(k => k.PalletId == palletId).Select(k => new PalletDTO
            {
                PalletId = k.PalletId,
                Material = _context.Materials1.Where(l => l.MaterialId == k.MaterialId).Select(x => new MaterialDTO
                {
                    MaterialSAPID = x.MaterialSapid.TrimStart('0'),
                    MaterialLOT = ClassConverters.GetLot(formCreationDate),
                    MaterialDescription = x.MaterialDescription,
                    PackagingLineDescription = x.PackagingLineDescription,
                    MaxDeliveryDate = ClassConverters.GetMaxDeliveryDate(formCreationDate, x.LagerDays),
                    ExpirationDate = ClassConverters.GetExpirationDate(formCreationDate, x.ExpirationDays)
                }).FirstOrDefault(),
                CountryCode = _context.Materials1.Include(j => j.Country).FirstOrDefault(k => k.MaterialId == k.MaterialId).Country.Code,
                PackagingLinePalletNo = k.PackagingLinePalletNo,
                QRCodeLink = k.Qrcode,
                BarCodeLink = k.BarCode,
                PalletCardLink = k.PalletCard,
                PalletCardLIDLLink = k.PalletCardLidl,
                NoPalletBox = _context.Materials1.Include(j => j.Country).FirstOrDefault(k => k.MaterialId == k.MaterialId).PalletFactor,
                StatusId = k.Status,
                SelectedStatus = ((PalletStatus)k.Status).AsString(EnumFormat.Description),
                CreationDateTime = k.PalletCreationDateTime.Value.ToString("HH:mm")
            }).FirstOrDefault();
            return Json(palletDTO);
        }
    }
}

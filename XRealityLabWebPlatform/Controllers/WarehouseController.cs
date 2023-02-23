/*using Application.Security;
using ARI.Common;
using ARI.Helpers;
using ARI.Models;
using ARI.PackagingLine;
using ARI.PDFTemplates.Models;
using ARI.Users.Queries;
using AzureStorage;
using Common.Constants;
using EdiTools;
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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZXing.QrCode;

namespace ARI.Controllers
{
    [Authorize]
    public class WarehouseController : Controller
    {
        private readonly ARIContext _context;
        private IUserRepository _userRepository;
        private readonly IHostingEnvironment _environment;
        private readonly CustomIDataProtection protector;

        public WarehouseController(ARIContext context, IHostingEnvironment IHostingEnvironment, CustomIDataProtection customIDataProtection)
        {
            _context = context;
            _userRepository = new UserRepository(_context);
            _environment = IHostingEnvironment;
            protector = customIDataProtection;
        }

        //Actions
        public async Task<IActionResult> TopView(string actionMessage)
        {
            List<PalletDTO> palletDTOs = new List<PalletDTO>();

            var packagingLines = await _context.PackagingLineForms.Where(k => k.Status != (int)PackagingLineStatus.Cancel).ToListAsync();
            foreach (var packagingLineItem in packagingLines)
            {
                palletDTOs.AddRange(_context.PackagingLineFormPallets.Include(k => k.Pallet).ThenInclude(k => k.Material).ThenInclude(k => k.Country).Where(k => k.PackagingLineForm == packagingLineItem && k.Pallet.PalletCreationDateTime != null).Select(k => new PalletDTO
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

            return View(new WarehouseDTO { WarehouseAreaListItems = WarehouseAreaListItems, PositionListItems = positionListItems, PalletListItems = palletListItems, PalletDTOs = palletDTOs });
        }
        public async Task<IActionResult> Load()
        {
            var load = await _context.Loads.Include(k => k.LoadPallets).ThenInclude(k => k.Pallet).Include(k => k.LoadOrders).ThenInclude(k => k.Order)
                .Select(k => new LoadDTO
                {
                    LoadId = k.LoadId,
                    PassToken = protector.Decode(k.LoadId.ToString()),
                    CreationDateTime = k.CreationDateTime,
                    IsMatch = false,
                    LoadMetricsDTO = new LoadMetricsDTO
                    {
                        NoOrders = k.LoadOrders.Count(),
                        NoOrderPallets = k.LoadOrders.Select(k => k.Order.Quantity).Sum(),
                        NoPallets = k.LoadPallets.Count()
                    },
                    StatusId = k.Status,
                    SelectedStatus = ((PackagingLineStatus)k.Status).AsString(EnumFormat.Description),
                    PalletCardsLIDL = k.PalletCardsLidl,
                    DriverDocuments = k.DriverDocuments,
                    CMRDocuments = k.Cmrdocuments,
                    LIDLOrderHeaders = k.LidlorderHeaders,
                    LIDLOrderLines = k.LidlorderLines
                }).ToListAsync();

            foreach (var loadItem in load)
            {
                var _pallets = await _context.Pallets.Include(k => k.LoadPallets).Where(k => k.LoadPallets.Select(k => k.LoadId).Contains(loadItem.LoadId)).ToListAsync();
                var _orderPallets = await _context.Orders.Include(k => k.LoadOrders).Where(k => k.LoadOrders.Select(k => k.LoadId).Contains(loadItem.LoadId)).ToListAsync();

                //Orders
                List<MaterialPalletDTO> materialOrderPalletDTO = new List<MaterialPalletDTO>();
                foreach (var _orderPallet in _orderPallets)
                {
                    if (materialOrderPalletDTO.Select(k=> k.MaterialId).Contains(_orderPallet.MaterialId))
                    {
                        materialOrderPalletDTO.Where(k => k.MaterialId == _orderPallet.MaterialId).FirstOrDefault().Quantity =
                            materialOrderPalletDTO.Where(k => k.MaterialId == _orderPallet.MaterialId).FirstOrDefault().Quantity + _orderPallet.Quantity;
                    }
                    else
                    {
                        materialOrderPalletDTO.Add(new MaterialPalletDTO
                        {
                            MaterialId = _orderPallet.MaterialId,
                            Quantity = _orderPallet.Quantity
                        });
                    }
                }

                //Pallets
                List<MaterialPalletDTO> materialPalletDTO = new List<MaterialPalletDTO>();
                foreach (var _pallet in _pallets)
                {
                    if (materialPalletDTO.Select(k => k.MaterialId).Contains(_pallet.MaterialId))
                    {
                        materialPalletDTO.Where(k => k.MaterialId == _pallet.MaterialId).FirstOrDefault().Quantity =
                            materialPalletDTO.Where(k => k.MaterialId == _pallet.MaterialId).FirstOrDefault().Quantity + 1;
                    }
                    else
                    {
                        materialPalletDTO.Add(new MaterialPalletDTO
                        {
                            MaterialId = _pallet.MaterialId,
                            Quantity = 1
                        });
                    }
                }

                materialOrderPalletDTO = materialOrderPalletDTO.OrderBy(k => k.MaterialId).ToList();
                materialPalletDTO = materialPalletDTO.OrderBy(k => k.MaterialId).ToList();
                if (materialOrderPalletDTO.Count == materialPalletDTO.Count && materialOrderPalletDTO.Count != 0 && materialPalletDTO.Count != 0)
                {
                    for (int i = 0; i < materialOrderPalletDTO.Count(); i++)
                    {
                        if (materialOrderPalletDTO[i].MaterialId != materialPalletDTO[i].MaterialId ||
                           materialOrderPalletDTO[i].Quantity != materialPalletDTO[i].Quantity)
                        {
                            loadItem.LoadMetricsDTO.IsIdentified = false;
                            loadItem.LoadMetricsDTO.Label = "Δεν ταυτίζονται οι παλέτες από το απόθεμα με τις παλέτες της παραγγελίας";
                            loadItem.IsMatch = false;
                            _context.Loads.Where(k => k.LoadId == loadItem.LoadId).FirstOrDefault().IsMatch = 0;
                            await _context.SaveChangesAsync();
                            break;
                        }
                        else
                        {
                            loadItem.LoadMetricsDTO.IsIdentified = true;
                            loadItem.LoadMetricsDTO.Label = "Πραγματοποιήθηκε η ταύτιση (Picking) παραγγελιών με τις παλέτες με επιτυχία";
                            loadItem.IsMatch = true;
                            _context.Loads.Where(k => k.LoadId == loadItem.LoadId).FirstOrDefault().IsMatch = 1;
                            await _context.SaveChangesAsync();
                        }
                    }
                }
                else
                {
                    loadItem.LoadMetricsDTO.IsIdentified = false;
                    loadItem.LoadMetricsDTO.Label = "Δεν βρέθηκαν παλέτες και παραγγελίες για την συγκεκριμένη φόρτωση";
                    loadItem.IsMatch = true;
                    _context.Loads.Where(k => k.LoadId == loadItem.LoadId).FirstOrDefault().IsMatch = 0;
                    await _context.SaveChangesAsync();
                }
                
            }

            return View(load);
        }
        public async Task<IActionResult> EditLoadOrders(string passToken)
        {
            if (passToken == null || passToken == string.Empty) return NotFound();

            var user = await _context.Users.Where(k => k.Email == User.Identity.Name).FirstOrDefaultAsync();
            var load = await _context.Loads.Include(k=>k.LoadOrders).Where(k => k.LoadId == int.Parse(protector.Encode(passToken))).FirstOrDefaultAsync();
            
            LoadDTO loadDTO = new LoadDTO();
            loadDTO.LoadId = load.LoadId;
            loadDTO.PassToken = protector.Decode(load.LoadId.ToString());
            loadDTO.EDIOrderDTOs = await GetOrdersModelAsync();
            var orderIdsToIgnore = _context.TruckOrderPallets.Select(k => k.OrderId).ToList();
            loadDTO.EDIOrderDTOs = loadDTO.EDIOrderDTOs.Where(k => !orderIdsToIgnore.Contains(k.OrderId)).ToList();
            loadDTO.EDIOrderDTOs.Where(k => load.LoadOrders.Select(l=>l.OrderId).Contains(k.OrderId)).Select(c => { c.IsSelectedForLoad = true; return c; }).ToList();
            loadDTO.GroupOrderDTOs = AddGroupOrders(loadDTO.EDIOrderDTOs.Where(k=>k.IsSelectedForLoad).ToList());
            loadDTO.LoadOrderFilter = _context.LoadOrderFilters.Where(k => k.UserId == user.UserId).FirstOrDefault();
            loadDTO.EDIOrderDTOs = GetFilterOrders(loadDTO.EDIOrderDTOs);

            //REDESIGN
            List<PalletDTO> palletDTOs = await GetPalletsModelAsync();
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
            //

            loadDTO.WarehouseDTO = new WarehouseDTO { WarehouseAreaListItems = WarehouseAreaListItems, PositionListItems = positionListItems, PalletListItems = palletListItems, PalletDTOs = palletDTOs};

            return View(loadDTO);
        }
        public async Task<IActionResult> AddBlankLoad()
        {
            await _context.Loads.AddAsync(new Load { CreationDateTime = DateTime.Now, Status = 0 });
            await _context.SaveChangesAsync();

            return RedirectToAction("Load", "Warehouse");
        }
        public async Task<IActionResult> EditLoadPallets(string passToken)
        {
            if (passToken == null || passToken == string.Empty) return NotFound();

            var user = await _context.Users.Where(k => k.Email == User.Identity.Name).FirstOrDefaultAsync();
            var load = await _context.Loads.Include(k => k.LoadOrders).Where(k => k.LoadId == int.Parse(protector.Encode(passToken))).FirstOrDefaultAsync();

            LoadDTO loadDTO = new LoadDTO();
            loadDTO.LoadId = load.LoadId;
            loadDTO.PassToken = protector.Decode(load.LoadId.ToString());

            List<PalletDTO> palletDTOs = new List<PalletDTO>();

            var packagingLines = await _context.PackagingLineForms.Where(k => k.Status != (int)PackagingLineStatus.Cancel).ToListAsync();
            foreach (var packagingLineItem in packagingLines)
            {
                palletDTOs.AddRange(_context.PackagingLineFormPallets.Include(k => k.Pallet).ThenInclude(k => k.Material).ThenInclude(k => k.Country).Where(k => k.PackagingLineForm == packagingLineItem && k.Pallet.PalletCreationDateTime != null).Select(k => new PalletDTO
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
                    CountryCode = k.Pallet.Material.Country.Code,
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

            var loadPallets = await _context.LoadPallets.Where(k => k.LoadId == loadDTO.LoadId).ToListAsync();
            foreach (var palletDTO in palletDTOs)
                palletDTO.IsSelectedForLoad = loadPallets.Any(k => k.PalletId == palletDTO.PalletId);

            var warehousePallets = await _context.Warehouses.Where(k => k.PalletId != null).ToListAsync();
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

            loadDTO.WarehouseDTO = new WarehouseDTO
            {
                WarehouseAreaListItems = WarehouseAreaListItems,
                PositionListItems = positionListItems,
                PalletListItems = palletListItems,
                PalletDTOs = palletDTOs
            };

            return View(loadDTO);
        }



        //Events
        [HttpGet]
        public JsonResult GetPalletInfo(int palletId)
        {
            return (palletId == 0) ? new JsonResult(new PalletDTO()) : Json(ClassConverters.GetPalletInfo(_context, protector, palletId));
        }
        [HttpGet]
        public JsonResult GetPalletWarehouseInfo(string warehouseName, string loadinglineId, string itemId)
        {
            string warehouseId = warehouseName == "sala-1-warehouse" ? "1" : warehouseName == "sala-1-psigeio-warehouse" ? "2" : warehouseName == "proektasi-warehouse" ? "3" :
                warehouseName == "sala-2-deksia-warehouse" ? "4" : warehouseName == "sala-2-aristeri-warehouse" ? "5" : warehouseName == "sala-2-domatio-warehouse" ? "6" :
                warehouseName == "mikio-warehouse" ? "7" : "-";

            if (loadinglineId == "-") loadinglineId = "0";

            int palletId = _context.Warehouses.Where(k => k.LoadingAreaId == int.Parse(warehouseId) && k.LoadingLine == int.Parse(loadinglineId) && k.LoadingPosition == int.Parse(itemId)).FirstOrDefault().PalletId.Value;
            return (palletId == 0) ? new JsonResult(new PalletDTO()) : Json(ClassConverters.GetPalletInfo(_context, protector, palletId));
        }
        [HttpPost]
        public JsonResult GetOrder(IFormCollection data)
        {
            int orderId = int.Parse(data["order-id"]);
            int ariOrderId = int.Parse(data["order-ari-id"]);
            string action = data["action"];

            var order = _context.Orders.Include(k => k.Material).Where(k => k.OrderId == orderId).Select(k => new EDIOrderDTO
            {
                OrderId = k.OrderId,
                PassToken = protector.Decode(k.OrderId.ToString()),
                ReferenceNumber = k.ReferenceNumber,
                Material = new MaterialDTO
                {
                    MaterialId = k.Material.MaterialId,
                    MaterialSAPID = k.Material.MaterialSapid.TrimStart('0'),
                    MaterialDescription = k.Material.MaterialDescription,
                    PackagingLineDescription = k.Material.PackagingLineDescription,
                    GTIN = k.Material.Gtin
                },
                ShipToParty = new CustomerDTO { Name1 = _context.Customers1.Where(x => x.CustomerId == k.ShipToParty).FirstOrDefault().Name1 },
                SoldToParty = new CustomerDTO { Name1 = _context.Customers1.Where(x => x.CustomerId == k.SoldToParty).FirstOrDefault().Name1 },
                DocumentDate = k.DocumentDate,
                PurchaseOrderDate = k.PurchaseOrderDate,
                RequestedDeliveryDate = k.RequestedDeliveryDate,
                Quantity = k.Quantity,
                SelectedStatus = Convert.ToString(k.Status)
            }).FirstOrDefault();

            //if (data["action"] == "insert")
            //{
            //    if (!_context.Loads.Include(k => k.LoadOrder).Where(k => k.LoadOrder.OrderId == orderId).Any())
            //    {
            //        _context.LoadOrder.Add(new OrderAriorder { OrderAriid = ariOrderId, OrderId = orderId });
            //        _context.SaveChanges();
            //    }
            //}
            //else if (data["action"] == "delete")
            //{
            //    if (_context.OrderAris.Include(k => k.OrderAriorder).Where(k => k.OrderAriorder.OrderId == orderId).Any())
            //    {
            //        var orderAriorder = _context.OrderAriorders.Where(k => k.OrderAriid == ariOrderId && k.OrderId == orderId).FirstOrDefault();
            //        _context.Remove(orderAriorder);
            //        _context.SaveChanges();
            //    }
            //}

            return Json(order);
        }
        public List<EDIOrderDTO>  GetFilterOrders(List<EDIOrderDTO> eDIOrderDTOs)
        {
            var user = _context.Users.Where(k => k.Email == User.Identity.Name).FirstOrDefault();
            var loadOrderFilter = _context.LoadOrderFilters.Where(k => k.UserId == user.UserId).FirstOrDefault();
            if(loadOrderFilter == null)
            {
                loadOrderFilter = new LoadOrderFilter { UserId = user.UserId };
                _context.Add(loadOrderFilter);
                _context.SaveChanges();
            }

            foreach (var item in eDIOrderDTOs.Where(k => k.ShipToParty.Name1 == null))
                item.ShipToParty.Name1 = "-";

            foreach (var item in eDIOrderDTOs.Where(k => k.SoldToParty.Name1 == null))
                item.SoldToParty.Name1 = "-";

            var text = loadOrderFilter.Text != null? loadOrderFilter.Text.ToLower() : null;
            eDIOrderDTOs.Where(k => 
            (loadOrderFilter.Text != null? k.Material.MaterialDescription.ToLower().Contains(text) || k.Material.MaterialLOT.ToString().Contains(text) || k.Material.GTIN.Contains(text) || k.Material.MaterialSAPID.TrimStart('0').Contains(text)
                || k.ReferenceNumber.ToLower().Contains(text) || k.ShipToParty.Name1.ToLower().Contains(text) || k.SoldToParty.Name1.ToLower().Contains(text) || k.Material.PackagingLineDescription.ToLower().Contains(text) : true)
                && !k.IsSelectedForLoad 
                && (loadOrderFilter.DocumentFrom > DateTime.Now.AddYears(-20) ? k.DocumentDate > loadOrderFilter.DocumentFrom : true)
                && (loadOrderFilter.DocumentUntil > DateTime.Now.AddYears(-20) ? k.DocumentDate < loadOrderFilter.DocumentUntil : true)
                && (loadOrderFilter.PurchaseFrom > DateTime.Now.AddYears(-20) ? k.PurchaseOrderDate > loadOrderFilter.PurchaseFrom : true)
                && (loadOrderFilter.PurchaseUntil > DateTime.Now.AddYears(-20) ? k.PurchaseOrderDate > loadOrderFilter.PurchaseUntil : true)
                && (loadOrderFilter.RequestedFrom > DateTime.Now.AddYears(-20) ? k.RequestedDeliveryDate > loadOrderFilter.RequestedFrom : true)
                && (loadOrderFilter.RequestedUntil > DateTime.Now.AddYears(-20) ? k.RequestedDeliveryDate < loadOrderFilter.RequestedUntil : true))
                .Select(c => { c.IsFiltered = true; return c; }).ToList();

            return eDIOrderDTOs;

            //var orders = _context.Orders.Include(k => k.Material).Where(k => k.Status == 0 
            //&& (loadOrderFilter.DocumentFrom > DateTime.Now.AddYears(-20)?  k.DocumentDate > loadOrderFilter.DocumentFrom : true)
            //&& (loadOrderFilter.DocumentUntil > DateTime.Now.AddYears(-20) ? k.DocumentDate < loadOrderFilter.DocumentUntil : true)
            //&& (loadOrderFilter.PurchaseFrom > DateTime.Now.AddYears(-20) ? k.PurchaseOrderDate > loadOrderFilter.PurchaseFrom : true)
            //&& (loadOrderFilter.PurchaseUntil > DateTime.Now.AddYears(-20) ? k.PurchaseOrderDate > loadOrderFilter.PurchaseUntil : true)
            //&& (loadOrderFilter.RequestedFrom > DateTime.Now.AddYears(-20) ? k.RequestedDeliveryDate > loadOrderFilter.RequestedFrom : true)
            //&& (loadOrderFilter.RequestedUntil > DateTime.Now.AddYears(-20) ? k.RequestedDeliveryDate < loadOrderFilter.RequestedUntil : true)).Select(k => new EDIOrderDTO
            //{
            //    OrderId = k.OrderId,
            //    PassToken = protector.Decode(k.OrderId.ToString()),
            //    ReferenceNumber = k.ReferenceNumber,
            //    Material = new MaterialDTO
            //    {
            //        MaterialId = k.Material.MaterialId,
            //        MaterialSAPID = k.Material.MaterialSapid,
            //        MaterialDescription = k.Material.MaterialDescription,
            //        PackagingLineDescription = k.Material.PackagingLineDescription,
            //        GTIN = k.Material.Gtin
            //    },
            //    ShipToParty = new CustomerDTO { Name1 = _context.Customers1.Where(x => x.CustomerId == k.ShipToParty).FirstOrDefault().Name1 },
            //    SoldToParty = new CustomerDTO { Name1 = _context.Customers1.Where(x => x.CustomerId == k.SoldToParty).FirstOrDefault().Name1 },
            //    DocumentDate = k.DocumentDate,
            //    PurchaseOrderDate = k.PurchaseOrderDate,
            //    RequestedDeliveryDate = k.RequestedDeliveryDate,
            //    Quantity = k.Quantity,
            //    SelectedStatus = Convert.ToString(k.Status),
            //}).ToList();

            //var orderIdsToIgnore = _context.LoadOrders.Select(k => k.OrderId).ToList();
            //orders = orders.Where(k => !orderIdsToIgnore.Contains(k.OrderId)).ToList();
        }
        public async Task<IActionResult> ChangeOrderARIStatus(string passToken, int formStatus)
        {
            if (passToken == null || passToken == string.Empty) return NotFound();
            var orderAri = await _context.Loads.Where(k => k.LoadId == int.Parse(protector.Encode(passToken))).FirstOrDefaultAsync();
            if (formStatus == 1 && orderAri.IsMatch == 1)
            {
                var loadPallets = await _context.LoadPallets.Where(k => k.LoadId == orderAri.LoadId).ToListAsync();
                List<int> selectedPallets = new List<int>();
                foreach (var loadPallet in loadPallets)
                {
                    var pallet = _context.Pallets.Where(k => k.PalletId == loadPallet.PalletId).FirstOrDefault();
                    pallet.Status = 4;
                    _context.Update(pallet);
                    selectedPallets.Add(loadPallet.PalletId);
                }
                var loadOrders = await _context.LoadOrders.Where(k => k.LoadId == orderAri.LoadId).ToListAsync();
                List<int> selectedOrders = new List<int>();
                foreach (var loadOrder in loadOrders)
                {
                    var order = _context.Orders.Where(k => k.OrderId == loadOrder.OrderId).FirstOrDefault();
                    order.Status = 2;
                    _context.Update(order);
                    selectedOrders.Add(loadOrder.OrderId);
                }

                //Create the order-pallet connection for the database
                var orderDTOs = _context.Orders.Include(k => k.Material).Where(k => selectedOrders.Contains(k.OrderId)).Select(k => new EDIOrderDTO
                {
                    OrderId = k.OrderId,
                    ReferenceNumber = k.ReferenceNumber,
                    ShipToParty = _context.Customers.Include(k => k.Country).Where(x => x.CustomerId == k.ShipToParty).Select(x => new CustomerDTO
                    {
                        Name1 = x.Name1,
                        Name2 = x.Name2,
                        Address = x.Address.ToString(),
                        City = x.City,
                        Street = x.Street,
                        Country = x.Country.NameEn,
                        //PostalCode = x.PostalCode,
                        GTIN = x.LocationNo1 + x.LocationNo2 + x.CheckDigit
                    }).FirstOrDefault(),
                    SoldToParty = _context.Customers.Include(k => k.Country).Where(x => x.CustomerId == k.SoldToParty).Select(x => new CustomerDTO
                    {
                        Name1 = x.Name1,
                        Name2 = x.Name2,
                        Address = x.Address.ToString(),
                        City = x.City,
                        Street = x.Street,
                        Country = x.Country.NameEn,
                        //PostalCode = x.PostalCode,
                        GTIN = x.LocationNo1 + x.LocationNo2 + x.CheckDigit
                    }).FirstOrDefault(),
                    Material = new MaterialDTO
                    {
                        MaterialId = k.Material.MaterialId,
                        Name = k.Material.MaterialDescription,
                        PalletFactor = k.Material.PalletFactor,
                        GrossWeight = k.Material.GrossWeight,
                        NetWeight = k.Material.NetWeight,
                        MaterialSAPID = k.Material.MaterialSapid.TrimStart('0'),
                        MaterialDescription = k.Material.MaterialDescription,
                        PackagingLineDescription = k.Material.PackagingLineDescription,
                        GTIN = k.Material.Gtin
                    },
                    Quantity = k.Quantity,
                    DocumentDate = k.DocumentDate,
                    PurchaseOrderDate = k.PurchaseOrderDate,
                    RequestedDeliveryDate = k.RequestedDeliveryDate
                }).ToList();

                var palletDTOs = _context.Pallets.Include(k => k.Material).Where(k => selectedPallets.Contains(k.PalletId)).Select(k => new PalletDTO
                {
                    PalletId = k.PalletId,
                    PalletLOT = k.PalletLot,
                    SSCC = k.Sscc,
                    Material = new MaterialDTO
                    {
                        MaterialId = k.Material.MaterialId,
                        MaterialSAPID = k.Material.MaterialSapid.TrimStart('0'),
                        PalletFactor = k.Material.PalletFactor,
                        MaxDeliveryDate = GetMaxDeliveryDate(k.PalletCreationDateTime.Value, k.Material.LagerDays),
                        MaterialDescription = k.Material.MaterialDescription,
                        PackagingLineDescription = k.Material.PackagingLineDescription,
                        GTIN = k.Material.Gtin
                    }
                }).ToList();

                var LoadOrderPallets = new List<TruckOrderPallet>();
                var _palletDTOs = palletDTOs;
                foreach (var orderDTO in orderDTOs)
                {
                    List<PalletDTO> palletCurrentOrderDTOs = new List<PalletDTO>();
                    palletCurrentOrderDTOs = _palletDTOs.Where(k => k.Material.MaterialId == orderDTO.Material.MaterialId).Take(orderDTO.Quantity).ToList();

                    foreach (var palletCurrentOrderDTO in palletCurrentOrderDTOs)
                    {
                        LoadOrderPallets.Add(new TruckOrderPallet
                        {
                            OrderId = orderDTO.OrderId,
                            PalletId = palletCurrentOrderDTO.PalletId,
                        });
                    }
                    _palletDTOs = _palletDTOs.Where(k => !palletCurrentOrderDTOs.Select(l => l.PalletId).Contains(k.PalletId)).ToList();
                }


                //Create LIDL palletcard for all pallets (per pallet & all)
                var palletCardLidlDTOs = new List<PalletCardLidlDTO>();
                var LoadOrderPalletsGrouped = LoadOrderPallets.GroupBy(k => k.OrderId).Select(k => k.ToList()).ToList();
                foreach (var LoadOrderPalletsGroupedItem in LoadOrderPalletsGrouped)
                {
                    var orderDTO = orderDTOs.Where(k => k.OrderId == LoadOrderPalletsGroupedItem.FirstOrDefault().OrderId).FirstOrDefault();
                    var palletNumber = 0;
                    foreach (var pallet in LoadOrderPalletsGroupedItem)
                    {
                        //Generate pallet card LIDL
                        var palletDTO = palletDTOs.Where(k => k.PalletId == pallet.PalletId).FirstOrDefault();
                        palletNumber++;
                        palletCardLidlDTOs.Add(new PalletCardLidlDTO
                        {
                            Supplier = "ARI S.A.",
                            OrderNumber = orderDTO.ReferenceNumber,
                            Material = orderDTO.Material,
                            Pallet = palletDTO,
                            PalletNumber = palletNumber,
                            SumPallet = LoadOrderPalletsGroupedItem.Count,
                            ShipToParty = orderDTO.ShipToParty,
                            SoldToParty = orderDTO.SoldToParty,
                            BestBefore = palletDTO.Material.ExpirationDate,
                            DeliveryDate = palletDTO.Material.MaxDeliveryDate,
                            SSCC = palletDTO.SSCC,
                            Order = orderDTO,
                            DeliveryNoteNo = "DA A1 0081144"
                        });
                    }
                }

                //Per pallet
                string randomIndex = string.Empty;
                foreach (var palletCardLidlDTO in palletCardLidlDTOs)
                {
                    //Random name
                    randomIndex = RandomString(15);

                    //Generate pallet card Lidl
                    GeneratePalletCardLIDL(palletCardLidlDTO, randomIndex);

                    var pallet = _context.Pallets.Where(k => k.PalletId == palletCardLidlDTO.Pallet.PalletId).FirstOrDefault();
                    pallet.PalletCardLidl = System.IO.Path.Combine("https://arisa.blob.core.windows.net", AzureGPV2Credentials.CONTAINER_PDF_FILES, "pallet_card_lidl_" + randomIndex + ".pdf");
                    pallet.Status = 4;
                    _context.Update(pallet);
                }

                //All pallets
                randomIndex = RandomString(15);

                //Generate pallet card Lidl
                GeneratePalletCardsLIDL(palletCardLidlDTOs, randomIndex);
                orderAri.PalletCardsLidl = System.IO.Path.Combine("https://arisa.blob.core.windows.net", AzureGPV2Credentials.CONTAINER_PDF_FILES, "pallet_card_lidl_" + randomIndex + ".pdf");

                //Create driver documents - All
                await GenerateDriverInvoice(palletCardLidlDTOs, randomIndex);
                orderAri.DriverDocuments = System.IO.Path.Combine("https://arisa.blob.core.windows.net", AzureGPV2Credentials.CONTAINER_PDF_FILES, "LFG" + randomIndex + ".pdf");
                
                //Create CMR documents - All
                await GenerateDriverCMR(palletCardLidlDTOs, randomIndex, CMRType.Chiajna);
                orderAri.Cmrdocuments = System.IO.Path.Combine("https://arisa.blob.core.windows.net", AzureGPV2Credentials.CONTAINER_PDF_FILES, "CMR" + randomIndex + ".pdf");

                //Create LIDL Orders Documents - All
                await GenerateLIDLOrdersDocuments(palletCardLidlDTOs, randomIndex);
                orderAri.LidlorderHeaders = System.IO.Path.Combine("https://arisa.blob.core.windows.net", AzureGPV2Credentials.CONTAINER_LIDL_ORDERS, "ARI_DESADV_HEADER_" + randomIndex + ".txt");
                orderAri.LidlorderLines = System.IO.Path.Combine("https://arisa.blob.core.windows.net", AzureGPV2Credentials.CONTAINER_LIDL_ORDERS, "ARI_DESADV_LINES_" + randomIndex + ".txt");


            }
            else if (formStatus == 0 && orderAri.Status == 1)
            {
                var loadPallets = await _context.LoadPallets.Where(k => k.LoadId == orderAri.LoadId).ToListAsync();
                foreach (var loadPallet in loadPallets)
                {
                    var pallet = _context.Pallets.Where(k => k.PalletId == loadPallet.PalletId).FirstOrDefault();
                    pallet.Status = 3;
                    pallet.PalletCardLidl = null;
                    _context.Update(pallet);
                }
                var loadOrders = await _context.LoadOrders.Where(k => k.LoadId == orderAri.LoadId).ToListAsync();
                foreach (var loadOrder in loadOrders)
                {
                    var order = _context.Orders.Where(k => k.OrderId == loadOrder.OrderId).FirstOrDefault();
                    order.Status = 1;
                    _context.Update(order);
                }
                orderAri.PalletCardsLidl = null;
                orderAri.DriverDocuments = null;
                orderAri.Cmrdocuments = null;
                orderAri.LidlorderHeaders = null;
                orderAri.LidlorderLines = null;
            }
            else if (formStatus == -1)
            {
                var loadPallets = await _context.LoadPallets.Where(k => k.LoadId == orderAri.LoadId).ToListAsync();
                var loadOrders = await _context.LoadOrders.Where(k => k.LoadId == orderAri.LoadId).ToListAsync();
                foreach (var loadPallet in loadPallets)
                {
                    var pallet = _context.Pallets.Where(k => k.PalletId == loadPallet.PalletId).FirstOrDefault();
                    pallet.Status = 2;
                    pallet.PalletCardLidl = null;
                    _context.Update(pallet);
                }
                foreach (var loadOrder in loadOrders)
                {
                    var order = _context.Orders.Where(k => k.OrderId == loadOrder.OrderId).FirstOrDefault();
                    order.Status = 0;
                    _context.Update(order);
                }
                _context.LoadPallets.RemoveRange(loadPallets);
                _context.LoadOrders.RemoveRange(loadOrders);

                orderAri.PalletCardsLidl = null;
                orderAri.DriverDocuments = null;
                orderAri.Cmrdocuments = null;
                orderAri.LidlorderHeaders = null;
                orderAri.LidlorderLines = null;
            }
            if(formStatus == 1 && orderAri.IsMatch != 1)
                return RedirectToAction("Load", "Warehouse");
            orderAri.Status = formStatus;
            _context.Update(orderAri);
            await _context.SaveChangesAsync();
            return RedirectToAction("Load", "Warehouse");
        }

        public async Task<IActionResult> AddOrderToLoad(string passToken, int orderId)
        {
            LoadOrder loadOrder = await _context.LoadOrders.Where(k => k.LoadId == int.Parse(protector.Encode(passToken)) && k.OrderId == orderId).FirstOrDefaultAsync();
            if (loadOrder == null)
            {
                await _context.LoadOrders.AddAsync(new LoadOrder
                {
                    LoadId = int.Parse(protector.Encode(passToken)),
                    OrderId = orderId
                });
                var order = await _context.Orders.Where(k => k.OrderId == orderId).FirstOrDefaultAsync();
                order.Status = 1;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("EditLoadOrders", "Warehouse", new { passToken = passToken });
        }
        public async Task<IActionResult> RemoveOrderFromLoad(string passToken, int orderId)
        {
            LoadOrder loadOrder = await _context.LoadOrders.Where(k => k.LoadId == int.Parse(protector.Encode(passToken)) && k.OrderId == orderId).FirstOrDefaultAsync();
            if (loadOrder != null)
            {
                _context.Remove(loadOrder);
                var order = await _context.Orders.Where(k => k.OrderId == orderId).FirstOrDefaultAsync();
                order.Status = 0;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("EditLoadOrders", "Warehouse", new { passToken = passToken });
        }
        public async Task<IActionResult> ApplyFilters(string passToken, string text, string docFromDate, 
            string docUntilDate, string purFromDate, string purUntilDate, string reqFromDate, string reqUntilDate)
        {
            DateTime? documentFrom, documentUntil, purchaseFrom, purchaseUntil, requestFromDate, requestUntilDate;
            if (docFromDate != "-") documentFrom = Convert.ToDateTime(docFromDate);
            else documentFrom = null;
            if (docUntilDate != "-") documentUntil = Convert.ToDateTime(docUntilDate);
            else documentUntil = null;
            if (purFromDate != "-") purchaseFrom = Convert.ToDateTime(purFromDate);
            else purchaseFrom = null;
            if (purUntilDate != "-") purchaseUntil = Convert.ToDateTime(purUntilDate);
            else purchaseUntil = null;
            if (reqFromDate != "-") requestFromDate = Convert.ToDateTime(reqFromDate);
            else requestFromDate = null;
            if (reqUntilDate != "-") requestUntilDate = Convert.ToDateTime(reqUntilDate);
            else requestUntilDate = null;

            var user = _context.Users.Where(k => k.Email == User.Identity.Name).FirstOrDefault();
            if (_context.LoadOrderFilters.Where(k => k.UserId == user.UserId).Any())
            {
                var loadOrderFilter = _context.LoadOrderFilters.Where(k => k.UserId == user.UserId).FirstOrDefault();
                loadOrderFilter.Text = text != "-"? text : null;
                loadOrderFilter.DocumentFrom = documentFrom;
                loadOrderFilter.DocumentUntil = documentUntil;
                loadOrderFilter.PurchaseFrom = purchaseFrom;
                loadOrderFilter.PurchaseUntil = purchaseUntil;
                loadOrderFilter.RequestedFrom = requestFromDate;
                loadOrderFilter.RequestedUntil = requestUntilDate;
                _context.Update(loadOrderFilter);
            }
            else
            {
                await _context.AddAsync(new LoadOrderFilter 
                { 
                    UserId = user.UserId, 
                    Text = text != "-" ? text : null,
                    DocumentFrom = documentFrom,
                    DocumentUntil = documentUntil,
                    PurchaseFrom = purchaseFrom,
                    PurchaseUntil = purchaseUntil,
                    RequestedFrom = requestFromDate,
                    RequestedUntil = requestUntilDate
                });
            }
            await _context.SaveChangesAsync();
            return RedirectToAction("EditLoadOrders", "Warehouse", new { passToken = passToken });
        }

        public async Task<IActionResult> RemovePalletFromLoad(string passToken, int palletId)
        {
            LoadPallet loadPallet = await _context.LoadPallets.Where(k => k.LoadId == int.Parse(protector.Encode(passToken)) && k.PalletId == palletId).FirstOrDefaultAsync();
            var pallet = _context.Pallets.Where(k => k.PalletId == palletId).FirstOrDefault();
            pallet.Status = 2;
            _context.Update(pallet);
            if (loadPallet != null)
            {
                _context.Remove(loadPallet);               
            }
            await _context.SaveChangesAsync();

            return RedirectToAction("EditLoadPallets", "Warehouse", new { passToken = passToken });
        }


        //Helpers
        public async Task<List<EDIOrderDTO>> GetOrdersModelAsync()
        {
            return await _context.Orders.Include(k => k.Material).Where(k => k.Status !=-1 && k.ShipToParty != null && k.SoldToParty != null && k.Quantity != 0).Select(k => new EDIOrderDTO
            {
                OrderId = k.OrderId,
                PassToken = protector.Decode(k.OrderId.ToString()),
                ReferenceNumber = k.ReferenceNumber,
                Material = new MaterialDTO
                {
                    MaterialId = k.Material.MaterialId,
                    MaterialSAPID = k.Material.MaterialSapid.TrimStart('0'),
                    MaterialDescription = k.Material.MaterialDescription,
                    PackagingLineDescription = k.Material.PackagingLineDescription,
                    GTIN = k.Material.Gtin
                },
                ShipToParty = new CustomerDTO { Name1 = _context.Customers1.Where(x => x.CustomerId == k.ShipToParty).FirstOrDefault().Name1 },
                SoldToParty = new CustomerDTO { Name1 = _context.Customers1.Where(x => x.CustomerId == k.SoldToParty).FirstOrDefault().Name1 },
                DocumentDate = k.DocumentDate,
                PurchaseOrderDate = k.PurchaseOrderDate,
                RequestedDeliveryDate = k.RequestedDeliveryDate,
                Quantity = k.Quantity,
                SelectedStatus = Convert.ToString(k.Status)
            }).ToListAsync();
        }

        public async Task<List<PalletDTO>> GetPalletsModelAsync()
        {
            return await _context.Pallets.Include(k => k.PackagingLineFormPallets).ThenInclude(k => k.PackagingLineForm)
                .Include(k => k.Material).ThenInclude(k => k.Country).Where(k => k.PalletCreationDateTime != null).Select(k => new PalletDTO
                {
                    PalletId = k.PalletId,
                    PassToken = protector.Decode(k.PalletId.ToString()),
                    Material = new MaterialDTO
                    {
                        MaterialSAPID = k.Material.MaterialSapid.TrimStart('0'),
                        MaterialLOT = GetLot(k.PackagingLineFormPallets.FirstOrDefault().PackagingLineForm.FormCreationDate),
                        MaterialDescription = k.Material.MaterialDescription,
                        PackagingLineDescription = k.Material.PackagingLineDescription,
                        MaxDeliveryDate = GetMaxDeliveryDate(k.PackagingLineFormPallets.FirstOrDefault().PackagingLineForm.FormCreationDate, k.Material.LagerDays),
                        ExpirationDate = GetExpirationDate(k.PackagingLineFormPallets.FirstOrDefault().PackagingLineForm.FormCreationDate, k.Material.ExpirationDays)
                    },
                    CountryCode = k.Material.Country.Code,
                    PackagingLinePalletNo = k.PackagingLinePalletNo,
                    QRCodeLink = k.Qrcode,
                    BarCodeLink = k.BarCode,
                    PalletCardLink = k.PalletCard,
                    PalletCardLIDLLink = k.PalletCardLidl,
                    NoPalletBox = k.Material.PalletFactor,
                    StatusId = k.Status,
                    SelectedStatus = ((PalletStatus)k.Status).AsString(EnumFormat.Description),
                    PackagingLineId = k.PackagingLineFormPallets.FirstOrDefault().PackagingLineForm.PackagingLineFormId,
                    LoadingAreaID = 0,
                    LoadingLine = 0,
                    LoadingPosition = 0,
                    CreationDateTime = k.PalletCreationDateTime.Value.ToString("HH:mm")
                }).ToListAsync();
        }
        //???
        public List<GroupOrderDTO> AddGroupOrders(List<EDIOrderDTO> eDIOrderDTOs)
        {
            List<GroupOrderDTO> groupOrderDTOs = new List<GroupOrderDTO>();
            foreach (var orderItem in eDIOrderDTOs)
            {
                if (groupOrderDTOs.Select(k => k.MaterialSAPID.TrimStart('0')).Contains(orderItem.Material.MaterialSAPID.TrimStart('0')))
                {
                    groupOrderDTOs.Where(k => k.MaterialSAPID.TrimStart('0') == orderItem.Material.MaterialSAPID.TrimStart('0')).FirstOrDefault().Quantity = groupOrderDTOs.Where(k => k.MaterialSAPID.TrimStart('0') == orderItem.Material.MaterialSAPID.TrimStart('0')).FirstOrDefault().Quantity + orderItem.Quantity;
                }
                else
                {
                    groupOrderDTOs.Add(new GroupOrderDTO { MaterialSAPID = orderItem.Material.MaterialSAPID.TrimStart('0'), MaterialDescription = orderItem.Material.MaterialDescription, Quantity = orderItem.Quantity });
                }
            }

            return groupOrderDTOs;
        }

        //Picking pallets
        [HttpPost]
        public JsonResult GetFilterPallets(IFormCollection data)
        {
            DateTime pacFromDate = !String.IsNullOrWhiteSpace(data["pac-from-date"].ToString()) ? Convert.ToDateTime(data["pac-from-date"]) : new DateTime(1900, 1, 1);
            DateTime pacUntilDate = !String.IsNullOrWhiteSpace(data["pac-until-date"].ToString()) ? Convert.ToDateTime(data["pac-until-date"]) : new DateTime(1900, 1, 1);
            DateTime lagFromDate = !String.IsNullOrWhiteSpace(data["lag-from-date"].ToString()) ? Convert.ToDateTime(data["lag-from-date"]) : new DateTime(1900, 1, 1);
            DateTime lagUntilDate = !String.IsNullOrWhiteSpace(data["lag-until-date"].ToString()) ? Convert.ToDateTime(data["lag-until-date"]) : new DateTime(1900, 1, 1);
            DateTime expFromDate = !String.IsNullOrWhiteSpace(data["exp-from-date"].ToString()) ? Convert.ToDateTime(data["exp-from-date"]) : new DateTime(1900, 1, 1);
            DateTime expUntilDate = !String.IsNullOrWhiteSpace(data["exp-until-date"].ToString()) ? Convert.ToDateTime(data["exp-until-date"]) : new DateTime(1900, 1, 1);

            //All
            var warehousePalletIds = _context.Warehouses.Where(k => k.PalletId != null).Select(k => k.PalletId).ToList();
            var activePallets = _context.Pallets.Include(k => k.Material).Include(k => k.Country).Where(k => k.Status == 2 && warehousePalletIds.Contains(k.PalletId)).ToList();

            string search_text1 = data["search-text1"].ToString().Trim().ToLower();
            string search_text2 = data["search-text2"].ToString().Trim().ToLower();
            string search_text3 = data["search-text3"].ToString().Trim().ToLower();

            List<int> activeIds = new List<int>();

            List<int> search1Ids = new List<int>();
            if (search_text1 != string.Empty && search_text1.Length >= 2)
            {
                search1Ids = activePallets.Where(k => (k.Material.MaterialDescription.ToLower().Contains(search_text1) || k.Material.PackagingLineDescription.ToLower().Contains(search_text1)
                || k.PalletLot.ToString().Contains(search_text1) || k.Material.Gtin.Contains(search_text1) || k.Material.MaterialSapid.TrimStart('0').Contains(search_text1) || k.Country.Code.ToLower().Contains(search_text1)
                || k.Country.NameEn.ToLower().Contains(search_text1) || k.PalletId.ToString().Contains(search_text1))
                    && (pacFromDate.Date > DateTime.Now.AddYears(-20) ? (k.PalletCreationDateTime.HasValue ? k.PalletCreationDateTime.Value > pacFromDate : true) : true)
                    && (pacUntilDate.Date > DateTime.Now.AddYears(-20) ? (k.PalletCreationDateTime.HasValue ? k.PalletCreationDateTime.Value < pacUntilDate : true) : true)
                    && (lagFromDate.Date > DateTime.Now.AddYears(-20) ? ClassConverters.GetMaxDeliveryDate(k.PalletCreationDateTime, k.Material.LagerDays) > lagFromDate : true)
                    && (lagUntilDate.Date > DateTime.Now.AddYears(-20) ? ClassConverters.GetMaxDeliveryDate(k.PalletCreationDateTime, k.Material.LagerDays) < lagUntilDate : true)
                    && (expFromDate.Date > DateTime.Now.AddYears(-20) ? ClassConverters.GetExpirationDate(k.PalletCreationDateTime, k.Material.ExpirationDays) > expFromDate : true)
                    && (expUntilDate.Date > DateTime.Now.AddYears(-20) ? ClassConverters.GetExpirationDate(k.PalletCreationDateTime, k.Material.ExpirationDays) < expUntilDate : true)).Select(k => k.PalletId).ToList();
            }

            List<int> search2Ids = new List<int>();
            if (search_text2 != string.Empty && search_text2.Length >= 2)
            {
                search2Ids = activePallets.Where(k => (k.Material.MaterialDescription.ToLower().Contains(search_text2) || k.Material.PackagingLineDescription.ToLower().Contains(search_text2)
                    || k.PalletLot.ToString().Contains(search_text2) || k.Material.Gtin.Contains(search_text2) || k.Material.MaterialSapid.TrimStart('0').Contains(search_text2) || k.Country.Code.ToLower().Contains(search_text2)
                    || k.Country.NameEn.ToLower().Contains(search_text2) || k.PalletId.ToString().Contains(search_text2))
                    && (pacFromDate.Date > DateTime.Now.AddYears(-20) ? (k.PalletCreationDateTime.HasValue ? k.PalletCreationDateTime.Value > pacFromDate : true) : true)
                    && (pacUntilDate.Date > DateTime.Now.AddYears(-20) ? (k.PalletCreationDateTime.HasValue ? k.PalletCreationDateTime.Value < pacUntilDate : true) : true)
                    && (lagFromDate.Date > DateTime.Now.AddYears(-20) ? ClassConverters.GetMaxDeliveryDate(k.PalletCreationDateTime, k.Material.LagerDays) > lagFromDate : true)
                    && (lagUntilDate.Date > DateTime.Now.AddYears(-20) ? ClassConverters.GetMaxDeliveryDate(k.PalletCreationDateTime, k.Material.LagerDays) < lagUntilDate : true)
                    && (expFromDate.Date > DateTime.Now.AddYears(-20) ? ClassConverters.GetExpirationDate(k.PalletCreationDateTime, k.Material.ExpirationDays) > expFromDate : true)
                    && (expUntilDate.Date > DateTime.Now.AddYears(-20) ? ClassConverters.GetExpirationDate(k.PalletCreationDateTime, k.Material.ExpirationDays) < expUntilDate : true)).Select(k => k.PalletId).ToList();
            }

            List<int> search3Ids = new List<int>();
            if (search_text3 != string.Empty && search_text3.Length >= 2)
            {
                search3Ids = activePallets.Where(k => (k.Material.MaterialDescription.ToLower().Contains(search_text3) || k.Material.PackagingLineDescription.ToLower().Contains(search_text3)
                    || k.PalletLot.ToString().Contains(search_text3) || k.Material.Gtin.Contains(search_text3) || k.Material.MaterialSapid.TrimStart('0').Contains(search_text3) || k.Country.Code.ToLower().Contains(search_text3)
                    || k.Country.NameEn.ToLower().Contains(search_text3) || k.PalletId.ToString().Contains(search_text3))
                    && (pacFromDate.Date > DateTime.Now.AddYears(-20) ? (k.PalletCreationDateTime.HasValue ? k.PalletCreationDateTime.Value > pacFromDate : true) : true)
                    && (pacUntilDate.Date > DateTime.Now.AddYears(-20) ? (k.PalletCreationDateTime.HasValue ? k.PalletCreationDateTime.Value < pacUntilDate : true) : true)
                    && (lagFromDate.Date > DateTime.Now.AddYears(-20) ? ClassConverters.GetMaxDeliveryDate(k.PalletCreationDateTime, k.Material.LagerDays) > lagFromDate : true)
                    && (lagUntilDate.Date > DateTime.Now.AddYears(-20) ? ClassConverters.GetMaxDeliveryDate(k.PalletCreationDateTime, k.Material.LagerDays) < lagUntilDate : true)
                    && (expFromDate.Date > DateTime.Now.AddYears(-20) ? ClassConverters.GetExpirationDate(k.PalletCreationDateTime, k.Material.ExpirationDays) > expFromDate : true)
                    && (expUntilDate.Date > DateTime.Now.AddYears(-20) ? ClassConverters.GetExpirationDate(k.PalletCreationDateTime, k.Material.ExpirationDays) < expUntilDate : true)).Select(k => k.PalletId).ToList();
            }

            List<int> search4Ids = new List<int>();
            if (search_text1 == string.Empty && search_text2 == string.Empty && search_text3 == string.Empty)
            {
                search4Ids = activePallets.Where(k => (pacFromDate.Date > DateTime.Now.AddYears(-20) ? (k.PalletCreationDateTime.HasValue ? k.PalletCreationDateTime.Value > pacFromDate : true) : true)
                    && (pacUntilDate.Date > DateTime.Now.AddYears(-20) ? (k.PalletCreationDateTime.HasValue ? k.PalletCreationDateTime.Value < pacUntilDate : true) : true)
                    && (lagFromDate.Date > DateTime.Now.AddYears(-20) ? ClassConverters.GetMaxDeliveryDate(k.PalletCreationDateTime, k.Material.LagerDays) > lagFromDate : true)
                    && (lagUntilDate.Date > DateTime.Now.AddYears(-20) ? ClassConverters.GetMaxDeliveryDate(k.PalletCreationDateTime, k.Material.LagerDays) < lagUntilDate : true)
                    && (expFromDate.Date > DateTime.Now.AddYears(-20) ? ClassConverters.GetExpirationDate(k.PalletCreationDateTime, k.Material.ExpirationDays) > expFromDate : true)
                    && (expUntilDate.Date > DateTime.Now.AddYears(-20) ? ClassConverters.GetExpirationDate(k.PalletCreationDateTime, k.Material.ExpirationDays) < expUntilDate : true)).Select(k => k.PalletId).ToList();
            }


            if (search1Ids.Count() == 0 && search2Ids.Count != 0 && search3Ids.Count != 0)
                activeIds.AddRange(search2Ids.Intersect(search3Ids));
            else if(search1Ids.Count() != 0 && search2Ids.Count == 0 && search3Ids.Count != 0)
                activeIds.AddRange(search1Ids.Intersect(search3Ids));
            else if (search1Ids.Count() != 0 && search2Ids.Count != 0 && search3Ids.Count == 0)
                activeIds.AddRange(search1Ids.Intersect(search2Ids));
            else if (search1Ids.Count() != 0 && search2Ids.Count != 0 && search3Ids.Count != 0)
                activeIds.AddRange(search1Ids.Intersect(search2Ids).Intersect(search3Ids));
            else if (search1Ids.Count() == 0 && search2Ids.Count == 0 && search3Ids.Count == 0 && search4Ids.Count() != 0)
                activeIds.AddRange(search4Ids);
            else
            {
                activeIds.AddRange(search1Ids);
                activeIds.AddRange(search2Ids);
                activeIds.AddRange(search3Ids);
            }
            return Json(activeIds);
        }
        [HttpPost]
        public async Task<JsonResult> SavePallets(IFormCollection data)
        {
            string loadPassToken = data["load-pass-token"].ToString();
            string[] palletIdsToString = data["selected-ids"].ToString().Split(',');
            List<LoadPallet> loadPallets = new List<LoadPallet>();
            var pallets = _context.Pallets;
            foreach (var palletId in palletIdsToString)
            {
                loadPallets.Add(new LoadPallet { LoadId = int.Parse(protector.Encode(loadPassToken)), PalletId = int.Parse(palletId) });
                pallets.Where(k => k.PalletId == int.Parse(palletId)).FirstOrDefault().Status = 3;
            }
            _context.LoadPallets.AddRange(loadPallets);
            await _context.SaveChangesAsync();

            return Json("ok");
        }
        [HttpPost]
        public async Task<JsonResult> ClearPallets(IFormCollection data)
        {
            string loadPassToken = data["load-pass-token"].ToString();
            var loadPallets = await _context.LoadPallets.Where(k => k.LoadId == int.Parse(protector.Encode(loadPassToken))).ToListAsync();
            foreach (var loadPallet in loadPallets)
            {
                var pallet = _context.Pallets.Where(k => k.PalletId == loadPallet.PalletId).FirstOrDefault();
                pallet.Status = 2;
            }
            _context.LoadPallets.RemoveRange(loadPallets);
            await _context.SaveChangesAsync();

            return Json("ok");
        }




        //TOREMOVE
        [Authorize]
        public async Task<IActionResult> WareHousePicking(bool? succeededPastAction)
        {
            OrderARIDTO orderARIDto = new OrderARIDTO();

            var orderIdsToIgnore = _context.TruckOrderPallets.Select(k => k.OrderId).ToList();

            orderARIDto.EDIOrderDTOs = await _context.Orders.Include(k => k.Material).Where(k=>k.Status == 0 && k.ShipToParty != null && k.SoldToParty != null && k.Quantity != 0).Select(k => new EDIOrderDTO
            {
                OrderId = k.OrderId,
                PassToken = protector.Decode(k.OrderId.ToString()),
                ReferenceNumber = k.ReferenceNumber,
                Material = new MaterialDTO
                {
                    MaterialId = k.Material.MaterialId,
                    MaterialSAPID = k.Material.MaterialSapid.TrimStart('0'),
                    MaterialDescription = k.Material.MaterialDescription,
                    PackagingLineDescription = k.Material.PackagingLineDescription,
                    GTIN = k.Material.Gtin
                },
                ShipToParty = new CustomerDTO { Name1 = _context.Customers1.Where(x => x.CustomerId == k.ShipToParty).FirstOrDefault().Name1 },
                SoldToParty = new CustomerDTO { Name1 = _context.Customers1.Where(x => x.CustomerId == k.SoldToParty).FirstOrDefault().Name1 },
                DocumentDate = k.DocumentDate,
                PurchaseOrderDate = k.PurchaseOrderDate,
                RequestedDeliveryDate = k.RequestedDeliveryDate,
                Quantity = k.Quantity,
                SelectedStatus = Convert.ToString(k.Status)
            }).ToListAsync();

            orderARIDto.EDIOrderDTOs = orderARIDto.EDIOrderDTOs.Where(k => !orderIdsToIgnore.Contains(k.OrderId)).ToList();

            //?
            List<PalletDTO> palletDTOs = new List<PalletDTO>();
            var packagingLines = await _context.PackagingLineForms.Where(k => *//* k.Status == (int)PackagingLineStatus.LoadPallets || *//* k.Status != (int)PackagingLineStatus.Cancel).ToListAsync();
            foreach (var packagingLineItem in packagingLines)
            {
                var tempPalletDTOs = _context.PackagingLineFormPallets.Include(k => k.Pallet).Where(k => k.PackagingLineForm == packagingLineItem && k.Pallet.PalletCreationDateTime != null).Select(k => new PalletDTO
                {
                    PalletId = k.PalletId,
                    Material = _context.Materials1.Where(l => l.MaterialId == k.Pallet.MaterialId).Select(x => new MaterialDTO
                    {
                        MaterialSAPID = x.MaterialSapid.TrimStart('0'),
                        MaterialLOT = GetLot(packagingLineItem.FormCreationDate),
                        MaterialDescription = x.MaterialDescription,
                        PackagingLineDescription = x.PackagingLineDescription,
                        MaxDeliveryDate = GetMaxDeliveryDate(packagingLineItem.FormCreationDate, x.LagerDays),
                        ExpirationDate = GetExpirationDate(packagingLineItem.FormCreationDate, x.ExpirationDays)
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
            //?
            orderARIDto.WarehouseDTO = new WarehouseDTO { WarehouseAreaListItems = WarehouseAreaListItems, PositionListItems = positionListItems, PalletListItems = palletListItems, PalletDTOs = palletDTOs, SucceededPastAction = succeededPastAction == true ? true : false };
            
            return View(orderARIDto);
        }

        [Authorize]
        public IActionResult WarehouseSupplies()
        {
            List<WarehouseSuppliesDTO> warehouseSuppliesDTOs = new List<WarehouseSuppliesDTO>();

            var packagingLineForms = _context.PackagingLineForms.ToList();
            var pallets = _context.Pallets.Where(k => k.PalletCreationDateTime != null).ToList();
            var palletsByMatLotIds = pallets.GroupBy(k => new { k.MaterialId, k.PalletLot }).Select(group => group.First()).ToList();

            var materials = _context.Materials1.Include(k => k.Country).Where(x => pallets.Select(k => k.MaterialId).Contains(x.MaterialId));
            var warehouseSlots = _context.Warehouses.Where(k => k.PalletId != null).ToList();
            var countries = _context.Countries.ToList();

            foreach (var palletGroup in palletsByMatLotIds)
            {
                WarehouseSuppliesDTO warehouseSuppliesDTO = new WarehouseSuppliesDTO();
                var _material = materials.Where(k => k.MaterialId == palletGroup.MaterialId).FirstOrDefault();
                var creationDate = pallets.Where(k => k.MaterialId == palletGroup.MaterialId && k.PalletLot == palletGroup.PalletLot).FirstOrDefault().PalletCreationDateTime;
                warehouseSuppliesDTO.MaterialDTO = new MaterialDTO
                {
                    MaterialId = _material.MaterialId,
                    MaterialSAPID = _material.MaterialSapid.TrimStart('0'),
                    MaterialLOT = palletGroup.PalletLot,
                    MaterialDescription = _material.MaterialDescription,
                    PackagingLineDescription = _material.PackagingLineDescription,
                    MaxDeliveryDate = GetMaxDeliveryDate((DateTime)creationDate, _material.LagerDays),
                    ExpirationDate = GetExpirationDate((DateTime)creationDate, _material.ExpirationDays),
                    LanguageCode = countries.Where(k => k.CountryId == _material.CountryId).FirstOrDefault().Code
                };

                //anam
                var _pallets_1 = pallets.Where(k => k.MaterialId == warehouseSuppliesDTO.MaterialDTO.MaterialId && k.PalletLot == warehouseSuppliesDTO.MaterialDTO.MaterialLOT && k.Status == 1).ToList();
                //apoth
                var _pallets_2 = pallets.Where(k => k.MaterialId == warehouseSuppliesDTO.MaterialDTO.MaterialId && k.PalletLot == warehouseSuppliesDTO.MaterialDTO.MaterialLOT && k.Status == 2).ToList();
                //desm
                var _pallets_3 = pallets.Where(k => k.MaterialId == warehouseSuppliesDTO.MaterialDTO.MaterialId && k.PalletLot == warehouseSuppliesDTO.MaterialDTO.MaterialLOT && k.Status == 3).ToList();
                //epoik
                var _pallets_4 = pallets.Where(k => k.MaterialId == warehouseSuppliesDTO.MaterialDTO.MaterialId && k.PalletLot == warehouseSuppliesDTO.MaterialDTO.MaterialLOT && k.Status == 4).ToList();

                List<SuppliesMetricsDTO> suppliesMetricsDTOs = new List<SuppliesMetricsDTO>();
                var suppliesMetricsDTO_anam = new SuppliesMetricsDTO
                {
                    Status = 1,
                    Sum = _pallets_1.Count()
                };
                suppliesMetricsDTOs.Add(suppliesMetricsDTO_anam);

                var suppliesMetricsDTO_apoth = new SuppliesMetricsDTO
                {
                    Status = 2,
                    Sala1 = warehouseSlots.Where(k => k.LoadingAreaId == 1 && _pallets_2.Select(l => l.PalletId).ToList().Contains(k.PalletId.Value)).Count(),
                    Sala1Psigeio = warehouseSlots.Where(k => k.LoadingAreaId == 2 && _pallets_2.Select(l => l.PalletId).ToList().Contains(k.PalletId.Value)).Count(),
                    Proektasi = warehouseSlots.Where(k => k.LoadingAreaId == 3 && _pallets_2.Select(l => l.PalletId).ToList().Contains(k.PalletId.Value)).Count(),
                    Sala2Deksia = warehouseSlots.Where(k => k.LoadingAreaId == 4 && _pallets_2.Select(l => l.PalletId).ToList().Contains(k.PalletId.Value)).Count(),
                    Sala2Aristera = warehouseSlots.Where(k => k.LoadingAreaId == 5 && _pallets_2.Select(l => l.PalletId).ToList().Contains(k.PalletId.Value)).Count(),
                    Sala2Dwmatio = warehouseSlots.Where(k => k.LoadingAreaId == 6 && _pallets_2.Select(l => l.PalletId).ToList().Contains(k.PalletId.Value)).Count(),
                    Makio = warehouseSlots.Where(k => k.LoadingAreaId == 7 && _pallets_2.Select(l => l.PalletId).ToList().Contains(k.PalletId.Value)).Count(),
                };
                suppliesMetricsDTO_apoth.Sum = suppliesMetricsDTO_apoth.Sala1 + suppliesMetricsDTO_apoth.Sala1Psigeio + suppliesMetricsDTO_apoth.Proektasi + suppliesMetricsDTO_apoth.Sala2Deksia + suppliesMetricsDTO_apoth.Sala2Aristera
                    + suppliesMetricsDTO_apoth.Sala2Dwmatio + suppliesMetricsDTO_apoth.Makio;
                suppliesMetricsDTOs.Add(suppliesMetricsDTO_apoth);

                var suppliesMetricsDTO_desm = new SuppliesMetricsDTO
                {
                    Status = 3,
                    Sala1 = warehouseSlots.Where(k => k.LoadingAreaId == 1 && _pallets_3.Select(l => l.PalletId).ToList().Contains(k.PalletId.Value)).Count(),
                    Sala1Psigeio = warehouseSlots.Where(k => k.LoadingAreaId == 2 && _pallets_3.Select(l => l.PalletId).ToList().Contains(k.PalletId.Value)).Count(),
                    Proektasi = warehouseSlots.Where(k => k.LoadingAreaId == 3 && _pallets_3.Select(l => l.PalletId).ToList().Contains(k.PalletId.Value)).Count(),
                    Sala2Deksia = warehouseSlots.Where(k => k.LoadingAreaId == 4 && _pallets_3.Select(l => l.PalletId).ToList().Contains(k.PalletId.Value)).Count(),
                    Sala2Aristera = warehouseSlots.Where(k => k.LoadingAreaId == 5 && _pallets_3.Select(l => l.PalletId).ToList().Contains(k.PalletId.Value)).Count(),
                    Sala2Dwmatio = warehouseSlots.Where(k => k.LoadingAreaId == 6 && _pallets_3.Select(l => l.PalletId).ToList().Contains(k.PalletId.Value)).Count(),
                    Makio = warehouseSlots.Where(k => k.LoadingAreaId == 7 && _pallets_3.Select(l => l.PalletId).ToList().Contains(k.PalletId.Value)).Count(),
                };
                suppliesMetricsDTO_desm.Sum = suppliesMetricsDTO_desm.Sala1 + suppliesMetricsDTO_desm.Sala1Psigeio + suppliesMetricsDTO_desm.Proektasi + suppliesMetricsDTO_desm.Sala2Deksia + suppliesMetricsDTO_desm.Sala2Aristera
                    + suppliesMetricsDTO_desm.Sala2Dwmatio + suppliesMetricsDTO_desm.Makio;
                suppliesMetricsDTOs.Add(suppliesMetricsDTO_desm);

                var suppliesMetricsDTO_epoik = new SuppliesMetricsDTO
                {
                    Status = 4,
                    Sala1 = warehouseSlots.Where(k => k.LoadingAreaId == 1 && _pallets_4.Select(l => l.PalletId).ToList().Contains(k.PalletId.Value)).Count(),
                    Sala1Psigeio = warehouseSlots.Where(k => k.LoadingAreaId == 2 && _pallets_4.Select(l => l.PalletId).ToList().Contains(k.PalletId.Value)).Count(),
                    Proektasi = warehouseSlots.Where(k => k.LoadingAreaId == 3 && _pallets_4.Select(l => l.PalletId).ToList().Contains(k.PalletId.Value)).Count(),
                    Sala2Deksia = warehouseSlots.Where(k => k.LoadingAreaId == 4 && _pallets_4.Select(l => l.PalletId).ToList().Contains(k.PalletId.Value)).Count(),
                    Sala2Aristera = warehouseSlots.Where(k => k.LoadingAreaId == 5 && _pallets_4.Select(l => l.PalletId).ToList().Contains(k.PalletId.Value)).Count(),
                    Sala2Dwmatio = warehouseSlots.Where(k => k.LoadingAreaId == 6 && _pallets_4.Select(l => l.PalletId).ToList().Contains(k.PalletId.Value)).Count(),
                    Makio = warehouseSlots.Where(k => k.LoadingAreaId == 7 && _pallets_4.Select(l => l.PalletId).ToList().Contains(k.PalletId.Value)).Count(),
                };
                suppliesMetricsDTO_epoik.Sum = suppliesMetricsDTO_epoik.Sala1 + suppliesMetricsDTO_epoik.Sala1Psigeio + suppliesMetricsDTO_epoik.Proektasi + suppliesMetricsDTO_epoik.Sala2Deksia + suppliesMetricsDTO_epoik.Sala2Aristera
                    + suppliesMetricsDTO_epoik.Sala2Dwmatio + suppliesMetricsDTO_epoik.Makio;
                suppliesMetricsDTOs.Add(suppliesMetricsDTO_epoik);

                warehouseSuppliesDTO.suppliesMetricsDTOs = suppliesMetricsDTOs;

                warehouseSuppliesDTOs.Add(warehouseSuppliesDTO);
            }


            return View(warehouseSuppliesDTOs);
        }

        [Authorize]
        public IActionResult WarehouseHistory()
        {
            var truckDTOs = _context.Trucks.Include(k => k.TruckOrderPallets).Where(k => k.IsAuthorized == 2).Select(k => new TruckDTO
            {
                TruckId = k.TruckId,
                PassToken = protector.Decode(k.TruckId.ToString()),
                CreationDateTime = k.CreationDateTime,
                IsAuthorized = k.IsAuthorized,
                NoOrders = k.TruckOrderPallets.Where(x => x.TruckId == k.TruckId).Select(k => k.OrderId).GroupBy(k => k.ToString()).Count(),
                NoPallets = k.TruckOrderPallets.Where(x => x.TruckId == k.TruckId).Select(k => k.PalletId).GroupBy(k => k.ToString()).Count(),
                PalletCardLIDL = k.PalletCardLidl,
                DriverDocuments = k.DriverDocuments,
                CMRDocuments = k.Cmrdocuments,
                LIDLOrdersHeaders = k.LidlordersHeaders,
                LIDLOrdersLines = k.LidlordersLines,
                CompletionDateTime = k.CompletionDateTime
            }).ToList();

            return View(truckDTOs);
        }

        public IActionResult DownloadWarehouseData()
        {
            try
            {
                List<WarehouseSuppliesDTO> warehouseSuppliesDTOs = new List<WarehouseSuppliesDTO>();

                var packagingLineForms = _context.PackagingLineForms.ToList();
                var pallets = _context.Pallets.Where(k => k.PalletCreationDateTime != null).ToList();
                var palletsByMatLotIds = pallets.GroupBy(k => new { k.MaterialId, k.PalletLot }).Select(group => group.First()).ToList();

                var materials = _context.Materials1.Include(k => k.Country).Where(x => pallets.Select(k => k.MaterialId).Contains(x.MaterialId));
                var warehouseSlots = _context.Warehouses.Where(k => k.PalletId != null).ToList();
                var countries = _context.Countries.ToList();

                foreach (var palletGroup in palletsByMatLotIds)
                {
                    WarehouseSuppliesDTO warehouseSuppliesDTO = new WarehouseSuppliesDTO();
                    var _material = materials.Where(k => k.MaterialId == palletGroup.MaterialId).FirstOrDefault();
                    var creationDate = packagingLineForms.Where(k => k.MaterialId == palletGroup.MaterialId && k.PalletsLot == palletGroup.PalletLot).FirstOrDefault().FormCreationDate;
                    warehouseSuppliesDTO.MaterialDTO = new MaterialDTO
                    {
                        MaterialId = _material.MaterialId,
                        MaterialSAPID = _material.MaterialSapid.TrimStart('0'),
                        MaterialLOT = GetLot(creationDate),
                        MaterialDescription = _material.MaterialDescription,
                        PackagingLineDescription = _material.PackagingLineDescription,
                        MaxDeliveryDate = GetMaxDeliveryDate(creationDate, _material.LagerDays),
                        ExpirationDate = GetExpirationDate(creationDate, _material.ExpirationDays),
                        LanguageCode = countries.Where(k => k.CountryId == _material.CountryId).FirstOrDefault().Code
                    };

                    //anam
                    var _pallets_1 = pallets.Where(k => k.MaterialId == warehouseSuppliesDTO.MaterialDTO.MaterialId && k.PalletLot == warehouseSuppliesDTO.MaterialDTO.MaterialLOT && k.Status == 1).ToList();
                    //apoth
                    var _pallets_2 = pallets.Where(k => k.MaterialId == warehouseSuppliesDTO.MaterialDTO.MaterialId && k.PalletLot == warehouseSuppliesDTO.MaterialDTO.MaterialLOT && k.Status == 2).ToList();
                    //desm
                    var _pallets_3 = pallets.Where(k => k.MaterialId == warehouseSuppliesDTO.MaterialDTO.MaterialId && k.PalletLot == warehouseSuppliesDTO.MaterialDTO.MaterialLOT && k.Status == 3).ToList();
                    //epoik
                    var _pallets_4 = pallets.Where(k => k.MaterialId == warehouseSuppliesDTO.MaterialDTO.MaterialId && k.PalletLot == warehouseSuppliesDTO.MaterialDTO.MaterialLOT && k.Status == 4).ToList();

                    List<SuppliesMetricsDTO> suppliesMetricsDTOs = new List<SuppliesMetricsDTO>();
                    var suppliesMetricsDTO_anam = new SuppliesMetricsDTO
                    {
                        Status = 1,
                        Sum = _pallets_1.Count()
                    };
                    suppliesMetricsDTOs.Add(suppliesMetricsDTO_anam);

                    var suppliesMetricsDTO_apoth = new SuppliesMetricsDTO
                    {
                        Status = 2,
                        Sala1 = warehouseSlots.Where(k => k.LoadingAreaId == 1 && _pallets_2.Select(l => l.PalletId).ToList().Contains(k.PalletId.Value)).Count(),
                        Sala1Psigeio = warehouseSlots.Where(k => k.LoadingAreaId == 2 && _pallets_2.Select(l => l.PalletId).ToList().Contains(k.PalletId.Value)).Count(),
                        Proektasi = warehouseSlots.Where(k => k.LoadingAreaId == 3 && _pallets_2.Select(l => l.PalletId).ToList().Contains(k.PalletId.Value)).Count(),
                        Sala2Deksia = warehouseSlots.Where(k => k.LoadingAreaId == 4 && _pallets_2.Select(l => l.PalletId).ToList().Contains(k.PalletId.Value)).Count(),
                        Sala2Aristera = warehouseSlots.Where(k => k.LoadingAreaId == 5 && _pallets_2.Select(l => l.PalletId).ToList().Contains(k.PalletId.Value)).Count(),
                        Sala2Dwmatio = warehouseSlots.Where(k => k.LoadingAreaId == 6 && _pallets_2.Select(l => l.PalletId).ToList().Contains(k.PalletId.Value)).Count(),
                        Makio = warehouseSlots.Where(k => k.LoadingAreaId == 7 && _pallets_2.Select(l => l.PalletId).ToList().Contains(k.PalletId.Value)).Count(),
                    };
                    suppliesMetricsDTO_apoth.Sum = suppliesMetricsDTO_apoth.Sala1 + suppliesMetricsDTO_apoth.Sala1Psigeio + suppliesMetricsDTO_apoth.Proektasi + suppliesMetricsDTO_apoth.Sala2Deksia + suppliesMetricsDTO_apoth.Sala2Aristera
                        + suppliesMetricsDTO_apoth.Sala2Dwmatio + suppliesMetricsDTO_apoth.Makio;
                    suppliesMetricsDTOs.Add(suppliesMetricsDTO_apoth);

                    var suppliesMetricsDTO_desm = new SuppliesMetricsDTO
                    {
                        Status = 3,
                        Sala1 = warehouseSlots.Where(k => k.LoadingAreaId == 1 && _pallets_3.Select(l => l.PalletId).ToList().Contains(k.PalletId.Value)).Count(),
                        Sala1Psigeio = warehouseSlots.Where(k => k.LoadingAreaId == 2 && _pallets_3.Select(l => l.PalletId).ToList().Contains(k.PalletId.Value)).Count(),
                        Proektasi = warehouseSlots.Where(k => k.LoadingAreaId == 3 && _pallets_3.Select(l => l.PalletId).ToList().Contains(k.PalletId.Value)).Count(),
                        Sala2Deksia = warehouseSlots.Where(k => k.LoadingAreaId == 4 && _pallets_3.Select(l => l.PalletId).ToList().Contains(k.PalletId.Value)).Count(),
                        Sala2Aristera = warehouseSlots.Where(k => k.LoadingAreaId == 5 && _pallets_3.Select(l => l.PalletId).ToList().Contains(k.PalletId.Value)).Count(),
                        Sala2Dwmatio = warehouseSlots.Where(k => k.LoadingAreaId == 6 && _pallets_3.Select(l => l.PalletId).ToList().Contains(k.PalletId.Value)).Count(),
                        Makio = warehouseSlots.Where(k => k.LoadingAreaId == 7 && _pallets_3.Select(l => l.PalletId).ToList().Contains(k.PalletId.Value)).Count(),
                    };
                    suppliesMetricsDTO_desm.Sum = suppliesMetricsDTO_desm.Sala1 + suppliesMetricsDTO_desm.Sala1Psigeio + suppliesMetricsDTO_desm.Proektasi + suppliesMetricsDTO_desm.Sala2Deksia + suppliesMetricsDTO_desm.Sala2Aristera
                        + suppliesMetricsDTO_desm.Sala2Dwmatio + suppliesMetricsDTO_desm.Makio;
                    suppliesMetricsDTOs.Add(suppliesMetricsDTO_desm);

                    var suppliesMetricsDTO_epoik = new SuppliesMetricsDTO
                    {
                        Status = 4,
                        Sala1 = warehouseSlots.Where(k => k.LoadingAreaId == 1 && _pallets_4.Select(l => l.PalletId).ToList().Contains(k.PalletId.Value)).Count(),
                        Sala1Psigeio = warehouseSlots.Where(k => k.LoadingAreaId == 2 && _pallets_4.Select(l => l.PalletId).ToList().Contains(k.PalletId.Value)).Count(),
                        Proektasi = warehouseSlots.Where(k => k.LoadingAreaId == 3 && _pallets_4.Select(l => l.PalletId).ToList().Contains(k.PalletId.Value)).Count(),
                        Sala2Deksia = warehouseSlots.Where(k => k.LoadingAreaId == 4 && _pallets_4.Select(l => l.PalletId).ToList().Contains(k.PalletId.Value)).Count(),
                        Sala2Aristera = warehouseSlots.Where(k => k.LoadingAreaId == 5 && _pallets_4.Select(l => l.PalletId).ToList().Contains(k.PalletId.Value)).Count(),
                        Sala2Dwmatio = warehouseSlots.Where(k => k.LoadingAreaId == 6 && _pallets_4.Select(l => l.PalletId).ToList().Contains(k.PalletId.Value)).Count(),
                        Makio = warehouseSlots.Where(k => k.LoadingAreaId == 7 && _pallets_4.Select(l => l.PalletId).ToList().Contains(k.PalletId.Value)).Count(),
                    };
                    suppliesMetricsDTO_epoik.Sum = suppliesMetricsDTO_epoik.Sala1 + suppliesMetricsDTO_epoik.Sala1Psigeio + suppliesMetricsDTO_epoik.Proektasi + suppliesMetricsDTO_epoik.Sala2Deksia + suppliesMetricsDTO_epoik.Sala2Aristera
                        + suppliesMetricsDTO_epoik.Sala2Dwmatio + suppliesMetricsDTO_epoik.Makio;
                    suppliesMetricsDTOs.Add(suppliesMetricsDTO_epoik);

                    warehouseSuppliesDTO.suppliesMetricsDTOs = suppliesMetricsDTOs;

                    warehouseSuppliesDTOs.Add(warehouseSuppliesDTO);
                }


                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine("Sap code, Material, LOT, Family, Seasonality, Country, Άθροισμα, Μεγ.ημ.παράδ., Ημ.λήξης");
                foreach (var item in warehouseSuppliesDTOs.Where(k => k.suppliesMetricsDTOs[0].Sum != 0))
                {
                    stringBuilder.AppendLine(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}", item.MaterialDTO.MaterialSAPID.TrimStart('0'), item.MaterialDTO.MaterialDescription, item.MaterialDTO.MaterialLOT,
                        item.MaterialDTO.ProductFamilyName, item.MaterialDTO.Seasonality, item.MaterialDTO.LanguageCode, item.suppliesMetricsDTOs[0].Sum, item.MaterialDTO.MaxDeliveryDate.ToString("dd/MM/yyyy"),
                        item.MaterialDTO.ExpirationDate.ToString("dd/MM/yyyy")));
                }
                stringBuilder.AppendLine(Environment.NewLine);

                stringBuilder.AppendLine("Sap code, Material, LOT, Family, Seasonality, Country, ΣΑΛΑ 1, ΣΑΛΑ 1 ΨΥΓΕΙΟ, ΠΡΟΕΚΤΑΣΗ, ΣΑΛΑ 2 ΔΕΞΙΑ, ΣΑΛΑ 2 ΑΡΙΣΤΕΡΑ, ΣΑΛΑ 2 ΔΩΜΑΤΙΟ, ΜΑΚΙΟ, Άθροισμα, Μεγ.ημ.παράδ., Ημ.λήξης");
                foreach (var item in warehouseSuppliesDTOs.Where(k => k.suppliesMetricsDTOs[1].Sum != 0))
                {
                    stringBuilder.AppendLine(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15}", item.MaterialDTO.MaterialSAPID.TrimStart('0'), item.MaterialDTO.MaterialDescription, item.MaterialDTO.MaterialLOT,
                        item.MaterialDTO.ProductFamilyName, item.MaterialDTO.Seasonality, item.MaterialDTO.LanguageCode, item.suppliesMetricsDTOs[1].Sala1, item.suppliesMetricsDTOs[1].Sala1Psigeio, item.suppliesMetricsDTOs[1].Proektasi,
                        item.suppliesMetricsDTOs[1].Sala2Deksia, item.suppliesMetricsDTOs[1].Sala2Aristera, item.suppliesMetricsDTOs[1].Sala2Dwmatio, item.suppliesMetricsDTOs[1].Makio, item.suppliesMetricsDTOs[0].Sum,
                        item.MaterialDTO.MaxDeliveryDate.ToString("dd/MM/yyyy"),
                        item.MaterialDTO.ExpirationDate.ToString("dd/MM/yyyy")));
                }
                stringBuilder.AppendLine(Environment.NewLine);

                stringBuilder.AppendLine("Sap code, Material, LOT, Family, Seasonality, Country, ΣΑΛΑ 1, ΣΑΛΑ 1 ΨΥΓΕΙΟ, ΠΡΟΕΚΤΑΣΗ, ΣΑΛΑ 2 ΔΕΞΙΑ, ΣΑΛΑ 2 ΑΡΙΣΤΕΡΑ, ΣΑΛΑ 2 ΔΩΜΑΤΙΟ, ΜΑΚΙΟ, Άθροισμα, Μεγ.ημ.παράδ., Ημ.λήξης");
                foreach (var item in warehouseSuppliesDTOs.Where(k => k.suppliesMetricsDTOs[2].Sum != 0))
                {
                    stringBuilder.AppendLine(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15}", item.MaterialDTO.MaterialSAPID.TrimStart('0'), item.MaterialDTO.MaterialDescription, item.MaterialDTO.MaterialLOT,
                        item.MaterialDTO.ProductFamilyName, item.MaterialDTO.Seasonality, item.MaterialDTO.LanguageCode, item.suppliesMetricsDTOs[2].Sala1, item.suppliesMetricsDTOs[2].Sala1Psigeio, item.suppliesMetricsDTOs[2].Proektasi,
                        item.suppliesMetricsDTOs[2].Sala2Deksia, item.suppliesMetricsDTOs[2].Sala2Aristera, item.suppliesMetricsDTOs[2].Sala2Dwmatio, item.suppliesMetricsDTOs[2].Makio, item.suppliesMetricsDTOs[2].Sum,
                        item.MaterialDTO.MaxDeliveryDate.ToString("dd/MM/yyyy"),
                        item.MaterialDTO.ExpirationDate.ToString("dd/MM/yyyy")));
                }
                stringBuilder.AppendLine(Environment.NewLine);

                stringBuilder.AppendLine("Sap code, Material, LOT, Family, Seasonality, Country, ΣΑΛΑ 1, ΣΑΛΑ 1 ΨΥΓΕΙΟ, ΠΡΟΕΚΤΑΣΗ, ΣΑΛΑ 2 ΔΕΞΙΑ, ΣΑΛΑ 2 ΑΡΙΣΤΕΡΑ, ΣΑΛΑ 2 ΔΩΜΑΤΙΟ, ΜΑΚΙΟ, Άθροισμα, Μεγ.ημ.παράδ., Ημ.λήξης");
                foreach (var item in warehouseSuppliesDTOs.Where(k => k.suppliesMetricsDTOs[3].Sum != 0))
                {
                    stringBuilder.AppendLine(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15}", item.MaterialDTO.MaterialSAPID.TrimStart('0'), item.MaterialDTO.MaterialDescription, item.MaterialDTO.MaterialLOT,
                        item.MaterialDTO.ProductFamilyName, item.MaterialDTO.Seasonality, item.MaterialDTO.LanguageCode, item.suppliesMetricsDTOs[3].Sala1, item.suppliesMetricsDTOs[3].Sala1Psigeio, item.suppliesMetricsDTOs[3].Proektasi,
                        item.suppliesMetricsDTOs[3].Sala2Deksia, item.suppliesMetricsDTOs[3].Sala2Aristera, item.suppliesMetricsDTOs[3].Sala2Dwmatio, item.suppliesMetricsDTOs[3].Makio, item.suppliesMetricsDTOs[3].Sum,
                        item.MaterialDTO.MaxDeliveryDate.ToString("dd/MM/yyyy"),
                        item.MaterialDTO.ExpirationDate.ToString("dd/MM/yyyy")));
                }

                var data = Encoding.UTF8.GetBytes(stringBuilder.ToString());
                var result = Encoding.UTF8.GetPreamble().Concat(data).ToArray();
                return File(result, "text/csv", string.Format("{0}_report_{1}.csv", "ari_warehouseSupplies", DateTimeOffset.Now.ToUnixTimeSeconds()));
                //return View(warehouseSuppliesDTOs);
            }
            catch (Exception ex)
            {
                return RedirectToAction("ExportPalletData", "PackagingLine");
            }
        }


        public IActionResult Pallet()
        {
            return View();
        }

        [Authorize]
        public async Task<IActionResult> TruckOrders(bool? succeededPastAction)
        {
            List<PalletDTO> palletDTOs = new List<PalletDTO>();

            var packagingLines = await _context.PackagingLineForms.Where(k => *//* k.Status == (int)PackagingLineStatus.LoadPallets || *//* k.Status == (int)PackagingLineStatus.Completed).ToListAsync();
            foreach (var packagingLineItem in packagingLines)
            {
                var tempPalletDTOs = _context.PackagingLineFormPallets.Include(k => k.Pallet).Where(k => k.PackagingLineForm == packagingLineItem && k.Pallet.PalletCreationDateTime != null).Select(k => new PalletDTO
                {
                    PalletId = k.PalletId,
                    Material = _context.Materials1.Where(l => l.MaterialId == k.Pallet.MaterialId).Select(x => new MaterialDTO
                    {
                        MaterialSAPID = x.MaterialSapid.TrimStart('0'),
                        MaterialLOT = GetLot(packagingLineItem.FormCreationDate),
                        MaterialDescription = x.MaterialDescription,
                        PackagingLineDescription = x.PackagingLineDescription,
                        MaxDeliveryDate = GetMaxDeliveryDate(packagingLineItem.FormCreationDate, x.LagerDays),
                        ExpirationDate = GetExpirationDate(packagingLineItem.FormCreationDate, x.ExpirationDays)
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

            var orderListItems = new List<SelectListItem>();
            orderListItems.Add(new SelectListItem { Text = "-", Value = "-" });

            var truckIds = _context.Trucks.Select(k => k.TruckId).ToList();
            for (int i = 0; i < truckIds.Count; i++)
                orderListItems.Add(new SelectListItem { Text = "ΦΠ" + truckIds[i], Value = truckIds[i].ToString() });

            for (int i = 1; i < 9; i++)
                WarehouseAreaListItems.Add(new SelectListItem { Text = ((Common.WarehouseArea)i).AsString(EnumFormat.Description), Value = i.ToString() });

            return View(new WarehouseDTO { WarehouseAreaListItems = WarehouseAreaListItems, PositionListItems = positionListItems, PalletListItems = palletListItems, PalletDTOs = palletDTOs, OrderListItems = orderListItems, SucceededPastAction = succeededPastAction == true ? true : false });
        }

        [Authorize]
        public async Task<IActionResult> TruckScanning(bool? succeededPastAction)
        {
            List<PalletDTO> palletDTOs = new List<PalletDTO>();

            var packagingLines = await _context.PackagingLineForms.Where(k => *//* k.Status == (int)PackagingLineStatus.LoadPallets || *//* k.Status == (int)PackagingLineStatus.Completed).ToListAsync();
            foreach (var packagingLineItem in packagingLines)
            {
                var tempPalletDTOs = _context.PackagingLineFormPallets.Include(k => k.Pallet).Where(k => k.PackagingLineForm == packagingLineItem && k.Pallet.PalletCreationDateTime != null).Select(k => new PalletDTO
                {
                    PalletId = k.PalletId,
                    Material = _context.Materials1.Where(l => l.MaterialId == k.Pallet.MaterialId).Select(x => new MaterialDTO
                    {
                        MaterialSAPID = x.MaterialSapid.TrimStart('0'),
                        MaterialLOT = GetLot(packagingLineItem.FormCreationDate),
                        MaterialDescription = x.MaterialDescription,
                        PackagingLineDescription = x.PackagingLineDescription,
                        MaxDeliveryDate = GetMaxDeliveryDate(packagingLineItem.FormCreationDate, x.LagerDays),
                        ExpirationDate = GetExpirationDate(packagingLineItem.FormCreationDate, x.ExpirationDays)
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
            //foreach (var warehousePalletItem in warehousePallets)
            //{
            //    palletDTOs.Where(k => k.PalletId == warehousePalletItem.PalletId).FirstOrDefault().LoadingAreaID = warehousePalletItem.LoadingAreaId;
            //    palletDTOs.Where(k => k.PalletId == warehousePalletItem.PalletId).FirstOrDefault().LoadingLine = warehousePalletItem.LoadingLine;
            //    palletDTOs.Where(k => k.PalletId == warehousePalletItem.PalletId).FirstOrDefault().LoadingPosition = warehousePalletItem.LoadingPosition;
            //}

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

            var orderListItems = new List<SelectListItem>();
            orderListItems.Add(new SelectListItem { Text = "-", Value = "-" });

            var truckIds = _context.Trucks.Where(k=> k.IsAuthorized == 1).Select(k => k.TruckId).ToList();
            for (int i = 0; i < truckIds.Count; i++)
                orderListItems.Add(new SelectListItem { Text = "ΦΠ" + truckIds[i], Value = truckIds[i].ToString() });

            for (int i = 1; i < 9; i++)
                WarehouseAreaListItems.Add(new SelectListItem { Text = ((Common.WarehouseArea)i).AsString(EnumFormat.Description), Value = i.ToString() });

            return View(new WarehouseDTO { WarehouseAreaListItems = WarehouseAreaListItems, PositionListItems = positionListItems, PalletListItems = palletListItems, PalletDTOs = palletDTOs, OrderListItems = orderListItems, SucceededPastAction = succeededPastAction == true ? true : false });
        }

        [HttpGet]
        public JsonResult GetScannedBarcodeData(string truckId, string barcode)
        {
            if (barcode.Length > 20)
            {
                if(!barcode.Contains("(00)"))
                    return Json("no valid");

                var sscc = barcode.Split(new string[] { "(00)" }, StringSplitOptions.None)[1];
                var pallet = _context.Pallets.Where(k => k.Sscc == sscc).FirstOrDefault();

                if (pallet == null)
                    return Json("no valid");

                if(_context.TruckOrderPallets.Where(k => k.PalletId == pallet.PalletId && k.TruckId == int.Parse(truckId)).Count() == 0)
                    return Json("no valid");

                PalletDTO palletDTO = new PalletDTO
                {
                    PalletId = pallet.PalletId,
                    PalletLOT = pallet.PalletLot,
                    Material = _context.Materials1.Where(l => l.MaterialId == pallet.MaterialId).Select(x => new MaterialDTO
                    {
                        MaterialSAPID = x.MaterialSapid.TrimStart('0'),
                        MaterialDescription = x.MaterialDescription,
                        PackagingLineDescription = x.PackagingLineDescription,
                    }).FirstOrDefault(),
                };

                return Json(palletDTO);
            }
            else
            {
                if (!barcode.Contains("(00)") || barcode.Length <= 5)
                    return Json("no valid");

                barcode = barcode.Split(')')[1];

                if (!int.TryParse(barcode, out int result))
                    return Json("no valid");

                var pallet = _context.Pallets.Where(k => k.PalletId == int.Parse(barcode)).FirstOrDefault();

                if (_context.TruckOrderPallets.Where(k => k.PalletId == pallet.PalletId && k.TruckId == int.Parse(truckId)).Count() == 0)
                    return Json("no valid");

                PalletDTO palletDTO = new PalletDTO
                {
                    PalletId = pallet.PalletId,
                    PalletLOT = pallet.PalletLot,
                    Material = _context.Materials1.Where(l => l.MaterialId == pallet.MaterialId).Select(x => new MaterialDTO
                    {
                        MaterialSAPID = x.MaterialSapid.TrimStart('0'),
                        MaterialDescription = x.MaterialDescription,
                        PackagingLineDescription = x.PackagingLineDescription,
                    }).FirstOrDefault(),
                };

                return Json(palletDTO);
            }
        }

        [HttpPost]
        public JsonResult FinalizeTruck(IFormCollection data)
        {
            int truckId = int.Parse(data["truck-id"]);

            var truck = _context.Trucks.Where(k => k.TruckId == truckId).FirstOrDefault();
            truck.IsAuthorized = 2;
            truck.CompletionDateTime = DateTime.Now;
            _context.Trucks.Update(truck);

            var palletIds = _context.TruckOrderPallets.Where(k => k.TruckId == truckId).Select(k => k.PalletId).ToList();
            var pallets = _context.Pallets.Where(k => palletIds.Contains(k.PalletId)).ToList();
            foreach (var pallet in pallets)
                pallet.Status = 5;
            _context.Pallets.UpdateRange(pallets);

            var warehouses = _context.Warehouses.Where(k => palletIds.Contains(k.PalletId.Value)).ToList();
            foreach (var warehouse in warehouses)
            {
                warehouse.PalletId = null;
                warehouse.ModificationDateTime = null;
            }
            _context.Warehouses.UpdateRange(warehouses);

            //_context.SaveChanges();

            return Json("ok");
        }

        [HttpGet(Name = "Get the Order pallet positions")]
        public JsonResult GetOrderPalletPositions(int truckId)
        {
            var truckDTO = _context.Trucks.Where(k=>k.TruckId == truckId).FirstOrDefault();
            var palletIds = _context.TruckOrderPallets.Include(k=> k.Pallet).Where(k => k.TruckId == truckId).ToList().Select(k=> k.PalletId).ToList();

            WarehouseTruckDTO warehouseTruckDTO = new WarehouseTruckDTO();
            warehouseTruckDTO.CreationDateTime = _context.Trucks.Where(k => k.TruckId == truckId).FirstOrDefault().CreationDateTime;
            warehouseTruckDTO.PalletBtnIds = new List<int>();
            warehouseTruckDTO.PalletBtnIds = palletIds;

            return Json(warehouseTruckDTO);
        }


        [HttpGet(Name = "Get the Order pallet positions")]
        public JsonResult GetTruckScanningData(int truckId)
        {
            var truckDTO = _context.Trucks.Where(k => k.TruckId == truckId).FirstOrDefault();
            var palletIds = _context.TruckOrderPallets.Include(k => k.Pallet).Where(k => k.TruckId == truckId).ToList().Select(k => k.PalletId).ToList();

            WarehouseTruckDTO warehouseTruckDTO = new WarehouseTruckDTO();
            warehouseTruckDTO.CreationDateTime = _context.Trucks.Where(k => k.TruckId == truckId).FirstOrDefault().CreationDateTime;
            warehouseTruckDTO.PalletBtnIds = new List<int>();
            warehouseTruckDTO.PalletBtnIds = palletIds;

            return Json(warehouseTruckDTO);
        }

        [HttpPost(Name = "Post pallets for truck")]
        public JsonResult CreateTruckOLD(List<String> palletPositions)
        {
            //if (palletPositions == null || palletPositions.Count < 5)
            //    return Json(new { success = false });

            //try
            //{
            //    Truck truck = new Truck();
            //    truck.CreationDateTime = DateTime.Now;
            //    _context.Trucks.Add(truck);

            //    List<TruckPallet> truckPallets = new List<TruckPallet>();
            //    for (int i = 0; i < palletPositions.Count; i++)
            //    {
            //        //string warehouseName, string loadinglineId, string itemId
            //        int warehouseId = int.Parse(palletPositions[i].Split('-')[2].Split('-')[0]);
            //        int loadingLineId = int.Parse(palletPositions[i].Split('-')[3].Split('-')[0]);
            //        int itemId = int.Parse(palletPositions[i].Split('-')[4].Split('-')[0]);
            //        truckPallets.Add(new TruckPallet { PalletId = _context.Warehouses.Where(k => k.LoadingAreaId == warehouseId && k.LoadingLine == loadingLineId && k.LoadingPosition == itemId).FirstOrDefault().PalletId.Value });
            //    }

            //    _context.SaveChanges();

            //    var truckId = _context.Trucks.OrderByDescending(k => k.TruckId).FirstOrDefault().TruckId;
            //    truckPallets.ForEach(k => k.TruckId = truckId);
            //    _context.TruckPallets.AddRange(truckPallets);
            //    await _context.SaveChangesAsync();

            //    //Update pallet status
            //    await _context.Pallets.Where(k => truckPallets.Select(l => l.PalletId).ToList().Contains(k.PalletId)).ForEachAsync(k => k.Status = k.Status + 1);
            //    await _context.SaveChangesAsync();

            //    //Navigate to TruckList page
            //    return Json(new { success = true });
            //}
            //catch (Exception ex)
            //{
            //    var truckToRevert = _context.Trucks.OrderByDescending(k => k.TruckId).FirstOrDefault();
            //    _context.Remove(truckToRevert);
            //    return Json(new { success = false });
            //}
            return null;
        }

        //[HttpGet(Name = "Confirm truck order")]
        //public async Task<JsonResult> ConfirmOrder(string truckId)
        //{
        //    if (int.Parse(truckId) == 0)
        //        return Json(new { success = false });

        //    try
        //    {
        //        var truck = _context.Trucks.Where(k => k.TruckId == int.Parse(truckId)).FirstOrDefault();

        //        var palletIds = _context.TruckPallets.Where(k => k.TruckId == int.Parse(truckId)).Select(k => k.PalletId).ToList();
        //        var pallets = _context.Pallets.Where(k => palletIds.Contains(k.PalletId)).ToList();
        //        foreach (var pallet in pallets)
        //            if (Convert.ToBoolean(truck.IsAuthorized))
        //                pallet.Status = (int)PalletStatus.SelectedForLoad;
        //            else
        //                pallet.Status = (int)PalletStatus.ConfirmedForLoad;

        //        _context.UpdateRange(pallets);

        //        truck.IsAuthorized = truck.IsAuthorized == 1 ? Convert.ToByte(0.ToString()) : Convert.ToByte(1.ToString());
        //        _context.Update(truck);

        //        await _context.SaveChangesAsync();

        //        return Json(new { success = true });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { success = false });
        //    }
        //}

        //Picking orders

        //Picking compare orders and pallets
        [HttpPost]
        public JsonResult CompareOrdersPallets(IFormCollection data)
        {
            try
            {
                List<int> selected_order_ids = new List<int>();
                string orderIdsStr = data["selected-order-ids"].ToString();
                if (!string.IsNullOrEmpty(orderIdsStr))
                    foreach (var orderIdStr in orderIdsStr.Split(','))
                        selected_order_ids.Add(int.Parse(orderIdStr));

                List<int> selected_pallet_ids = new List<int>();
                string palletIdsStr = data["selected-pallet-ids"].ToString();
                if (!string.IsNullOrEmpty(palletIdsStr))
                    foreach (var palletIdStr in palletIdsStr.Split(','))
                        selected_pallet_ids.Add(int.Parse(palletIdStr));

                var orders = _context.Orders.Include(k => k.Material).Where(k => selected_order_ids.Contains(k.OrderId) && k.Status == 0 && k.ShipToParty != null && k.SoldToParty != null && k.Quantity != 0).Select(k => new EDIOrderDTO
                {
                    Material = new MaterialDTO
                    {
                        MaterialId = k.Material.MaterialId,
                        MaterialSAPID = k.Material.MaterialSapid.TrimStart('0'),
                        MaterialDescription = k.Material.MaterialDescription,
                        PackagingLineDescription = k.Material.PackagingLineDescription,
                        GTIN = k.Material.Gtin
                    },
                    Quantity = k.Quantity
                }).ToList();

                var pallets = _context.Pallets.Include(k => k.Material).Where(k => selected_pallet_ids.Contains(k.PalletId)).Select(k => new
                {
                    Material = new MaterialDTO
                    {
                        MaterialId = k.Material.MaterialId,
                        MaterialSAPID = k.Material.MaterialSapid.TrimStart('0'),
                        MaterialDescription = k.Material.MaterialDescription,
                        PackagingLineDescription = k.Material.PackagingLineDescription,
                        GTIN = k.Material.Gtin
                    }
                }).ToList();

                List<MaterialOrderPalletDTO> materialOrderPalletDTOs = new List<MaterialOrderPalletDTO>();
                foreach (var order in orders)
                {
                    if (materialOrderPalletDTOs.Select(k => k.Material.MaterialId).Any(k => k == order.Material.MaterialId))
                        materialOrderPalletDTOs.Where(k => k.Material.MaterialId == order.Material.MaterialId).ToList()
                            .ForEach(k => { k.OrderPalletQuantity = k.OrderPalletQuantity + order.Quantity; k.NoOrders = k.NoOrders + 1; });
                    else
                    {
                        materialOrderPalletDTOs.Add(new MaterialOrderPalletDTO
                        {
                            Material = order.Material,
                            OrderPalletQuantity = order.Quantity,
                            NoOrders = 1
                        });
                    }
                }

                //foreach orders
                foreach (var pallet in pallets)
                {
                    if (materialOrderPalletDTOs.Select(k => k.Material.MaterialId).Any(k => k == pallet.Material.MaterialId))
                        materialOrderPalletDTOs.Where(k => k.Material.MaterialId == pallet.Material.MaterialId).ToList()
                            .ForEach(k => { k.PalletQuantity = k.PalletQuantity + 1; });
                    else
                    {
                        materialOrderPalletDTOs.Add(new MaterialOrderPalletDTO
                        {
                            Material = pallet.Material,
                            PalletQuantity = 1
                        });
                    }
                }
                return Json(materialOrderPalletDTOs);
            }
            catch(Exception ex)
            {
                return Json(ex);
            }
        }

        string issue = string.Empty;
        [HttpPost]
        public async Task<IActionResult> CreateTruck(IFormCollection data)
        {
            //DELETE <----
            try
            {
                List<int> selected_order_ids = new List<int>();
                string orderIdsStr = data["selected-order-ids"].ToString();
                if (!string.IsNullOrEmpty(orderIdsStr))
                    foreach (var orderIdStr in orderIdsStr.Split(','))
                        selected_order_ids.Add(int.Parse(orderIdStr));

                List<int> selected_pallet_ids = new List<int>();
                string palletIdsStr = data["selected-pallet-ids"].ToString();
                if (!string.IsNullOrEmpty(palletIdsStr))
                    foreach (var palletIdStr in palletIdsStr.Split(','))
                        selected_pallet_ids.Add(int.Parse(palletIdStr));

                //List<int> selected_order_ids = new List<int>();
                //selected_order_ids.Add(2);
                var orders = _context.Orders.Include(k => k.Material).Where(k => selected_order_ids.Contains(k.OrderId)).ToList();

                issue = "1";
                //List<int> selected_pallet_ids = new List<int>();
                //selected_pallet_ids.Add(510);
                //selected_pallet_ids.Add(509);
                //selected_pallet_ids.Add(508);
                //selected_pallet_ids.Add(507);
                //selected_pallet_ids.Add(506);
                //selected_pallet_ids.Add(505);
                //selected_pallet_ids.Add(504);
                //selected_pallet_ids.Add(503);
                //selected_pallet_ids.Add(502);
                //selected_pallet_ids.Add(501);
                var pallets = _context.Pallets.Include(k => k.Material).Where(k => selected_pallet_ids.Contains(k.PalletId)).ToList();


                //Create new truck
                Truck truck = new Truck
                {
                    IsAuthorized = 0,
                    CreationDateTime = DateTime.Now
                };
                issue = "2";
                //Create the order-pallet connection for the database
                var orderDTOs = _context.Orders.Include(k => k.Material).Where(k => selected_order_ids.Contains(k.OrderId)).Select(k => new EDIOrderDTO
                {
                    OrderId = k.OrderId,
                    ReferenceNumber = k.ReferenceNumber,
                    ShipToParty = _context.Customers.Include(k => k.Country).Where(x => x.CustomerId == k.ShipToParty).Select(x => new CustomerDTO
                    {
                        Name1 = x.Name1,
                        Name2 = x.Name2,
                        Address = x.Address.ToString(),
                        City = x.City,
                        Street = x.Street,
                        Country = x.Country.NameEn,
                        //PostalCode = x.PostalCode,
                        GTIN = x.LocationNo1 + x.LocationNo2 + x.CheckDigit
                    }).FirstOrDefault(),
                    SoldToParty = _context.Customers.Include(k => k.Country).Where(x => x.CustomerId == k.SoldToParty).Select(x => new CustomerDTO
                    {
                        Name1 = x.Name1,
                        Name2 = x.Name2,
                        Address = x.Address.ToString(),
                        City = x.City,
                        Street = x.Street,
                        Country = x.Country.NameEn,
                        //PostalCode = x.PostalCode,
                        GTIN = x.LocationNo1 + x.LocationNo2 + x.CheckDigit
                    }).FirstOrDefault(),
                    Material = new MaterialDTO
                    {
                        MaterialId = k.Material.MaterialId,
                        Name = k.Material.MaterialDescription,
                        PalletFactor = k.Material.PalletFactor,
                        GrossWeight = k.Material.GrossWeight,
                        NetWeight = k.Material.NetWeight,
                        MaterialSAPID = k.Material.MaterialSapid.TrimStart('0'),
                        MaterialDescription = k.Material.MaterialDescription,
                        PackagingLineDescription = k.Material.PackagingLineDescription,
                        GTIN = k.Material.Gtin
                    },
                    Quantity = k.Quantity,
                    DocumentDate = k.DocumentDate,
                    PurchaseOrderDate = k.PurchaseOrderDate,
                    RequestedDeliveryDate = k.RequestedDeliveryDate
                }).ToList();

                issue = "3";
                var palletDTOs = _context.Pallets.Include(k => k.Material).Where(k => selected_pallet_ids.Contains(k.PalletId)).Select(k => new PalletDTO
                {
                    PalletId = k.PalletId,
                    PalletLOT = k.PalletLot,
                    SSCC = k.Sscc,
                    Material = new MaterialDTO
                    {
                        MaterialId = k.Material.MaterialId,
                        MaterialSAPID = k.Material.MaterialSapid.TrimStart('0'),
                        PalletFactor = k.Material.PalletFactor,
                        MaxDeliveryDate = GetMaxDeliveryDate(k.PalletCreationDateTime.Value, k.Material.LagerDays),
                        MaterialDescription = k.Material.MaterialDescription,
                        PackagingLineDescription = k.Material.PackagingLineDescription,
                        GTIN = k.Material.Gtin
                    }
                }).ToList();

                var truckOrderPallets = new List<TruckOrderPallet>();
                var _palletDTOs = palletDTOs;
                foreach (var orderDTO in orderDTOs)
                {
                    List<PalletDTO> palletCurrentOrderDTOs = new List<PalletDTO>();
                    palletCurrentOrderDTOs = _palletDTOs.Where(k => k.Material.MaterialId == orderDTO.Material.MaterialId).Take(orderDTO.Quantity).ToList();

                    foreach (var palletCurrentOrderDTO in palletCurrentOrderDTOs)
                    {
                        truckOrderPallets.Add(new TruckOrderPallet
                        {
                            OrderId = orderDTO.OrderId,
                            PalletId = palletCurrentOrderDTO.PalletId,
                        });
                    }
                    _palletDTOs = _palletDTOs.Where(k => !palletCurrentOrderDTOs.Select(l => l.PalletId).Contains(k.PalletId)).ToList();
                }
                truck.TruckOrderPallets = truckOrderPallets;

                issue = "4";
                //Create LIDL palletcard for all pallets (per pallet & all)
                var palletCardLidlDTOs = new List<PalletCardLidlDTO>();
                var truckOrderPalletsGrouped = truckOrderPallets.GroupBy(k => k.OrderId).Select(k => k.ToList()).ToList();
                foreach (var truckOrderPalletsGroupedItem in truckOrderPalletsGrouped)
                {
                    var orderDTO = orderDTOs.Where(k => k.OrderId == truckOrderPalletsGroupedItem.FirstOrDefault().OrderId).FirstOrDefault();
                    var palletNumber = 0;
                    foreach (var pallet in truckOrderPalletsGroupedItem)
                    {
                        //Generate pallet card LIDL
                        var palletDTO = palletDTOs.Where(k => k.PalletId == pallet.PalletId).FirstOrDefault();
                        palletNumber++;
                        palletCardLidlDTOs.Add(new PalletCardLidlDTO
                        {
                            Supplier = "ARI S.A.",
                            OrderNumber = orderDTO.ReferenceNumber,
                            Material = orderDTO.Material,
                            Pallet = palletDTO,
                            PalletNumber = palletNumber,
                            SumPallet = truckOrderPalletsGroupedItem.Count,
                            ShipToParty = orderDTO.ShipToParty,
                            SoldToParty = orderDTO.SoldToParty,
                            BestBefore = palletDTO.Material.ExpirationDate,
                            DeliveryDate = palletDTO.Material.MaxDeliveryDate,
                            SSCC = palletDTO.SSCC,
                            Order = orderDTO,
                            DeliveryNoteNo = "DA A1 0081144"
                        });
                    }
                }

                issue = "5";
                //Per pallet
                string randomIndex = string.Empty;
                foreach (var palletCardLidlDTO in palletCardLidlDTOs)
                {
                    //Random name
                    randomIndex = RandomString(15);

                    //Generate pallet card Lidl
                    GeneratePalletCardLIDL(palletCardLidlDTO, randomIndex);

                    var pallet = _context.Pallets.Where(k => k.PalletId == palletCardLidlDTO.Pallet.PalletId).FirstOrDefault();
                    pallet.PalletCardLidl = System.IO.Path.Combine("https://arisa.blob.core.windows.net", AzureGPV2Credentials.CONTAINER_PDF_FILES, "pallet_card_lidl_" + randomIndex + ".pdf");
                    pallet.Status = 3;
                    _context.Update(pallet);
                }

                //All pallets
                randomIndex = RandomString(15);

                //Generate pallet card Lidl
                GeneratePalletCardsLIDL(palletCardLidlDTOs, randomIndex);
                truck.PalletCardLidl = System.IO.Path.Combine("https://arisa.blob.core.windows.net", AzureGPV2Credentials.CONTAINER_PDF_FILES, "pallet_card_lidl_" + randomIndex + ".pdf");

                issue = "61";
                //Create driver documents - All
                await GenerateDriverInvoice(palletCardLidlDTOs, randomIndex);
                issue = "62";

                truck.DriverDocuments = System.IO.Path.Combine("https://arisa.blob.core.windows.net", AzureGPV2Credentials.CONTAINER_PDF_FILES, "LFG" + randomIndex + ".pdf");
                issue = "63";
                //Create CMR documents - All
                await GenerateDriverCMR(palletCardLidlDTOs, randomIndex, CMRType.Chiajna);
                issue = "64";

                truck.Cmrdocuments = System.IO.Path.Combine("https://arisa.blob.core.windows.net", AzureGPV2Credentials.CONTAINER_PDF_FILES, "CMR" + randomIndex + ".pdf");

                issue = "7";
                //Create LIDL Orders Documents - All
                await GenerateLIDLOrdersDocuments(palletCardLidlDTOs, randomIndex);
                truck.LidlordersHeaders = System.IO.Path.Combine("https://arisa.blob.core.windows.net", AzureGPV2Credentials.CONTAINER_LIDL_ORDERS, "ARI_DESADV_HEADER_" + randomIndex + ".txt");
                truck.LidlordersLines = System.IO.Path.Combine("https://arisa.blob.core.windows.net", AzureGPV2Credentials.CONTAINER_LIDL_ORDERS, "ARI_DESADV_LINES_" + randomIndex + ".txt");

                var ordersToUpdate = _context.Orders.Where(k => selected_order_ids.Contains(k.OrderId)).ToList();

                foreach (var order in ordersToUpdate)
                    order.Status = 1;

                issue = "8";
                _context.Orders.UpdateRange(ordersToUpdate);

                //Save truck
                issue = "9";
                await _context.Trucks.AddAsync(truck);
                await _context.SaveChangesAsync();

                issue = "10";

                return Json(issue);
            }
            catch (Exception ex)
            {
                return Json(issue);
            }
        }

        public IActionResult TruckList()
        {
            var truckDTOs = _context.Trucks.Include(k=> k.TruckOrderPallets).Where(k=>k.IsAuthorized != 2).Select(k => new TruckDTO 
            { 
                TruckId = k.TruckId,
                PassToken = protector.Decode(k.TruckId.ToString()),
                CreationDateTime = k.CreationDateTime,
                IsAuthorized = k.IsAuthorized,
                NoOrders = k.TruckOrderPallets.Where(x => x.TruckId == k.TruckId).Select(k=>k.OrderId).GroupBy(k=>k.ToString()).Count(),
                NoPallets = k.TruckOrderPallets.Where(x => x.TruckId == k.TruckId).Select(k => k.PalletId).GroupBy(k => k.ToString()).Count(),
                PalletCardLIDL = k.PalletCardLidl,
                DriverDocuments = k.DriverDocuments,
                CMRDocuments = k.Cmrdocuments,
                LIDLOrdersHeaders = k.LidlordersHeaders,
                LIDLOrdersLines = k.LidlordersLines,
                CompletionDateTime = k.CompletionDateTime
            }).ToList();

            return View(truckDTOs);
        }

        public async Task<IActionResult> DeleteTruck(int truckId)
        {
            //if (int.Parse(truckId) == 0)
            //    return Json(new { success = false });

            var truck = _context.Trucks.Where(k => k.TruckId == truckId).FirstOrDefault();
            var truckOrderPallets = _context.TruckOrderPallets.Where(k => k.TruckId == truck.TruckId).ToList();

            List<int> palletIds = new List<int>();
            foreach (var item in truckOrderPallets)
                palletIds.Add(item.PalletId);

            var pallets = _context.Pallets.Where(k => palletIds.Contains(k.PalletId)).ToList();
            foreach (var pallet in pallets)
            {
                pallet.Status = 2;
                pallet.PalletCardLidl = null;
            }

            var orderIds = truckOrderPallets.Select(k => k.OrderId).ToList();
            var orders = _context.Orders.Where(k => orderIds.Contains(k.OrderId)).ToList();

            foreach (var order in orders)
                order.Status = 0;

            _context.Orders.UpdateRange(orders);
            _context.UpdateRange(pallets);
            _context.RemoveRange(truckOrderPallets);
            _context.Remove(truck);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(TruckList));
        }

        [HttpPost]
        public JsonResult HandleTruckValidation(IFormCollection data)
        {
            int truck_id = int.Parse(data["truck-id"]);

            var truck = _context.Trucks.Where(k => k.TruckId == truck_id).FirstOrDefault();
            truck.IsAuthorized = (byte)(truck.IsAuthorized == 1? 0 : 1);
            _context.Trucks.Update(truck);

            var palletIds = _context.TruckOrderPallets.Where(k => k.TruckId == truck_id).Select(k=>k.PalletId).ToList();
            var pallets = _context.Pallets.Where(k => palletIds.Contains(k.PalletId)).ToList();
            foreach (var pallet in pallets)
                if (truck.IsAuthorized == 0)
                    pallet.Status = 3;
                else
                    pallet.Status = 4;
            _context.Pallets.UpdateRange(pallets);
            _context.SaveChanges();

            return new JsonResult("ok");
        }

        //Helpers
        public static int GetLot(DateTime formDateTime)
        {
            DateTime referenceDate = new DateTime(2021, 1, 1);
            TimeSpan difference = formDateTime - referenceDate;
            return difference.Days + 466;
        }
        public static DateTime GetExpirationDate(DateTime formDateTime, int expirationDays)
        {
            return formDateTime.AddDays(expirationDays - 1);
        }
        public static DateTime GetMaxDeliveryDate(DateTime formDateTime, int lagerDays)
        {
            return formDateTime.AddDays(lagerDays - 1);
        }
        private static Random random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        private void GenerateQRCode(string PalletPassToken, string randomIndex)
        {
            QRCodeGenerator _qrCode = new QRCodeGenerator();
            QRCodeData _qrCodeData = _qrCode.CreateQrCode($"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}" + "/PackagingLine/Pallet?pallet=" + PalletPassToken, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(_qrCodeData);
            System.Drawing.Bitmap qrCodeImage = qrCode.GetGraphic(20);
            string randomNewName = "lidl_qrcode_" + randomIndex + ".jpg";
            qrCodeImage.Save(Path.Combine(_environment.WebRootPath, "images", "temp-qr-codes") + $@"\" + randomNewName);
        }
        private System.Drawing.Image GenerateBarCode(string code)
        {
            var width = 400;
            var height = 800;
            var barcodeWriterPixelData = new ZXing.BarcodeWriterPixelData
            {
                Format = ZXing.BarcodeFormat.CODE_128,
                Options = new QrCodeEncodingOptions
                {
                    Height = height,
                    Width = width,
                    PureBarcode = false,
                    Margin = 10,
                }
            };

            var pixelData = barcodeWriterPixelData.Write(code);
            using (var bitmap = new System.Drawing.Bitmap(pixelData.Width, pixelData.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb))
            {
                using (var memoryStream = new MemoryStream())
                {
                    var bitmapData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, pixelData.Width, pixelData.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
                    try
                    {
                        System.Runtime.InteropServices.Marshal.Copy(pixelData.Pixels, 0, bitmapData.Scan0, pixelData.Pixels.Length);
                    }
                    finally
                    {
                        bitmap.UnlockBits(bitmapData);
                    }
                    Bitmap _bitmap = new Bitmap(bitmap);
                    return ((System.Drawing.Image)_bitmap);
                }
            }
        }

        public async void GeneratePalletCardLIDL(PalletCardLidlDTO palletCardLidlDTO, string randomIndex)
        {
            //Get palletInfo
            string PDFNewFileName = "pallet_card_lidl_" + randomIndex + ".pdf";
            string templateDirectory = Path.Combine(_environment.WebRootPath, "pdf-templates") + $@"\" + PDFTemplateUrl.PALLET_CARD_LIDL_NAME;
            string filePathDirectory = Path.Combine(_environment.WebRootPath, "pdf-files") + $@"\" + PDFNewFileName;
            try
            {
                using (var templateFileStream = new System.IO.FileStream(templateDirectory, System.IO.FileMode.Open))
                {
                    // Open existing PDF
                    var pdfReader = new PdfReader(templateFileStream);
                    iTextSharp.text.Rectangle size = pdfReader.GetPageSizeWithRotation(1);
                    iTextSharp.text.Document document = new iTextSharp.text.Document(size);

                    // open the writer
                    FileStream fs = new FileStream(filePathDirectory, FileMode.Create, FileAccess.Write);
                    PdfWriter writer = PdfWriter.GetInstance(document, fs);
                    document.Open();

                    // the pdf content
                    PdfContentByte cb = writer.DirectContent;

                    // X1 copies
                    for (int i = 0; i < 1; i++)
                    {
                        document.NewPage();
                        // create the new page and add it to the pdf
                        PdfImportedPage page = writer.GetImportedPage(pdfReader, 1);
                        cb.AddTemplate(page, 0, 0);

                        ////Add barcode code image
                        //1st
                        System.Drawing.Image img = GenerateBarCode("(02)" + palletCardLidlDTO.Material.GTIN + "(400)" + palletCardLidlDTO.OrderNumber);
                        iTextSharp.text.Image jpgBarRCode = iTextSharp.text.Image.GetInstance(img, System.Drawing.Imaging.ImageFormat.Jpeg);
                        jpgBarRCode.SetAbsolutePosition(20, 190);
                        jpgBarRCode.ScaleAbsolute(new iTextSharp.text.Rectangle(380, 56));
                        cb.AddImage(jpgBarRCode);
                        jpgBarRCode.SetAbsolutePosition(446, 190);
                        jpgBarRCode.ScaleAbsolute(new iTextSharp.text.Rectangle(380, 56));
                        cb.AddImage(jpgBarRCode);
                        //2nd
                        jpgBarRCode = iTextSharp.text.Image.GetInstance(GenerateBarCode("(15)" + palletCardLidlDTO.Order.RequestedDeliveryDate.ToString("yyMMdd") + "(10)" + palletCardLidlDTO.Pallet.PalletLOT.ToString() + "(37)" + palletCardLidlDTO.Material.PalletFactor.ToString()), ImageFormat.Png);
                        jpgBarRCode.SetAbsolutePosition(70, 118);
                        jpgBarRCode.ScaleAbsolute(new iTextSharp.text.Rectangle(260, 56));
                        cb.AddImage(jpgBarRCode);
                        jpgBarRCode.SetAbsolutePosition(496, 118);
                        jpgBarRCode.ScaleAbsolute(new iTextSharp.text.Rectangle(260, 56));
                        cb.AddImage(jpgBarRCode);
                        //3rd
                        jpgBarRCode = iTextSharp.text.Image.GetInstance(GenerateBarCode("(00)" + palletCardLidlDTO.SSCC), ImageFormat.Png);
                        jpgBarRCode.SetAbsolutePosition(70, 43);
                        jpgBarRCode.ScaleAbsolute(new iTextSharp.text.Rectangle(260, 56));
                        cb.AddImage(jpgBarRCode);
                        jpgBarRCode.SetAbsolutePosition(496, 43);
                        jpgBarRCode.ScaleAbsolute(new iTextSharp.text.Rectangle(260, 56));
                        cb.AddImage(jpgBarRCode);

                        // select the font properties
                        BaseFont bf = BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                        BaseFont nf = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                        cb.SetColorFill(BaseColor.DARK_GRAY);

                        //Supplier
                        cb.BeginText();
                        cb.SetFontAndSize(bf, 15);
                        cb.ShowTextAligned(0, palletCardLidlDTO.Supplier, 85, 575, 0);
                        cb.ShowTextAligned(0, palletCardLidlDTO.Supplier, 511, 575, 0);
                        //Order Number (Lidl)
                        cb.SetFontAndSize(bf, 18);
                        cb.ShowTextAligned(0, palletCardLidlDTO.OrderNumber, 160, 535, 0);
                        cb.ShowTextAligned(0, palletCardLidlDTO.OrderNumber, 586, 535, 0);
                        //Warehouse (RDC)
                        cb.SetFontAndSize(bf, 12);
                        cb.ShowTextAligned(0, "???", 18, 497, 0);
                        cb.ShowTextAligned(0, "???", 444, 497, 0);
                        //Country
                        cb.ShowTextAligned(0, palletCardLidlDTO.ShipToParty.Name2, 153, 497, 0);
                        cb.ShowTextAligned(0, palletCardLidlDTO.ShipToParty.Name2, 579, 497, 0);
                        //Logistics
                        cb.ShowTextAligned(0, "???", 289, 497, 0);
                        cb.ShowTextAligned(0, "???", 725, 497, 0);
                        //Number of Pallets
                        cb.ShowTextAligned(0, palletCardLidlDTO.SumPallet.ToString(), 125, 448, 0);
                        cb.ShowTextAligned(0, palletCardLidlDTO.SumPallet.ToString(), 551, 448, 0);
                        //Pallet Number
                        cb.ShowTextAligned(0, palletCardLidlDTO.PalletNumber + " of " + palletCardLidlDTO.SumPallet, 305, 448, 0);
                        cb.ShowTextAligned(0, palletCardLidlDTO.PalletNumber + " of " + palletCardLidlDTO.SumPallet, 731, 448, 0);
                        //Delivery to Forwarder
                        cb.ShowTextAligned(0, palletCardLidlDTO.ShipToParty.Street + " " + palletCardLidlDTO.ShipToParty.City, 18, 410, 0);
                        cb.ShowTextAligned(0, palletCardLidlDTO.ShipToParty.Street + " " + palletCardLidlDTO.ShipToParty.City, 444, 410, 0);
                        cb.ShowTextAligned(0, "Delivery Date: " + palletCardLidlDTO.Order.RequestedDeliveryDate.ToString("dd.MM.yy"), 18, 395, 0);
                        cb.ShowTextAligned(0, "Delivery Date: " + palletCardLidlDTO.Order.RequestedDeliveryDate.ToString("dd.MM.yy"), 444, 395, 0);
                        //Item
                        cb.SetFontAndSize(bf, 14);
                        cb.ShowTextAligned(0, palletCardLidlDTO.Material.Name, 18, 350, 0);
                        cb.ShowTextAligned(0, palletCardLidlDTO.Material.Name, 444, 350, 0);
                        //Content (Case-GTIN)
                        cb.SetFontAndSize(bf, 12);
                        cb.ShowTextAligned(0, palletCardLidlDTO.Material.GTIN, 18, 310, 0);
                        cb.ShowTextAligned(0, palletCardLidlDTO.Material.GTIN, 444, 310, 0);
                        //Best before
                        cb.ShowTextAligned(0, string.Format("{0}.{1}.{2}", palletCardLidlDTO.Order.RequestedDeliveryDate.ToString("yy"),
                            palletCardLidlDTO.Order.RequestedDeliveryDate.ToString("MM"), palletCardLidlDTO.Order.RequestedDeliveryDate.ToString("dd")), 220, 310, 0);
                        cb.ShowTextAligned(0, string.Format("{0}.{1}.{2}", palletCardLidlDTO.Order.RequestedDeliveryDate.ToString("yy"),
                            palletCardLidlDTO.Order.RequestedDeliveryDate.ToString("MM"), palletCardLidlDTO.Order.RequestedDeliveryDate.ToString("dd")), 646, 310, 0);
                        //Batch/Lot
                        cb.ShowTextAligned(0, palletCardLidlDTO.Pallet.PalletLOT.ToString(), 18, 283, 0);
                        cb.ShowTextAligned(0, palletCardLidlDTO.Pallet.PalletLOT.ToString(), 444, 283, 0);
                        //Count
                        cb.ShowTextAligned(0, palletCardLidlDTO.Material.PalletFactor.ToString(), 220, 283, 0);
                        cb.ShowTextAligned(0, palletCardLidlDTO.Material.PalletFactor.ToString(), 646, 283, 0);
                        //SSCC
                        cb.ShowTextAligned(0, palletCardLidlDTO.SSCC, 55, 258, 0);
                        cb.ShowTextAligned(0, palletCardLidlDTO.SSCC, 481, 258, 0);

                        cb.SetFontAndSize(bf, 12);
                        //1st barcode
                        cb.ShowTextAligned(0, "(02)" + palletCardLidlDTO.Material.GTIN + "(400)" + palletCardLidlDTO.OrderNumber, 100, 178, 0);
                        cb.ShowTextAligned(0, "(02)" + palletCardLidlDTO.Material.GTIN + "(400)" + palletCardLidlDTO.OrderNumber, 526, 178, 0);
                        //2nd barcode
                        cb.ShowTextAligned(0, "(15)" + palletCardLidlDTO.Order.RequestedDeliveryDate.ToString("yyMMdd") + "(10)" + palletCardLidlDTO.Pallet.PalletLOT.ToString() + "(37)" + palletCardLidlDTO.Material.PalletFactor.ToString(), 120, 108, 0);
                        cb.ShowTextAligned(0, "(15)" + palletCardLidlDTO.Order.RequestedDeliveryDate.ToString("yyMMdd") + "(10)" + palletCardLidlDTO.Pallet.PalletLOT.ToString() + "(37)" + palletCardLidlDTO.Material.PalletFactor.ToString(), 546, 108, 0);
                        //4th barcode
                        cb.ShowTextAligned(0, "(00)" + palletCardLidlDTO.SSCC, 130, 33, 0);
                        cb.ShowTextAligned(0, "(00)" + palletCardLidlDTO.SSCC, 556, 33, 0);

                        cb.EndText();
                    }

                    document.Close();
                    fs.Close();
                    writer.Close();
                    pdfReader.Close();
                }
            }
            catch (Exception ex)
            {

            }

            //Upload to cloud
            bool isUploaded = await ContainerService.UploadOperationAsync(Path.Combine(_environment.WebRootPath, "pdf-files") + $@"\", PDFNewFileName, AzureGPV2Credentials.CONTAINER_PDF_FILES);
        }
        public async void GeneratePalletCardsLIDL(List<PalletCardLidlDTO> palletCardLidlDTOs, string randomIndex)
        {
            //Get palletInfo
            string PDFNewFileName = "pallet_card_lidl_" + randomIndex + ".pdf";
            string templateDirectory = Path.Combine(_environment.WebRootPath, "pdf-templates") + $@"\" + PDFTemplateUrl.PALLET_CARD_LIDL_NAME;
            string filePathDirectory = Path.Combine(_environment.WebRootPath, "pdf-files") + $@"\" + PDFNewFileName;
            try
            {
                using (var templateFileStream = new System.IO.FileStream(templateDirectory, System.IO.FileMode.Open))
                {
                    // Open existing PDF
                    var pdfReader = new PdfReader(templateFileStream);
                    iTextSharp.text.Rectangle size = pdfReader.GetPageSizeWithRotation(1);
                    iTextSharp.text.Document document = new iTextSharp.text.Document(size);

                    // open the writer
                    FileStream fs = new FileStream(filePathDirectory, FileMode.Create, FileAccess.Write);
                    PdfWriter writer = PdfWriter.GetInstance(document, fs);
                    document.Open();

                    // the pdf content
                    PdfContentByte cb = writer.DirectContent;

                    foreach (var palletCardLidlDTO in palletCardLidlDTOs)
                    {
                        // X2 copies
                        for (int i = 0; i < 1; i++)
                        {
                            document.NewPage();
                            // create the new page and add it to the pdf
                            PdfImportedPage page = writer.GetImportedPage(pdfReader, 1);
                            cb.AddTemplate(page, 0, 0);

                            ////Add barcode code image
                            //1st
                            System.Drawing.Image img = GenerateBarCode("(02)" + palletCardLidlDTO.Material.GTIN + "(400)" + palletCardLidlDTO.OrderNumber);
                            iTextSharp.text.Image jpgBarRCode = iTextSharp.text.Image.GetInstance(img, System.Drawing.Imaging.ImageFormat.Jpeg);
                            jpgBarRCode.SetAbsolutePosition(20, 190);
                            jpgBarRCode.ScaleAbsolute(new iTextSharp.text.Rectangle(380, 56));
                            cb.AddImage(jpgBarRCode);
                            jpgBarRCode.SetAbsolutePosition(446, 190);
                            jpgBarRCode.ScaleAbsolute(new iTextSharp.text.Rectangle(380, 56));
                            cb.AddImage(jpgBarRCode);
                            //2nd
                            jpgBarRCode = iTextSharp.text.Image.GetInstance(GenerateBarCode("(15)" + palletCardLidlDTO.Order.RequestedDeliveryDate.ToString("yyMMdd") + "(10)" + palletCardLidlDTO.Pallet.PalletLOT.ToString() + "(37)" + palletCardLidlDTO.Material.PalletFactor.ToString()), ImageFormat.Png);
                            jpgBarRCode.SetAbsolutePosition(70, 118);
                            jpgBarRCode.ScaleAbsolute(new iTextSharp.text.Rectangle(260, 56));
                            cb.AddImage(jpgBarRCode);
                            jpgBarRCode.SetAbsolutePosition(496, 118);
                            jpgBarRCode.ScaleAbsolute(new iTextSharp.text.Rectangle(260, 56));
                            cb.AddImage(jpgBarRCode);
                            //3rd
                            jpgBarRCode = iTextSharp.text.Image.GetInstance(GenerateBarCode("(00)" + palletCardLidlDTO.SSCC), ImageFormat.Png);
                            jpgBarRCode.SetAbsolutePosition(70, 43);
                            jpgBarRCode.ScaleAbsolute(new iTextSharp.text.Rectangle(260, 56));
                            cb.AddImage(jpgBarRCode);
                            jpgBarRCode.SetAbsolutePosition(496, 43);
                            jpgBarRCode.ScaleAbsolute(new iTextSharp.text.Rectangle(260, 56));
                            cb.AddImage(jpgBarRCode);

                            // select the font properties
                            BaseFont bf = BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                            BaseFont nf = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                            cb.SetColorFill(BaseColor.DARK_GRAY);

                            //Supplier
                            cb.BeginText();
                            cb.SetFontAndSize(bf, 15);
                            cb.ShowTextAligned(0, palletCardLidlDTO.Supplier, 85, 575, 0);
                            cb.ShowTextAligned(0, palletCardLidlDTO.Supplier, 511, 575, 0);
                            //Order Number (Lidl)
                            cb.SetFontAndSize(bf, 18);
                            cb.ShowTextAligned(0, palletCardLidlDTO.OrderNumber, 160, 535, 0);
                            cb.ShowTextAligned(0, palletCardLidlDTO.OrderNumber, 586, 535, 0);
                            //Warehouse (RDC)
                            cb.SetFontAndSize(bf, 12);
                            cb.ShowTextAligned(0, "???", 18, 497, 0);
                            cb.ShowTextAligned(0, "???", 444, 497, 0);
                            //Country
                            cb.ShowTextAligned(0, palletCardLidlDTO.ShipToParty.Name2, 153, 497, 0);
                            cb.ShowTextAligned(0, palletCardLidlDTO.ShipToParty.Name2, 579, 497, 0);
                            //Logistics
                            cb.ShowTextAligned(0, "???", 289, 497, 0);
                            cb.ShowTextAligned(0, "???", 725, 497, 0);
                            //Number of Pallets
                            cb.ShowTextAligned(0, palletCardLidlDTO.SumPallet.ToString(), 125, 448, 0);
                            cb.ShowTextAligned(0, palletCardLidlDTO.SumPallet.ToString(), 551, 448, 0);
                            //Pallet Number
                            cb.ShowTextAligned(0, palletCardLidlDTO.PalletNumber + " of " + palletCardLidlDTO.SumPallet, 305, 448, 0);
                            cb.ShowTextAligned(0, palletCardLidlDTO.PalletNumber + " of " + palletCardLidlDTO.SumPallet, 731, 448, 0);
                            //Delivery to Forwarder
                            cb.ShowTextAligned(0, palletCardLidlDTO.ShipToParty.Street + " " + palletCardLidlDTO.ShipToParty.City, 18, 410, 0);
                            cb.ShowTextAligned(0, palletCardLidlDTO.ShipToParty.Street + " " + palletCardLidlDTO.ShipToParty.City, 444, 410, 0);
                            cb.ShowTextAligned(0, "Delivery Date: " + palletCardLidlDTO.Order.RequestedDeliveryDate.ToString("dd.MM.yy"), 18, 395, 0);
                            cb.ShowTextAligned(0, "Delivery Date: " + palletCardLidlDTO.Order.RequestedDeliveryDate.ToString("dd.MM.yy"), 444, 395, 0);
                            //Item
                            cb.SetFontAndSize(bf, 14);
                            cb.ShowTextAligned(0, palletCardLidlDTO.Material.Name, 18, 350, 0);
                            cb.ShowTextAligned(0, palletCardLidlDTO.Material.Name, 444, 350, 0);
                            //Content (Case-GTIN)
                            cb.SetFontAndSize(bf, 12);
                            cb.ShowTextAligned(0, palletCardLidlDTO.Material.GTIN, 18, 310, 0);
                            cb.ShowTextAligned(0, palletCardLidlDTO.Material.GTIN, 444, 310, 0);
                            //Best before
                            cb.ShowTextAligned(0, string.Format("{0}.{1}.{2}", palletCardLidlDTO.Order.RequestedDeliveryDate.ToString("yy"),
                                palletCardLidlDTO.Order.RequestedDeliveryDate.ToString("MM"), palletCardLidlDTO.Order.RequestedDeliveryDate.ToString("dd")), 220, 310, 0);
                            cb.ShowTextAligned(0, string.Format("{0}.{1}.{2}", palletCardLidlDTO.Order.RequestedDeliveryDate.ToString("yy"),
                                palletCardLidlDTO.Order.RequestedDeliveryDate.ToString("MM"), palletCardLidlDTO.Order.RequestedDeliveryDate.ToString("dd")), 646, 310, 0);
                            //Batch/Lot
                            cb.ShowTextAligned(0, palletCardLidlDTO.Pallet.PalletLOT.ToString(), 18, 283, 0);
                            cb.ShowTextAligned(0, palletCardLidlDTO.Pallet.PalletLOT.ToString(), 444, 283, 0);
                            //Count
                            cb.ShowTextAligned(0, palletCardLidlDTO.Material.PalletFactor.ToString(), 220, 283, 0);
                            cb.ShowTextAligned(0, palletCardLidlDTO.Material.PalletFactor.ToString(), 646, 283, 0);
                            //SSCC
                            cb.ShowTextAligned(0, palletCardLidlDTO.SSCC, 55, 258, 0);
                            cb.ShowTextAligned(0, palletCardLidlDTO.SSCC, 481, 258, 0);

                            cb.SetFontAndSize(bf, 12);
                            //1st barcode
                            cb.ShowTextAligned(0, "(02)" + palletCardLidlDTO.Material.GTIN + "(400)" + palletCardLidlDTO.OrderNumber, 100, 178, 0);
                            cb.ShowTextAligned(0, "(02)" + palletCardLidlDTO.Material.GTIN + "(400)" + palletCardLidlDTO.OrderNumber, 526, 178, 0);
                            //2nd barcode
                            cb.ShowTextAligned(0, "(15)" + palletCardLidlDTO.Order.RequestedDeliveryDate.ToString("yyMMdd") + "(10)" + palletCardLidlDTO.Pallet.PalletLOT.ToString() + "(37)" + palletCardLidlDTO.Material.PalletFactor.ToString(), 120, 108, 0);
                            cb.ShowTextAligned(0, "(15)" + palletCardLidlDTO.Order.RequestedDeliveryDate.ToString("yyMMdd") + "(10)" + palletCardLidlDTO.Pallet.PalletLOT.ToString() + "(37)" + palletCardLidlDTO.Material.PalletFactor.ToString(), 546, 108, 0);
                            //4th barcode
                            cb.ShowTextAligned(0, "(00)" + palletCardLidlDTO.SSCC, 130, 33, 0);
                            cb.ShowTextAligned(0, "(00)" + palletCardLidlDTO.SSCC, 556, 33, 0);

                            cb.EndText();
                        }
                    }

                    document.Close();
                    fs.Close();
                    writer.Close();
                    pdfReader.Close();
                }
            }
            catch (Exception ex)
            {

            }

            //Upload to cloud
            bool isUploaded = await ContainerService.UploadOperationAsync(Path.Combine(_environment.WebRootPath, "pdf-files") + $@"\", PDFNewFileName, AzureGPV2Credentials.CONTAINER_PDF_FILES);
        }

        private async Task GenerateDriverInvoice(List<PalletCardLidlDTO> palletCardLidlDTOs, string randomIndex)
        {
            try
            {
                issue = "611";
                string PDFNewFileName = "LFG" + randomIndex + ".pdf";
                string templateDirectory = Path.Combine(_environment.WebRootPath, "pdf-templates") + $@"\" + PDFTemplateUrl.LFG_LIDL_NAME;
                string filePathDirectory = Path.Combine(_environment.WebRootPath, "pdf-files") + $@"\" + PDFNewFileName;
                issue = "612";
                using (var templateFileStream = new FileStream(templateDirectory, FileMode.Open))
                {
                    issue = "6121";
                    // Open existing PDF
                    var pdfReader = new PdfReader(templateFileStream);
                    iTextSharp.text.Rectangle size = pdfReader.GetPageSizeWithRotation(1);
                    iTextSharp.text.Document document = new iTextSharp.text.Document(size);
                    issue = "6122";
                    // open the writer
                    FileStream fs = new FileStream(filePathDirectory, FileMode.Create, FileAccess.Write);
                    PdfWriter writer = PdfWriter.GetInstance(document, fs);
                    issue = "6123";
                    document.Open();
                    issue = "613";

                    // X6 copies
                    for (int i = 0; i < 6; i++)
                    {
                        document.NewPage();
                        // the pdf content
                        PdfContentByte cb = writer.DirectContent;

                        // create the new page and add it to the pdf
                        PdfImportedPage page = writer.GetImportedPage(pdfReader, 1);
                        cb.AddTemplate(page, 0, 0);

                        // select the font properties
                        BaseFont bf = BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                        BaseFont nf = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                        cb.SetColorFill(BaseColor.DARK_GRAY);
                        cb.SetFontAndSize(bf, 8);

                        //CMR Texts
                        cb.BeginText();
                        cb.SetFontAndSize(bf, 10);
                        cb.ShowTextAligned(Element.ALIGN_LEFT, "DELIVERY NOTE", 560, 800, 270);
                        cb.ShowTextAligned(Element.ALIGN_LEFT, "ORIGINAL", 565, 420, 270);
                        cb.ShowTextAligned(Element.ALIGN_LEFT, "Supplier", 545, 800, 270);
                        cb.ShowTextAligned(Element.ALIGN_LEFT, "Goods Recipient", 440, 800, 270);
                        cb.SetFontAndSize(bf, 11);
                        cb.ShowTextAligned(Element.ALIGN_LEFT, "Date:  " + DateTime.Now.ToString("dd.MM.yyyy"), 480, 230, 270);
                        cb.ShowTextAligned(Element.ALIGN_LEFT, "Delivery note no:  " + palletCardLidlDTOs.FirstOrDefault().DeliveryNoteNo, 460, 250, 270);
                        cb.ShowTextAligned(Element.ALIGN_LEFT, "Order no:  " + palletCardLidlDTOs.FirstOrDefault().OrderNumber, 435, 250, 270);
                        cb.ShowTextAligned(Element.ALIGN_LEFT, "Delivery date:  " + palletCardLidlDTOs.FirstOrDefault().Order.RequestedDeliveryDate.ToString("dd.MM.yyyy"), 410, 230, 270);
                        cb.SetFontAndSize(bf, 8);
                        cb.ShowTextAligned(Element.ALIGN_LEFT, "Item no.", 380, 815, 270);
                        cb.ShowTextAligned(Element.ALIGN_LEFT, "Item Description", 380, 770, 270);
                        cb.ShowTextAligned(Element.ALIGN_LEFT, "Case size", 380, 600, 270);
                        cb.ShowTextAligned(Element.ALIGN_LEFT, "Batch no.", 380, 555, 270);
                        cb.ShowTextAligned(Element.ALIGN_LEFT, "Best Before", 380, 500, 270);
                        cb.ShowTextAligned(Element.ALIGN_LEFT, "Case Qty", 380, 420, 270);
                        cb.ShowTextAligned(Element.ALIGN_LEFT, "Pallet", 380, 360, 270);
                        cb.ShowTextAligned(Element.ALIGN_LEFT, "amount", 370, 360, 270);
                        cb.ShowTextAligned(Element.ALIGN_LEFT, "Pallet", 380, 300, 270);
                        cb.ShowTextAligned(Element.ALIGN_LEFT, "factor", 370, 300, 270);
                        cb.ShowTextAligned(Element.ALIGN_LEFT, "Net Weight(kg)", 380, 240, 270);
                        cb.ShowTextAligned(Element.ALIGN_LEFT, "Gross Weight/", 380, 160, 270);
                        cb.ShowTextAligned(Element.ALIGN_LEFT, "pallet(kg)", 370, 160, 270);
                        cb.ShowTextAligned(Element.ALIGN_LEFT, "Gross Weight/", 380, 90, 270);
                        cb.ShowTextAligned(Element.ALIGN_LEFT, "total(kg)", 370, 85, 270);
                        cb.ShowTextAligned(Element.ALIGN_LEFT, "SUM:", 240, 815, 270);

                        cb.ShowTextAligned(Element.ALIGN_LEFT, "IRN80084571", 90, 815, 270);
                        cb.ShowTextAligned(Element.ALIGN_LEFT, "FOR ARI SA", 65, 800, 270);
                        cb.ShowTextAligned(Element.ALIGN_LEFT, "Account Department", 40, 810, 270);
                        cb.ShowTextAligned(Element.ALIGN_LEFT, "TAX FREE INTRACOMMUNITY DELIVERY", 20, 810, 270);
                        issue = "614";
                        //Supplier
                        cb.SetFontAndSize(nf, 8);
                        cb.ShowTextAligned(Element.ALIGN_LEFT, palletCardLidlDTOs.FirstOrDefault().Supplier, 532, 800, 270);
                        cb.ShowTextAligned(Element.ALIGN_LEFT, "63080 VIPE Lakkoma", 522, 800, 270);
                        cb.ShowTextAligned(Element.ALIGN_LEFT, "Halkidiki, P.O. Box 303", 512, 800, 270);
                        cb.ShowTextAligned(Element.ALIGN_LEFT, "VAT Number:  " + "EL099774239", 502, 800, 270);
                        cb.ShowTextAligned(Element.ALIGN_LEFT, "Tel:  " + "0030 23990 20127", 492, 800, 270);
                        cb.ShowTextAligned(Element.ALIGN_LEFT, "Fax:  " + "0030 23990 20129", 482, 800, 270);
                        cb.ShowTextAligned(Element.ALIGN_LEFT, "EBEX:  " + "13405/5-3-02", 472, 800, 270);
                        cb.SetFontAndSize(nf, 10);
                        cb.ShowTextAligned(Element.ALIGN_LEFT, "Supplier number:   " + "10002", 455, 800, 270);
                        cb.SetFontAndSize(nf, 8);
                        //Goods recipient
                        cb.ShowTextAligned(Element.ALIGN_LEFT, palletCardLidlDTOs.FirstOrDefault().ShipToParty.Name2, 427, 800, 270);
                        cb.ShowTextAligned(Element.ALIGN_LEFT, palletCardLidlDTOs.FirstOrDefault().ShipToParty.Address, 417, 800, 270);
                        cb.ShowTextAligned(Element.ALIGN_LEFT, palletCardLidlDTOs.FirstOrDefault().ShipToParty.Street, 407, 800, 270);
                        cb.ShowTextAligned(Element.ALIGN_LEFT, palletCardLidlDTOs.FirstOrDefault().ShipToParty.Country, 397, 800, 270);
                        //Items
                        int alignX = 320;
                        foreach (var materialData in palletCardLidlDTOs.GroupBy(k => new { k.Material.MaterialId, k.Pallet.PalletLOT}).ToList())
                        {
                            cb.ShowTextAligned(Element.ALIGN_LEFT, "124552", 320, 815, 270);
                            cb.ShowTextAligned(Element.ALIGN_LEFT, materialData.FirstOrDefault().Material.MaterialDescription, alignX, 770, 270);
                            cb.ShowTextAligned(Element.ALIGN_LEFT, "12.00??", alignX, 600, 270);
                            cb.ShowTextAligned(Element.ALIGN_LEFT, materialData.FirstOrDefault().Pallet.PalletLOT.ToString(), alignX, 555, 270);
                            cb.ShowTextAligned(Element.ALIGN_LEFT, materialData.FirstOrDefault().DeliveryDate.ToString("dd.MM.yyyy"), alignX, 500, 270);
                            cb.ShowTextAligned(Element.ALIGN_LEFT, (materialData.FirstOrDefault().Material.PalletFactor * materialData.Count()).ToString(), alignX, 420, 270);
                            cb.ShowTextAligned(Element.ALIGN_LEFT, materialData.Count().ToString(), alignX, 360, 270);
                            cb.ShowTextAligned(Element.ALIGN_LEFT, materialData.FirstOrDefault().Material.PalletFactor.ToString(), alignX, 300, 270);
                            cb.ShowTextAligned(Element.ALIGN_LEFT, materialData.FirstOrDefault().Material.NetWeight.ToString(), alignX, 240, 270);
                            cb.ShowTextAligned(Element.ALIGN_LEFT, materialData.FirstOrDefault().Material.GrossWeight.ToString(), alignX, 160, 270);
                            cb.ShowTextAligned(Element.ALIGN_LEFT, (materialData.FirstOrDefault().Material.GrossWeight * materialData.Count()).ToString(), alignX, 90, 270);
                            alignX = alignX - 10;
                        }
                        //SUM
                        cb.ShowTextAligned(Element.ALIGN_LEFT, palletCardLidlDTOs.Count().ToString(), 240, 360, 270);
                        cb.ShowTextAligned(Element.ALIGN_LEFT, palletCardLidlDTOs.FirstOrDefault().Material.PalletFactor.ToString(), 240, 300, 270);
                        cb.ShowTextAligned(Element.ALIGN_LEFT, (palletCardLidlDTOs.FirstOrDefault().Material.GrossWeight * palletCardLidlDTOs.Count()).ToString(), 240, 90, 270);

                        //SubItem
                        //int alignX = 300;
                        //foreach (var subItems in lFGDTO.ProductItems[0].DeliveryProductSubItem)
                        //{
                        //    cb.ShowTextAligned(Element.ALIGN_LEFT, subItems.ItemDescription, alignX, 770, 270);
                        //    cb.ShowTextAligned(Element.ALIGN_LEFT, subItems.CaseSize.ToString(), alignX, 600, 270);
                        //    cb.ShowTextAligned(Element.ALIGN_LEFT, subItems.CaseQuantity.ToString(), alignX, 420, 270);
                        //    alignX = alignX - 10;
                        //}
                        cb.ShowTextAligned(Element.ALIGN_LEFT, "Palette notes will not be handed-out", 220, 800, 270);
                        cb.ShowTextAligned(Element.ALIGN_LEFT, "Tsiolakis Aristovoulos", 50, 810, 270);
                        issue = "615";
                        cb.EndText();
                    }
                    issue = "616";
                    document.Close();
                    fs.Close();
                    writer.Close();
                    pdfReader.Close();
                    issue = "617";
                }

                //Upload to cloud
                bool isUploaded = await ContainerService.UploadOperationAsync(Path.Combine(_environment.WebRootPath, "pdf-files") + $@"\", PDFNewFileName, AzureGPV2Credentials.CONTAINER_PDF_FILES);
                issue = "618";
            }
            catch(Exception ex) {
                issue = ex.Message;
            }
        }
        private async Task GenerateDriverCMR(List<PalletCardLidlDTO> palletCardLidlDTOs, string randomIndex, CMRType cMRType)
        {
            string PDFNewFileName = "CMR" + randomIndex + ".pdf";
            string templateDirectory = Path.Combine(_environment.WebRootPath, "pdf-templates") + $@"\" + PDFTemplateUrl.CMR_LIDL_NAME;
            string filePathDirectory = Path.Combine(_environment.WebRootPath, "pdf-files") + $@"\" + PDFNewFileName;
            try
            {
                using (var templateFileStream = new FileStream(templateDirectory, FileMode.Open))
                {
                    // Open existing PDF
                    var pdfReader = new PdfReader(templateFileStream);
                    iTextSharp.text.Rectangle size = pdfReader.GetPageSizeWithRotation(1);
                    iTextSharp.text.Document document = new iTextSharp.text.Document(size);

                    // open the writer
                    FileStream fs = new FileStream(filePathDirectory, FileMode.Create, FileAccess.Write);
                    PdfWriter writer = PdfWriter.GetInstance(document, fs);
                    document.Open();

                    // X6 copies
                    for (int i = 0; i < 6; i++)
                    {
                        document.NewPage();

                        // the pdf content
                        PdfContentByte cb = writer.DirectContent;

                        // create the new page and add it to the pdf
                        PdfImportedPage page = writer.GetImportedPage(pdfReader, 1);
                        cb.AddTemplate(page, 0, 0);

                        // select the font properties
                        BaseFont bf = BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                        cb.SetColorFill(BaseColor.DARK_GRAY);
                        cb.SetFontAndSize(bf, 10);

                        //CMR Texts
                        //Sender
                        cb.BeginText();
                        cb.ShowTextAligned(1, palletCardLidlDTOs.FirstOrDefault().Supplier, 260, 965, 0);
                        cb.ShowTextAligned(1, "Industrial Area of Lakkoma", 260, 952, 0);
                        cb.ShowTextAligned(1, "GR 63080, Halkidiki", 260, 939, 0);
                        cb.ShowTextAligned(1, "Greece", 260, 926, 0);
                        //Consignee
                        cb.ShowTextAligned(1, "LIDL ROMANIA HQ", 260, 880, 0);
                        cb.ShowTextAligned(1, "STR.DN72, CRANGUL LUI BOT KM 73+810", 260, 867, 0);
                        cb.ShowTextAligned(1, "COUNTRY PRAHOVA, 107027", 260, 854, 0);
                        cb.ShowTextAligned(1, "ROMANIA", 260, 841, 0);
                        //Place Of Delivery
                        cb.ShowTextAligned(1, "LIDL, ROMANIA", 260, 800, 0);
                        cb.ShowTextAligned(1, "STRADA TIMISORII, NR 161", 260, 787, 0);
                        cb.ShowTextAligned(1, "LUGOJ, ROMANIA", 260, 774, 0);
                        //Place and Date of taking
                        cb.ShowTextAligned(1, palletCardLidlDTOs.FirstOrDefault().Supplier, 260, 728, 0);
                        cb.ShowTextAligned(1, "Industrial Area of Lakkoma", 260, 715, 0);
                        cb.ShowTextAligned(1, "GR 63080, Halkidiki", 260, 702, 0);
                        cb.ShowTextAligned(1, "Greece", 260, 689, 0);
                        //Delivery Note
                        cb.ShowTextAligned(1, "DA A 1 79199 79209", 165, 645, 0);
                        //Reference
                        cb.ShowTextAligned(1, palletCardLidlDTOs.FirstOrDefault().OrderNumber, 155, 490, 0);
                        //Temporary Storage to
                        cb.ShowTextAligned(1, "Alexander Logistics - 9A, Treti Mart, 1532, Kazichane", 300, 393, 0);
                        //Transport Temperature
                        cb.ShowTextAligned(1, "0 -+4°C", 175, 342, 0);
                        //Established in
                        cb.ShowTextAligned(1, "Thessaloniki / Greece", 180, 251, 0);
                        //Established on
                        cb.ShowTextAligned(1, DateTime.Now.ToString("dd/MM/yyyy"), 400, 251, 0);
                        //Carrier [23]
                        cb.ShowTextAligned(1, "E4238KH/E4210EE", 440, 230, 0);
                        //Carrier [16]
                        cb.ShowTextAligned(1, "E4238KH/E4210EE", 600, 867, 0);

                        //Product Items
                        //Marks and nos
                        int alignY = 550;
                        foreach (var materialData in palletCardLidlDTOs.GroupBy(k => k.Material.MaterialId).ToList())
                        {
                            cb.ShowTextAligned(Element.ALIGN_LEFT, materialData.FirstOrDefault().Material.MaterialDescription, 50, alignY, 0);
                            cb.ShowTextAligned(1, (materialData.FirstOrDefault().Material.PalletFactor * materialData.Count()).ToString(), 310, alignY, 0);
                            cb.ShowTextAligned(1, materialData.Count().ToString(), 420, alignY, 0);
                            cb.ShowTextAligned(1, (materialData.FirstOrDefault().Material.GrossWeight * materialData.Count()).ToString(), 675, alignY, 0);
                            alignY = alignY + 25;
                        }

                        //Total
                        cb.ShowTextAligned(1, palletCardLidlDTOs.Select(k => k.Material.GrossWeight).Sum().ToString(), 675, 480, 0);

                        cb.EndText();
                    }
                    document.Close();
                    fs.Close();
                    writer.Close();
                    pdfReader.Close();
                }

                //Upload to cloud
                bool isUploaded = await ContainerService.UploadOperationAsync(Path.Combine(_environment.WebRootPath, "pdf-files") + $@"\", PDFNewFileName, AzureGPV2Credentials.CONTAINER_PDF_FILES);
            }
            catch (Exception ex) 
            {

            }
        }

        private async Task GenerateLIDLOrdersDocuments(List<PalletCardLidlDTO> palletCardLidlDTOs, string randomIndex)
        {
            //Headers
            string headerFileName = "ARI_DESADV_HEADER_" + randomIndex + ".txt";
            string filePathDirectory = Path.Combine(_environment.WebRootPath, "lidl-orders") + $@"\" + headerFileName;

            using (var tw = new StreamWriter(filePathDirectory, true))
            {
                StringBuilder sb = new StringBuilder();
                foreach (var order in palletCardLidlDTOs.GroupBy(k=>k.Order).Select(k=> k.Key).ToList())
                {
                    sb.AppendFormat("{1}{0}{2}{0}{3}{0}{4}{0}{5}{0}{6}{0}{7}{0}{8}{0}{9}{0}{10}{0}{11}{0}{12}{0}{13}", "\t", "5209999001363", "4333685000000", order.DocumentDate.ToString("yyyyMMdd"), order.PurchaseOrderDate.ToString("yyyyMMdd"),
                        order.PurchaseOrderDate.ToString("yyyyMMdd"), order.ReferenceNumber, palletCardLidlDTOs.FirstOrDefault().DeliveryNoteNo, "4304493000018", order.ShipToParty.GTIN, order.SoldToParty.GTIN, order.SoldToParty.GTIN, order.SoldToParty.GTIN, "9099999131179");
                    sb.Append("\n");
                }

                tw.WriteLine(sb);
            }

            //Lines
            string lineFileName = "ARI_DESADV_LINES_" + randomIndex + ".txt";
            filePathDirectory = Path.Combine(_environment.WebRootPath, "lidl-orders") + $@"\" + lineFileName;

            using (var tw = new StreamWriter(filePathDirectory, true))
            {
                StringBuilder sb = new StringBuilder();
                foreach (var materialData in palletCardLidlDTOs.GroupBy(k => k.Material.MaterialId).ToList())
                {
                    sb.AppendFormat("{1}{0}{2}{0}{3}{0}{4}{0}{5}{0}{6}{0}{7}{0}{8}{0}{9}{0}{10}{0}{11}{0}{12}{0}{13}{0}{14}", "\t", "0080105819", materialData.Count(), "4052917418641", materialData.FirstOrDefault().Material.MaterialDescription, materialData.FirstOrDefault().SumPallet,
                        materialData.FirstOrDefault().Pallet.PalletLOT, materialData.FirstOrDefault().DeliveryDate.ToString("yyyyMMdd"), materialData.FirstOrDefault().Material.PalletFactor, materialData.Count(), (materialData.FirstOrDefault().Material.PalletFactor * materialData.Count()).ToString(),
                        materialData.FirstOrDefault().Material.NetWeight.ToString(), materialData.FirstOrDefault().Material.GrossWeight.ToString(), (materialData.FirstOrDefault().Material.GrossWeight * materialData.Count()).ToString(), "00152069720003883682");
                    sb.Append("\n");
                }
                tw.WriteLine(sb);
            }

            //Upload to cloud
            bool isheaderUploaded = await ContainerService.UploadOperationAsync(Path.Combine(_environment.WebRootPath, "lidl-orders") + $@"\", headerFileName, AzureGPV2Credentials.CONTAINER_LIDL_ORDERS);
            bool islineUploaded = await ContainerService.UploadOperationAsync(Path.Combine(_environment.WebRootPath, "lidl-orders") + $@"\", lineFileName, AzureGPV2Credentials.CONTAINER_LIDL_ORDERS);
        }
    }
}
*/
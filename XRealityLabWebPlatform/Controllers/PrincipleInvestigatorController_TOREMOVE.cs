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

namespace XRealityLab.Controllers
{
    /*[Authorize(Roles = "Υπεύθυνος Παραγωγής,Επόπτης Λογιστής,Υπεύθυνος Ροών Εταιρίας")]*/
    public class PrincipleInvestigatorController_TOREMOVE : Controller
    {
        private readonly XRealityLabdbContext _context;
        private IUserRepository _userRepository;
        private readonly IHostingEnvironment _environment;
        private readonly CustomIDataProtection protector;


        public PrincipleInvestigatorController_TOREMOVE(XRealityLabdbContext context, IHostingEnvironment IHostingEnvironment, CustomIDataProtection customIDataProtection)
        {
            _context = context;
/*            _userRepository = new UserRepository(_context);
*/            _environment = IHostingEnvironment;
            protector = customIDataProtection;
        }

        //Actions


        public async Task<IActionResult> PrincipleInvestigators(string actionMessage)
        {
            return View();
        }
        public async Task<IActionResult> ResearchAssistants(string actionMessage)
        {
            return View();
        }








        /* public async Task<IActionResult> Orders(string actionMessage)
         {
             List<EDIOrderDTO> orderDtos = await _context.Orders.Include(k => k.Material).Select(k => new EDIOrderDTO
             {
                 OrderId = k.OrderId,
                 PassToken = protector.Decode(k.OrderId.ToString()),
                 ReferenceNumber = k.ReferenceNumber,
                 Material = new MaterialDTO {
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
             ViewBag.actionMessage = actionMessage;
             return View(orderDtos);
         }
         public IActionResult AddEDIFile()
         {
             return View();
         }
         [HttpPost, ValidateAntiForgeryToken]
         public IActionResult AddEDIFile(IFormFile file)
         {
             if (file == null) return View();

             EdiDocument ediDocument = EdiDocument.Load(file.OpenReadStream());
             List<EDIOrderDTO> eDIOrdersDTOs = new List<EDIOrderDTO>();
             foreach (var transactionSet in ediDocument.TransactionSets)
             {
                 eDIOrdersDTOs.AddRange(GetOrderRegistries(transactionSet, file.FileName));
             }
             return View(eDIOrdersDTOs);
         }


         //Events
         [HttpPost]
         public JsonResult UploadEDIFiles(IFormCollection data, IFormFile ediFile)
         {
             if (ModelState.IsValid)
             {
                 if (ediFile == null) return null;

                 EdiDocument ediDocument = EdiDocument.Load(ediFile.OpenReadStream());
                 List<EDIOrderDTO> eDIOrdersDTOs = new List<EDIOrderDTO>();
                 foreach (var transactionSet in ediDocument.TransactionSets)
                 {
                     var eDIOrdersDTO = GetOrderRegistries(transactionSet, ediFile.FileName);
                     eDIOrdersDTOs.AddRange(eDIOrdersDTO);
                 }
                 return Json(eDIOrdersDTOs);
             }
             return null;
         }
         [HttpPost]
         public async Task<JsonResult> SaveOrder(string _orders, string selectedItem)
         {
             var ordersDTO = JsonConvert.DeserializeObject<List<EDIOrderDTO>>(_orders);

             if (selectedItem == "all")
             {
                 List<Order> orders = new List<Order>();
                 foreach (var orderDTO in ordersDTO)
                 {
                     Order order = new Order
                     {
                         ReferenceNumber = orderDTO.ReferenceNumber,
                         MaterialId = orderDTO.Material.MaterialId,
                         Quantity = orderDTO.Quantity,
                         DocumentDate = orderDTO.DocumentDate,
                         PurchaseOrderDate = orderDTO.PurchaseOrderDate,
                         RequestedDeliveryDate = orderDTO.RequestedDeliveryDate,
                         FileName = orderDTO.FileName,
                         InsertionDateTime = DateTime.Now,
                         Status = 0,
                         LastModificationDateTime = DateTime.Now
                     };
                     if (orderDTO.ShipToParty.CustomerId != 0)
                         order.ShipToParty = orderDTO.ShipToParty.CustomerId;
                     if (orderDTO.SoldToParty.CustomerId != 0)
                         order.SoldToParty = orderDTO.SoldToParty.CustomerId;

                     orders.Add(order);
                 }
                 await _context.AddRangeAsync(orders);
                 await _context.SaveChangesAsync();
                 return new JsonResult("all");
             }
             else
             {
                 var orderToAdd = ordersDTO[int.Parse(selectedItem.Remove(0, 1))];
                 Order order = new Order
                 {
                     ReferenceNumber = orderToAdd.ReferenceNumber,
                     Quantity = orderToAdd.Quantity,
                     DocumentDate = orderToAdd.DocumentDate,
                     PurchaseOrderDate = orderToAdd.PurchaseOrderDate,
                     RequestedDeliveryDate = orderToAdd.RequestedDeliveryDate,
                     FileName = orderToAdd.FileName,
                     InsertionDateTime = DateTime.Now,
                     Status = 0,
                     LastModificationDateTime = DateTime.Now
                 };
                 if (orderToAdd.ShipToParty.CustomerId != 0)
                     order.ShipToParty = orderToAdd.ShipToParty.CustomerId;
                 if (orderToAdd.SoldToParty.CustomerId != 0)
                     order.SoldToParty = orderToAdd.SoldToParty.CustomerId;
                 if (orderToAdd.Material.MaterialId != 0)
                     order.MaterialId = orderToAdd.Material.MaterialId;
                 try
                 {
                     await _context.AddAsync(order);
                     await _context.SaveChangesAsync();
                 }
                 catch (Exception ex)
                 {
                 }
                 return new JsonResult("ok");
             }

         }
         public async Task<IActionResult> DeleteOrder(string passToken)
         {
             if (passToken == null) NotFound();

             var order = await _context.Orders.FindAsync(int.Parse(protector.Encode(passToken)));
             if (order.Status ==2)
                 return RedirectToAction("Orders", "Logistics", new { actionMessage = "Δεν επιτρέπεται η διαγραφή παραγγ. που έχει γίνει φόρτωση" + order.OrderId.ToString() });
            if (order.Status == 1)
             {
                 var loadOrder = _context.LoadOrders.Where(k => k.OrderId == order.OrderId).FirstOrDefault();
                 _context.Loads.Where(k => k.LoadId == loadOrder.LoadId).FirstOrDefault().IsMatch = 0;
                 _context.LoadOrders.Remove(loadOrder);
             }
             _context.Orders.Remove(order);

             await _context.SaveChangesAsync();
             return RedirectToAction("Orders", "Logistics", new { actionMessage = "Διαγράφτηκε η εγγραφή με Α/Α ΠΑ" + order.OrderId.ToString() });
         }

         //Helpers
         private List<EDIOrderDTO> GetOrderRegistries(EdiTransactionSet ediTransactionSet, string fileName)
         {
             var lins = ediTransactionSet.Segments.Where(k => k.Id == "LIN").ToList();
             var qtys = ediTransactionSet.Segments.Where(k => k.Id == "QTY").ToList();
             var pacs = ediTransactionSet.Segments.Where(k => k.Id == "PAC").ToList();

             List<EDIOrderDTO> ediOrders = new List<EDIOrderDTO>();
             List<FailedEDIDTO> failedEDIDTOs = new List<FailedEDIDTO>();
             for (int i = 0; i < lins.Count(); i++)
             {
                 EDIOrderDTO ediOrderDTO = new EDIOrderDTO();
                 ediOrderDTO.ReferenceNumber = ediTransactionSet.Segments[1].Elements[1].Value;

                 Material1 material = _context.Materials1.Include(k => k.Country).Where(k => k.Gtin == lins[i].Elements[2].Components[0].Value).FirstOrDefault();

                 var ucShipToParty = ediTransactionSet.Segments.Where(k => k.Id == "NAD").ToList().Where(k => k.Elements[0].Value == "UC").FirstOrDefault().Elements[1].Components[0].Value;
                 Customer1 customerShipToParty = _context.Customers1.Where(k => k.LocationNo1 == ucShipToParty.Substring(0, 7)
                                 && k.LocationNo2 == ucShipToParty.Substring(7, ucShipToParty.Length - 1 - 7).TrimStart('0').PadLeft(1, '0') && k.CheckDigit == ucShipToParty.Substring(ucShipToParty.Length - 1, 1)).FirstOrDefault();
                 if(customerShipToParty == null)
                 {
                     failedEDIDTOs.Add(new FailedEDIDTO
                     {
                         ReferenceNumber = ediOrderDTO.ReferenceNumber,
                         Item = lins[i].Elements[2].Components[0].Value,
                         Message = "Απέτυχε να σαρωθεί το αντικείμενο με reference number " + ediOrderDTO.ReferenceNumber
                         + ", γιατί δεν βρέθηκε πελάτης με Location1: " + ucShipToParty.Substring(0,7) + ", Location2: " + ucShipToParty.Substring(7, ucShipToParty.Length-1-7).TrimStart('0').PadLeft(1,'0') + ", CheckDigit: " + ucShipToParty.Substring(ucShipToParty.Length - 1, 1)
                     });
                 }
                 var bySoldToParty = ediTransactionSet.Segments.Where(k => k.Id == "NAD").ToList().Where(k => k.Elements[0].Value == "BY").FirstOrDefault().Elements[1].Components[0].Value;
                 Customer1 customerSoldToParty = _context.Customers1.Where(k => k.LocationNo1 == bySoldToParty.Substring(0, 7)
                 && k.LocationNo2 == bySoldToParty.Substring(7, bySoldToParty.Length - 1 - 7).TrimStart('0').PadLeft(1, '0') && k.CheckDigit == bySoldToParty.Substring(bySoldToParty.Length - 1, 1)).FirstOrDefault();
                 if (customerSoldToParty == null)
                 {
                     failedEDIDTOs.Add(new FailedEDIDTO
                     {
                         ReferenceNumber = ediOrderDTO.ReferenceNumber,
                         Item = lins[i].Elements[2].Components[0].Value,
                         Message = "Απέτυχε να σαρωθεί το αντικείμενο με reference number " + ediOrderDTO.ReferenceNumber
                         + ", γιατί δεν βρέθηκε πελάτης με Location1: " + bySoldToParty.Substring(0, 7) + ", Location2: " + bySoldToParty.Substring(7, bySoldToParty.Length - 1 - 7).TrimStart('0').PadLeft(1, '0') + ", CheckDigit: " + bySoldToParty.Substring(bySoldToParty.Length - 1, 1)
                     });
                 }
                 if (material == null)
                 {
                     failedEDIDTOs.Add(new FailedEDIDTO
                     {
                         ReferenceNumber = ediOrderDTO.ReferenceNumber,
                         Item = lins[i].Elements[2].Components[0].Value,
                         Message = "Απέτυχε να σαρωθεί το αντικείμενο με reference number " + ediOrderDTO.ReferenceNumber
                         + ", γιατί δεν βρέθηκε προιον με κωδικό GTIN " + lins[i].Elements[2].Components[0].Value
                     });
                     continue;
                 }

                 ediOrderDTO.Material = new MaterialDTO
                 {
                     MaterialId = material != null ? material.MaterialId : 0,
                     MaterialDescription = material != null ? material.MaterialDescription : "invalid",
                     PackagingLineDescription = material != null ? material.PackagingLineDescription : "invalid",
                     MaterialSAPID = material != null ? material.MaterialSapid : "invalid",
                     GTIN = lins[i].Elements[2].Components[0].Value
                 };

                 ediOrderDTO.Quantity = material != null ? int.Parse(qtys[i].Elements[0].Components[1].Value) / material.PalletFactor : int.Parse(qtys[i].Elements[0].Components[1].Value);
                 ediOrderDTO.NoPalletBox = material != null ? material.PalletFactor : 0;
                 ediOrderDTO.DocumentDate = ediTransactionSet.Segments[2].Elements[0].Components[1].DateValue;
                 ediOrderDTO.PurchaseOrderDate = ediTransactionSet.Segments[5].Elements[0].Components[1].DateValue;
                 ediOrderDTO.RequestedDeliveryDate = DateTime.ParseExact(ediTransactionSet.Segments[3].Elements[0].Components[1].Value.Substring(0, 8), "yyyyMMdd", null);
                 ediOrderDTO.ShipToParty = new CustomerDTO
                 {
                     CustomerId = customerShipToParty != null ? customerShipToParty.CustomerId : 0,
                     CustomerSAPID = customerShipToParty != null ? customerShipToParty.CustomerSapid : "invalid",
                     Name1 = customerShipToParty != null ? customerShipToParty.Name1 : "invalid",
                 };


                 ediOrderDTO.SoldToParty = new CustomerDTO
                 {
                     CustomerId = customerSoldToParty != null ? customerSoldToParty.CustomerId : 0,
                     CustomerSAPID = customerSoldToParty != null ? customerSoldToParty.CustomerSapid : "invalid",
                     Name1 = customerSoldToParty != null ? customerSoldToParty.Name1 : "invalid",
                 };

                 ediOrderDTO.FileName = fileName;
                 ediOrderDTO.ToAdd = false;

                 ediOrders.Add(ediOrderDTO);
             }
             if (failedEDIDTOs.Count() >= 1 && ediOrders.Count() >= 1)
                 ediOrders.FirstOrDefault().failedEDIDTOs = failedEDIDTOs;
             if (failedEDIDTOs.Count() >= 1 && ediOrders.Count() == 0)
             {
                 EDIOrderDTO ediOrderDTO = new EDIOrderDTO();
                 ediOrderDTO.ReferenceNumber = "";
                 ediOrderDTO.Material = new MaterialDTO
                 {
                     MaterialId = 0,
                     MaterialDescription = "invalid",
                     PackagingLineDescription = "invalid",
                     MaterialSAPID = "invalid",
                     GTIN = ""
                 };

                 ediOrderDTO.Quantity = 0;
                 ediOrderDTO.NoPalletBox = 0;
                 ediOrderDTO.DocumentDate = DateTime.MinValue;
                 ediOrderDTO.PurchaseOrderDate = DateTime.MinValue;
                 ediOrderDTO.RequestedDeliveryDate = DateTime.MinValue;
                 ediOrderDTO.ShipToParty = new CustomerDTO
                 {
                     CustomerId = 0,
                     CustomerSAPID = "invalid",
                     Name1 = "invalid",
                 };


                 ediOrderDTO.SoldToParty = new CustomerDTO
                 {
                     CustomerId = 0,
                     CustomerSAPID = "invalid",
                     Name1 = "invalid",
                 };

                 ediOrderDTO.FileName = fileName;
                 ediOrderDTO.ToAdd = false;

                 ediOrders.Add(ediOrderDTO);
                 ediOrders.FirstOrDefault().failedEDIDTOs = failedEDIDTOs;
             }
             return ediOrders;
         }



         //Dev
         public IActionResult DeleteOrderData()
         {
             var orders = _context.Orders.ToList();
             _context.RemoveRange(orders);
             _context.SaveChanges();
             return RedirectToAction("Orders", "Logistics");
         }








         public IActionResult AddOrderManually(string actionMessage)
         {
             EDIOrderDTO eDIOrderDTO = new EDIOrderDTO();

             var materialListItems = new List<SelectListItem>();
             materialListItems.Add(new SelectListItem { Text = "-", Value = "-" });
             materialListItems.AddRange(_context.Materials1.ToList().GroupBy(X => X.MaterialSapid.TrimStart('0')).Select(X => X.FirstOrDefault()).ToList().Select(k => new SelectListItem { Text = k.MaterialSapid.TrimStart('0') + " " + k.MaterialDescription, Value = k.MaterialId.ToString() }));
             materialListItems = materialListItems.OrderBy(k => k.Text).ToList();
             eDIOrderDTO.MaterialListItems = materialListItems;
             eDIOrderDTO.SelectedMaterial = "-";

             var customerListItems = new List<SelectListItem>();
             customerListItems.Add(new SelectListItem { Text = "-", Value = "-" });
             customerListItems.AddRange(_context.Customers1.Where(k => k.CountryId != 6 *//*Greece*//* && (k.LocationNo1 + k.LocationNo2 + k.CheckDigit != "000")).Select(k => new SelectListItem
             {
                 Text = "SAPID: " + k.CustomerSapid + " | " + k.LocationNo1 + k.LocationNo2 +
                   k.CheckDigit + " " + k.Name1 + " | " + k.Name2 + " | ",
                 Value = k.CustomerId.ToString()
             }));
             customerListItems = customerListItems.OrderBy(k => k.Text).ToList();
             eDIOrderDTO.ShipToPartyListItems = customerListItems.Where(k => k.Text.Contains("3099") || k.Text == "-");
             eDIOrderDTO.SelectedShipToParty = "-";

             eDIOrderDTO.SoldToPartyListItems = customerListItems.Where(k => !k.Text.Contains("3099"));
             eDIOrderDTO.SelectedSoldToParty = "-";

             ViewBag.actionMessage = actionMessage;
             return View(eDIOrderDTO);
         }
         [HttpPost, ValidateAntiForgeryToken]
         public async Task<IActionResult> AddOrderManually([Bind("ReferenceNumber,SelectedMaterial,Quantity,SelectedShipToParty,SelectedSoldToParty," +
             "DocumentDate,PurchaseOrderDate,RequestedDeliveryDate")] EDIOrderDTO eDIOrderDTO)
         {
             if (eDIOrderDTO.ReferenceNumber == null || eDIOrderDTO.ReferenceNumber.Length < 3 || eDIOrderDTO.ReferenceNumber.Length > 20 || eDIOrderDTO.DocumentDate < DateTime.Now.AddYears(-20)
                 || eDIOrderDTO.PurchaseOrderDate < DateTime.Now.AddYears(-20) || eDIOrderDTO.RequestedDeliveryDate < DateTime.Now.AddYears(-20)
                 || eDIOrderDTO.SelectedMaterial == "-" || eDIOrderDTO.SelectedShipToParty == "-" || eDIOrderDTO.SelectedSoldToParty == "-"
                 || eDIOrderDTO.Quantity == 0)
                 return RedirectToAction(nameof(AddOrderManually), new { actionMessage = "Δεν προσθέσατε τα πεδία σωστά. Παρακαλώ προσπαθήστε ξανά." });

             Order order = new Order
             {
                 ReferenceNumber = eDIOrderDTO.ReferenceNumber,
                 MaterialId = int.Parse(eDIOrderDTO.SelectedMaterial),
                 Quantity = eDIOrderDTO.Quantity,
                 ShipToParty = int.Parse(eDIOrderDTO.SelectedShipToParty),
                 SoldToParty = int.Parse(eDIOrderDTO.SelectedSoldToParty),
                 DocumentDate = eDIOrderDTO.DocumentDate,
                 PurchaseOrderDate = eDIOrderDTO.PurchaseOrderDate,
                 RequestedDeliveryDate = eDIOrderDTO.RequestedDeliveryDate,
                 Status = 0,
                 InsertionDateTime = DateTime.Now,
                 FileName = "no file",
                 LastModificationDateTime = DateTime.Now
             };

             _context.Add(order);
             await _context.SaveChangesAsync();

             return RedirectToAction(nameof(Orders), new { actionMessage = "Προστέθηκε η εγγραφή με επιτυχία" });
         }


         //Edit
         public IActionResult EditOrder(string passToken, string actionMessage)
         {
             if (passToken == null || passToken == string.Empty) return NotFound();

             var materialListItem = new List<SelectListItem>();
             materialListItem.AddRange(_context.Materials1.Select(k => new SelectListItem { Text = k.MaterialSapid.TrimStart('0') + " " + k.MaterialDescription, Value = k.MaterialId.ToString() }));
             materialListItem = materialListItem.OrderBy(k => k.Text).ToList();

             var customerListItems = new List<SelectListItem>();
             customerListItems.Add(new SelectListItem { Text = "-", Value = "-" });
             customerListItems.AddRange(_context.Customers1.Where(k => k.CountryId != 6 *//*Greece*//* && (k.LocationNo1 + k.LocationNo2 + k.CheckDigit != "000")).Select(k => new SelectListItem
             {
                 Text = "SAPID: " + k.CustomerSapid + " | " + k.LocationNo1 + k.LocationNo2 +
                 k.CheckDigit + " " + k.Name1 + " | " + k.Name2 + " | ",
                 Value = k.CustomerId.ToString()
             }));
             customerListItems = customerListItems.OrderBy(k => k.Text).ToList();

             var statusListItems = new List<SelectListItem>();
             statusListItems.Add(new SelectListItem { Text = "Προστέθηκε", Value = Convert.ToString((int)OrderStatus.Created) });
             statusListItems.Add(new SelectListItem { Text = "Έγινε picking", Value = Convert.ToString((int)OrderStatus.Picking) });
             statusListItems.Add(new SelectListItem { Text = "Ολοκληρώθηκε", Value = Convert.ToString((int)OrderStatus.Completed) });
             statusListItems.Add(new SelectListItem { Text = "Άκυρο", Value = Convert.ToString((int)OrderStatus.Cancel) });

             var orderDto = _context.Orders.Include(k => k.Material).Where(k => k.OrderId == int.Parse(protector.Encode(passToken))).Select(k => new EDIOrderDTO
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
                 MaterialListItems = materialListItem,
                 SelectedMaterial = k.Material.MaterialId.ToString(),
                 Quantity = k.Quantity,
                 ShipToPartyListItems = customerListItems,
                 SelectedShipToParty = k.ShipToParty.ToString(),
                 ShipToParty = _context.Customers1.Where(x=> x.CustomerId == k.ShipToParty).Select(x => new CustomerDTO 
                 { 
                     Name1 = x.Name1,
                     SearchTermCode = x.SearchTermsCode
                 }).FirstOrDefault(),
                 SoldToPartyListItems = customerListItems,
                 SelectedSoldToParty = k.SoldToParty.ToString(),
                 SoldToParty = _context.Customers1.Where(x => x.CustomerId == k.SoldToParty).Select(x => new CustomerDTO
                 {
                     Name1 = x.Name1,
                     SearchTermCode = x.SearchTermsCode
                 }).FirstOrDefault(),
                 DocumentDate = k.DocumentDate,
                 PurchaseOrderDate = k.PurchaseOrderDate,
                 RequestedDeliveryDate = k.RequestedDeliveryDate,
                 SelectedStatus = Convert.ToString(k.Status),
                 StatusListItems = statusListItems,
                 FileName = k.FileName,
                 InsertionDateTime = k.InsertionDateTime,
                 LastModificationDateTime = k.LastModificationDateTime,
                 ActionMessage = (actionMessage != null) ? actionMessage : null
             }).FirstOrDefault();

             return View(orderDto);
         }




         //ORDER Methods//
         [HttpPost]
         public JsonResult GetCustomerTerm(string customerId)
         {
             var customer = _context.Customers1.Where(k => k.CustomerId == int.Parse(customerId)).FirstOrDefault();
             return new JsonResult(customer.SearchTermsCode);
         }

         [HttpPost]
         public JsonResult UpdateEDIOrderValue(string category, string passToken, string selectedItem)
         {
             var order = _context.Orders.Where(k => k.OrderId == int.Parse(protector.Encode(passToken))).FirstOrDefault();

             if (category == "refnum-save")
                 order.ReferenceNumber = selectedItem;
             else if (category == "status-save")
                 order.Status = int.Parse(selectedItem);
             else if (category == "material-save")
                 order.MaterialId = int.Parse(selectedItem);
             else if (category == "quant-save")
                 order.Quantity = int.Parse(selectedItem);
             else if (category == "shiptoparty-save")
                 order.ShipToParty = int.Parse(selectedItem);
             else if (category == "soldtoparty-save")
                 order.SoldToParty = int.Parse(selectedItem);
             if (category == "documentdate-save")
                 order.DocumentDate = DateTime.Parse(selectedItem);
             else if (category == "purchdate-save")
                 order.PurchaseOrderDate = DateTime.Parse(selectedItem);
             else if (category == "requestdate-save")
                 order.RequestedDeliveryDate = DateTime.Parse(selectedItem);

             order.LastModificationDateTime = DateTime.Now;

             _context.Update(order);
             _context.SaveChanges();

             return new JsonResult("ok");
         }
 */













        ///////////////
        //OLD
        /*  [HttpPost]
          public JsonResult UpdatedOrderMaterialValue(string passToken, string selectedItem, string materialId, string quantity)
          {
              //var orderMaterial = _context.OrderMaterials.Where(k => k.OrderMaterialId == int.Parse(selectedItem)).FirstOrDefault();
              //orderMaterial.MaterialId = int.Parse(materialId);
              //orderMaterial.Quantity = int.Parse(quantity);

              //_context.Update(orderMaterial);
              //_context.SaveChangesAsync();

              return new JsonResult("ok");
          }
          [HttpPost]
          public JsonResult AddOrderMaterialValue(string passToken, string materialId, string quantity)
          {
              OrderMaterial orderMaterial = new OrderMaterial();
              orderMaterial.OrderId = int.Parse(protector.Encode(passToken));
              orderMaterial.MaterialId = int.Parse(materialId);
              orderMaterial.Quantity = int.Parse(quantity);

              _context.Add(orderMaterial);
              _context.SaveChangesAsync();

              return new JsonResult("ok");
          }
          [HttpPost]
          public JsonResult RemoveOrderMaterialValue(string passToken, string selectedItem)
          {
              //var orderMaterial = _context.OrderMaterials.Where(k => k.OrderMaterialId == int.Parse(selectedItem)).FirstOrDefault();
              //_context.Remove(orderMaterial);
              //_context.SaveChangesAsync();

              return new JsonResult("ok");
          }*/



        //Load orders and loads
        //public async Task<IActionResult> CreateDriverDocumentsAsync(string truckId)
        //{
        //    DriverPreLoadDTO driverPreLoadDTO = new DriverPreLoadDTO();
        //    driverPreLoadDTO.eDIOrderDTOs = new List<EDIOrderDTO>();

        //    //WarehouseDTO
        //    WarehouseDTO warehouseDTO = new WarehouseDTO();

        //    var truckListItems = new List<SelectListItem>();
        //    truckListItems.Add(new SelectListItem { Text = "-", Value = "-" });

        //    var truckIds = _context.Trucks.Where(k => Convert.ToBoolean(k.IsAuthorized)).Select(k => k.TruckId).ToList();
        //    for (int i = 0; i < truckIds.Count; i++)
        //        truckListItems.Add(new SelectListItem { Text = "ΦΠ" + truckIds[i], Value = truckIds[i].ToString() });

        //    warehouseDTO.OrderListItems = truckListItems;

        //    //Order
        //    var ediOrderListItem = new List<SelectListItem>();
        //    ediOrderListItem.AddRange(_context.Orders.Select(k => new SelectListItem { Text = k.ReferenceNumber + " " + k.DocumentDate.ToString("dd/MM/yyyy"), Value = k.OrderId.ToString() }));
        //    ediOrderListItem = ediOrderListItem.OrderBy(k => k.Text).ToList();

        //    //Get orders
        //    var orderIds = _context.TruckOrders.Select(k => k.OrderId).ToList();

        //    foreach (var orderId in orderIds)
        //    {
        //        var orderDto = _context.Orders.Where(k => k.OrderId == orderId).Select(k => new EDIOrderDTO
        //        {
        //            OrderId = k.OrderId,
        //            PassToken = protector.Decode(k.OrderId.ToString()),
        //            ReferenceNumber = k.ReferenceNumber,
        //            DocumentDate = k.DocumentDate,
        //        }).FirstOrDefault();
        //        driverPreLoadDTO.eDIOrderDTOs.Add(orderDto);
        //    }

        //    driverPreLoadDTO.warehouseDTO = warehouseDTO;
        //    driverPreLoadDTO.OrderListItems = ediOrderListItem;
        //    driverPreLoadDTO.selectedTruckId = (truckId != null) ? int.Parse(truckId) : 0;

        //    return View(driverPreLoadDTO);
        //}

        //Update Truck details
        //[HttpPost]
        //public async Task<JsonResult> UpdateTruckDetails(string truckValue)
        //{
        //    if (truckValue == null)
        //        return new JsonResult("issue");

        //    var truck = _context.Trucks.Include(k => k.TruckPallets).Where(k => k.TruckId == int.Parse(truckValue)).FirstOrDefault();

        //    List<WarehouseSuppliesDTO> warehouseSuppliesDTOs = new List<WarehouseSuppliesDTO>();

        //    var packagingLineForms = _context.PackagingLineForms.ToList();
        //    var pallets = _context.Pallets.Where(k => truck.TruckPallets.Select(l => l.PalletId).Contains(k.PalletId)).ToList();
        //    var palletsByMatLotIds = pallets.GroupBy(k => new { k.MaterialId, k.PalletLot }).Select(group => group.First()).ToList();

        //    var materials = _context.Materials.Include(k => k.Country).Where(x => pallets.Select(k => k.MaterialId).Contains(x.MaterialId));
        //    var countries = _context.Countries.ToList();

        //    foreach (var palletGroup in palletsByMatLotIds)
        //    {
        //        WarehouseSuppliesDTO warehouseSuppliesDTO = new WarehouseSuppliesDTO();
        //        var _material = materials.Where(k => k.MaterialId == palletGroup.MaterialId).FirstOrDefault();
        //        var creationDate = packagingLineForms.Where(k => k.MaterialId == palletGroup.MaterialId && k.PalletsLot == palletGroup.PalletLot).FirstOrDefault().FormCreationDate;
        //        warehouseSuppliesDTO.MaterialDTO = new MaterialDTO
        //        {
        //            MaterialId = _material.MaterialId,
        //            MaterialSAPID = _material.MaterialSapid,
        //            MaterialLOT = GetLot(creationDate),
        //            MaterialDescription = _material.MaterialDescription,
        //            PackagingLineDescription = _material.PackagingLineDescription,
        //            MaxDeliveryDate = GetMaxDeliveryDate(creationDate, _material.LagerDays),
        //            ExpirationDate = GetExpirationDate(creationDate, _material.ExpirationDays),
        //            LanguageCode = countries.Where(k => k.CountryId == _material.CountryId).FirstOrDefault().Code
        //        };
        //        warehouseSuppliesDTO.CreationDate = creationDate;
        //        var _pallets_4 = pallets.Where(k => k.MaterialId == warehouseSuppliesDTO.MaterialDTO.MaterialId && k.PalletLot == warehouseSuppliesDTO.MaterialDTO.MaterialLOT && k.Status == 4).ToList();

        //        List<SuppliesMetricsDTO> suppliesMetricsDTOs = new List<SuppliesMetricsDTO>();
        //        var suppliesMetricsDTO_epoik = new SuppliesMetricsDTO { Status = 4, Sum = _pallets_4.Count() };
        //        suppliesMetricsDTOs.Add(suppliesMetricsDTO_epoik);

        //        warehouseSuppliesDTO.suppliesMetricsDTOs = suppliesMetricsDTOs;

        //        warehouseSuppliesDTOs.Add(warehouseSuppliesDTO);
        //    }

        //    return Json(warehouseSuppliesDTOs);
        //}

        /* [HttpPost]*/
        /* public JsonResult AddOrderItem(string truckId, string orderId)
         {
             var truckOrder = new TruckOrder { OrderId = int.Parse(orderId), TruckId = int.Parse(truckId) };
             _context.Add(truckOrder);
             _context.SaveChangesAsync();

             return new JsonResult(truckId);
         }*/
        //[HttpPost]
        //public JsonResult RemoveOrderItem(string truckId, string orderId)
        //{
        //    //var truckOrder = _context.TruckOrders.Where(k => k.OrderId == int.Parse(orderId) && k.TruckId == int.Parse(truckId)).FirstOrDefault();
        //    //_context.Remove(truckOrder);
        //    //_context.SaveChangesAsync();

        //    //return new JsonResult(truckId);
        //}

        //Print invoice documents
        //[HttpPost]
        //public async Task<JsonResult> PrintDriverDocumentsAsync(string truckId)
        //{
        //    var truck = _context.Trucks.Where(k => k.TruckId == int.Parse(truckId)).FirstOrDefault();
        //    var truckOrders = _context.TruckOrders.Where(k => k.TruckId == truck.TruckId).ToList();
        //    var countries = _context.Countries.ToList();

        //    if (truckOrders.Count == 0)
        //        return new JsonResult("not ok");

        //    List<LFGDTO> lFGDTOs = new List<LFGDTO>();
        //    foreach (var truckOrder in truckOrders)
        //        lFGDTOs.Add(GenerateInvoiceData(truck, truckOrder, countries));


        //    //Print documents
        //    List<string> urlList = new List<string>();
        //    foreach (var LFGTO in lFGDTOs)
        //    {
        //        Random rnd = new Random();
        //        string PDFTemplateDirectory = Path.Combine(_environment.WebRootPath, "pdf-templates") + $@"\" + PDFTemplateUrl.LFG_LIDL_NAME;
        //        string PDFNewFileName = "LFG" + rnd.Next(0, 5000).ToString() + ".pdf";
        //        string PDFNewDirectory = Path.Combine(_environment.WebRootPath, "pdf-templates");
        //        string PDFNewFilePath = Path.Combine(_environment.WebRootPath, "pdf-templates") + $@"\" + PDFNewFileName;

        //        CreateDriverInvoice(PDFTemplateDirectory, PDFNewFilePath, LFGTO);
        //        bool isUploaded = await ContainerService.UploadOperationAsync(PDFNewDirectory, PDFNewFileName, AzureGPV2Credentials.CONTAINER_PDF_FILES);
        //        urlList.Add("https://arisa.blob.core.windows.net/pdf-files/" + PDFNewFileName);
        //    }
        //    return new JsonResult(urlList);
        //}

        //Print cmr documents
        //[HttpPost]
        //public async Task<JsonResult> PrintCMRDocuments(string truckId)
        //{
        //    var truck = _context.Trucks.Where(k => k.TruckId == int.Parse(truckId)).FirstOrDefault();
        //    var truckOrders = _context.TruckOrders.Where(k => k.TruckId == truck.TruckId).ToList();
        //    var countries = _context.Countries.ToList();

        //    if (truckOrders.Count == 0)
        //        return new JsonResult("not ok");

        //    List<CMRLidlDTO> cmrLidlDTOs = new List<CMRLidlDTO>();
        //    foreach (var truckOrder in truckOrders)
        //        cmrLidlDTOs.Add(GenerateCMRData(truck, truckOrder, countries));

        //    //Print documents
        //    List<string> urlList = new List<string>();
        //    foreach (var cMRLidlDTO in cmrLidlDTOs)
        //    {
        //        Random rnd = new Random();
        //        string PDFTemplateDirectory = Path.Combine(_environment.WebRootPath, "pdf-templates") + $@"\" + PDFTemplateUrl.CMR_LIDL_NAME;
        //        string PDFNewFileName = "CMR_LIDL" + rnd.Next(0, 5000).ToString() + ".pdf";
        //        string PDFNewDirectory = Path.Combine(_environment.WebRootPath, "pdf-templates");
        //        string PDFNewFilePath = Path.Combine(_environment.WebRootPath, "pdf-templates") + $@"\" + PDFNewFileName;

        //        string pdfDirectory = CreateDriverCMR(PDFTemplateDirectory, PDFNewFilePath, cMRLidlDTO, CMRType.Chiajna);
        //        bool isUploaded = await ContainerService.UploadOperationAsync(PDFNewDirectory, PDFNewFileName, AzureGPV2Credentials.CONTAINER_PDF_FILES);
        //        urlList.Add("https://arisa.blob.core.windows.net/pdf-files/" + PDFNewFileName);
        //    }
        //    return new JsonResult(urlList);
        //}









        //Helpers
        /*public static int GetLot(DateTime formDateTime)
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
        }*/

    }
}
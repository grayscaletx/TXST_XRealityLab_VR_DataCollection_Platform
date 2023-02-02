using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ARI.Models
{
    public class EDIOrderDTO
    {
        public int OrderId { get; set; }
        public string PassToken { get; set; }
        public string ReferenceNumber { get; set; }
        public string CountryCode { get; set; }
        public DateTime DocumentDate { get; set; }
        public DateTime PurchaseOrderDate { get; set; }
        public DateTime RequestedDeliveryDate { get; set; }
        public string FileName { get; set; }
        public DateTime InsertionDateTime { get; set; }
        public DateTime? LastModificationDateTime { get; set; }
        public string ControlReference { get; set; }

        public MaterialDTO Material { get; set; }
        public CustomerDTO ShipToParty { get; set; }
        public CustomerDTO SoldToParty { get; set; }
        [Range(0, 500, ErrorMessage = "Please enter valid integer Number")]
        public int Quantity { get; set; }
        public int NoPalletBox { get; set; }

        public IEnumerable<SelectListItem> StatusListItems { get; set; }
        [Display(Name = "Status")]
        public string SelectedStatus { get; set; }

        public IEnumerable<SelectListItem> MaterialListItems { get; set; }
        [Display(Name = "Material")]
        public string SelectedMaterial { get; set; }

        public IEnumerable<SelectListItem> ShipToPartyListItems { get; set; }
        [Display(Name = "ShipToParty")]
        public string SelectedShipToParty { get; set; }

        public IEnumerable<SelectListItem> SoldToPartyListItems { get; set; }
        [Display(Name = "SoldToParty")]
        public string SelectedSoldToParty { get; set; }

        public List<FailedEDIDTO> failedEDIDTOs { get; set; }

        public bool ToAdd { get; set; }
        public string ActionMessage { get; set; }

        public List<GroupOrderDTO> GroupOrderDTOs { get; set; }
        public bool IsSelectedForLoad { get; set; }
        public bool IsFiltered { get; set; }
    }

    public class GroupOrderDTO
    {
        public string MaterialSAPID { get; set; }
        public string MaterialDescription { get; set; }
        public int Quantity { get; set; }
    }

    public class FailedEDIDTO
    {
        public string ReferenceNumber { get; set; }
        public string Item { get; set; }
        public string Message { get; set; }
    }
}

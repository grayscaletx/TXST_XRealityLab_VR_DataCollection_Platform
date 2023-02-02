using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ARI.Models
{
    public class WarehouseDTO
    {
        public int WarehouseId { get; set; }
        public string PassToken { get; set; }
        public string CreationTime { get; set; }
        public List<PalletDTO> PalletDTOs { get; set; }

        public IEnumerable<SelectListItem> PalletListItems { get; set; }
        [Display(Name = "Αποθήκες")]
        public string SelectedPallet { get; set; }

        public IEnumerable<SelectListItem> WarehouseAreaListItems { get; set; }
        [Display(Name = "Αποθήκες")]
        public string SelectedWarehouseArea { get; set; }

        public IEnumerable<SelectListItem> PositionListItems { get; set; }
        [Display(Name = "Θέση")]
        public string SelectedPosition { get; set; }

        public IEnumerable<SelectListItem> OrderListItems { get; set; }
        [Display(Name = "Παραγγελία")]
        public string SelectedOrder { get; set; }

        public DateTime? MaxDeliveryDate { get; set; }

        public bool SucceededPastAction { get; set; }
    }
}

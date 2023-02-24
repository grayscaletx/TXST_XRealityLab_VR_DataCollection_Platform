using XRealityLab.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace XRealityLab.Models
{
    public class DriverPreLoadDTO
    {
        public WarehouseDTO warehouseDTO { get; set; }

        public IEnumerable<SelectListItem> OrderListItems { get; set; }
        [Display(Name = "Παραγγελία")]
        public string SelectedOrder { get; set; }

        public int selectedTruckId {get;set;}

        public List<EDIOrderDTO> eDIOrderDTOs { get; set; }
    }
}

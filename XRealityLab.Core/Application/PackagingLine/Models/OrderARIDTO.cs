using ARI.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace ARI.Models
{
    public class OrderARIDTO
    {
        public int orderARIId { get; set; }
        public List<EDIOrderDTO> EDIOrderDTOs { get; set; }
        public WarehouseDTO WarehouseDTO { get; set; }
        public DateTime CreationDateTime { get; set; }
        public bool IsAuthorized { get; set; }

        public bool SucceededPastAction { get; set; }
    }
}

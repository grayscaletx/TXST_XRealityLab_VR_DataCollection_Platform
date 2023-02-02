using System;
using System.Collections.Generic;
using System.Text;

namespace ARI.Models
{
    public class WarehouseTruckDTO
    {
        public List<int> PalletBtnIds { get; set; } 
        public DateTime CreationDateTime { get; set; }
        public bool IsAuthorized { get; set; }
    }
}

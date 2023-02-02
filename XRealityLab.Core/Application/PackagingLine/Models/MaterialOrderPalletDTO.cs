using System;
using System.Collections.Generic;
using System.Text;

namespace ARI.Models
{
    public class MaterialOrderPalletDTO
    {
        public MaterialDTO Material {get;set;}
        public int MaterialId {get;set; }
        public int NoOrders { get;set;}
        public int OrderPalletQuantity { get;set; }
        public int PalletQuantity  { get;set; }
    }

    public class MaterialPalletDTO
    {
        public int MaterialId { get; set; }
        public int NoOrders { get; set; }
        public int Quantity { get; set; }
    }
}

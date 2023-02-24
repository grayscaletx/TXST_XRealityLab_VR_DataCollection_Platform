using System;
using System.Collections.Generic;
using System.Text;

namespace XRealityLab.Models
{
    public class ManufacturingOperationDTO
    {
        public int OrdersLidl { get; set; }
        public int OrdersAri { get; set; }
        public int PackagingLine { get; set; }
        public int Pallets { get; set; }
        public int ReservedPallets { get; set; }
    }
}

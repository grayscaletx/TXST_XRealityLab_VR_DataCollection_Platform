using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class TruckPallet
    {
        public int TruckPalletId { get; set; }
        public int TruckId { get; set; }
        public int PalletId { get; set; }

        public virtual Pallet Pallet { get; set; }
        public virtual Truck Truck { get; set; }
    }
}

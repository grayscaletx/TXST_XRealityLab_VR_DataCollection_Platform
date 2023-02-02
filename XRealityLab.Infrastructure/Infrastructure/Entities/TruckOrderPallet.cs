using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class TruckOrderPallet
    {
        public int TruckOrderPalletId { get; set; }
        public int TruckId { get; set; }
        public int OrderId { get; set; }
        public int PalletId { get; set; }

        public virtual Order Order { get; set; }
        public virtual Pallet Pallet { get; set; }
        public virtual Truck Truck { get; set; }
    }
}

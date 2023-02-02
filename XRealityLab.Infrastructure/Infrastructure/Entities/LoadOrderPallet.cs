using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class LoadOrderPallet
    {
        public int LoadOrderPalletId { get; set; }
        public int LoadId { get; set; }
        public int OrderId { get; set; }
        public int PalletId { get; set; }

        public virtual Load Load { get; set; }
        public virtual Order Order { get; set; }
        public virtual Pallet Pallet { get; set; }
    }
}

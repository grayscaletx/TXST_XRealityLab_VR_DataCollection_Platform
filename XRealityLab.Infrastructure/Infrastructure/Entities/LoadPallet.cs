using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class LoadPallet
    {
        public int LoadPalletId { get; set; }
        public int LoadId { get; set; }
        public int PalletId { get; set; }

        public virtual Load Load { get; set; }
        public virtual Pallet Pallet { get; set; }
    }
}

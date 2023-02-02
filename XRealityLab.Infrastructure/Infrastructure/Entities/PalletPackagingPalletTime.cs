using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class PalletPackagingPalletTime
    {
        public int PalletPackagingPalletTimeId { get; set; }
        public int PalletId { get; set; }
        public int PackagingPalletTimeId { get; set; }

        public virtual PackagingPalletTime PackagingPalletTime { get; set; }
        public virtual PackagingLineForm Pallet { get; set; }
    }
}

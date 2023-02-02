using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class PackagingLineFormPallet
    {
        public int PackagingLineFormPalletId { get; set; }
        public int PackagingLineFormId { get; set; }
        public int PalletId { get; set; }

        public virtual PackagingLineForm PackagingLineForm { get; set; }
        public virtual Pallet Pallet { get; set; }
    }
}

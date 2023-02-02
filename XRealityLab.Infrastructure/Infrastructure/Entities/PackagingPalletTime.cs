using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class PackagingPalletTime
    {
        public PackagingPalletTime()
        {
            PalletPackagingPalletTimes = new HashSet<PalletPackagingPalletTime>();
        }

        public int PackagingPalletTimeId { get; set; }
        public int PalletBoxNo { get; set; }
        public DateTime PalletBoxCreationDateTime { get; set; }

        public virtual ICollection<PalletPackagingPalletTime> PalletPackagingPalletTimes { get; set; }
    }
}

using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class BrandType
    {
        public BrandType()
        {
            PalletBoxes = new HashSet<PalletBox>();
        }

        public int BrandTypeId { get; set; }
        public string Name { get; set; }

        public virtual ICollection<PalletBox> PalletBoxes { get; set; }
    }
}

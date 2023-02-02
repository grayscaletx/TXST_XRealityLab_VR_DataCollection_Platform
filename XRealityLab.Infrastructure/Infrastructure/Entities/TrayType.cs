using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class TrayType
    {
        public TrayType()
        {
            PalletBoxes = new HashSet<PalletBox>();
        }

        public int TrayTypeId { get; set; }
        public string Name { get; set; }

        public virtual ICollection<PalletBox> PalletBoxes { get; set; }
    }
}

using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class Tray
    {
        public Tray()
        {
            PalletBoxOldTrays = new HashSet<PalletBox>();
            PalletBoxRevisedTrays = new HashSet<PalletBox>();
        }

        public int TrayId { get; set; }
        public string Name { get; set; }

        public virtual ICollection<PalletBox> PalletBoxOldTrays { get; set; }
        public virtual ICollection<PalletBox> PalletBoxRevisedTrays { get; set; }
    }
}

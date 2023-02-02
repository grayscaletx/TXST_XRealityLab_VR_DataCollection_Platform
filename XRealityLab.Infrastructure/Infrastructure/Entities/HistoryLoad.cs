using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class HistoryLoad
    {
        public HistoryLoad()
        {
            HistoryLoadOrderPallets = new HashSet<HistoryLoadOrderPallet>();
        }

        public int LoadId { get; set; }
        public DateTime CreationDateTime { get; set; }
        public string PalletCardsLidl { get; set; }
        public string DriverDocuments { get; set; }
        public string Cmrdocuments { get; set; }
        public string LidlorderHeaders { get; set; }
        public string LidlorderLines { get; set; }

        public virtual ICollection<HistoryLoadOrderPallet> HistoryLoadOrderPallets { get; set; }
    }
}

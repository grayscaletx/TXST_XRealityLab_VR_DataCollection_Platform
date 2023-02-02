using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class HistoryLoadOrderPallet
    {
        public int LoadOrderPalletId { get; set; }
        public int LoadId { get; set; }
        public int OrderId { get; set; }
        public int PalletId { get; set; }

        public virtual HistoryLoad Load { get; set; }
        public virtual HistoryOrder Order { get; set; }
        public virtual HistoryPallet Pallet { get; set; }
    }
}

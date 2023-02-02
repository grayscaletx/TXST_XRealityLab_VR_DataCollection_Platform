using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class HistoryPallet
    {
        public HistoryPallet()
        {
            HistoryLoadOrderPallets = new HashSet<HistoryLoadOrderPallet>();
        }

        public int PalletId { get; set; }
        public int? MaterialId { get; set; }
        public string WareHousePosition { get; set; }
        public int CountryId { get; set; }
        public string PalletLot { get; set; }
        public int PackagingLinePalletNo { get; set; }
        public int PackagingLinePalletToCreated { get; set; }
        public DateTime PalletCreationDate { get; set; }
        public string Qrcode { get; set; }
        public string BarCode { get; set; }
        public string PalletCard { get; set; }
        public string PalletCardLidl { get; set; }
        public string Sscc { get; set; }

        public virtual Country Country { get; set; }
        public virtual Material1 Material { get; set; }
        public virtual ICollection<HistoryLoadOrderPallet> HistoryLoadOrderPallets { get; set; }
    }
}

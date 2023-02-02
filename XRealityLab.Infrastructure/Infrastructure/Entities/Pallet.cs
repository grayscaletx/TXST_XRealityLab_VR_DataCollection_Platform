using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class Pallet
    {
        public Pallet()
        {
            LoadOrderPallets = new HashSet<LoadOrderPallet>();
            LoadPallets = new HashSet<LoadPallet>();
            PackagingLineFormPallets = new HashSet<PackagingLineFormPallet>();
            TruckOrderPallets = new HashSet<TruckOrderPallet>();
        }

        public int PalletId { get; set; }
        public int MaterialId { get; set; }
        public int CountryId { get; set; }
        public int PalletLot { get; set; }
        public int PackagingLinePalletNo { get; set; }
        public DateTime? PalletCreationDateTime { get; set; }
        public string Qrcode { get; set; }
        public string BarCode { get; set; }
        public string PalletCard { get; set; }
        public string PalletCardLidl { get; set; }
        public string Sscc { get; set; }
        public byte IsVirtual { get; set; }
        public int Status { get; set; }

        public virtual Country Country { get; set; }
        public virtual Material1 Material { get; set; }
        public virtual ICollection<LoadOrderPallet> LoadOrderPallets { get; set; }
        public virtual ICollection<LoadPallet> LoadPallets { get; set; }
        public virtual ICollection<PackagingLineFormPallet> PackagingLineFormPallets { get; set; }
        public virtual ICollection<TruckOrderPallet> TruckOrderPallets { get; set; }
    }
}

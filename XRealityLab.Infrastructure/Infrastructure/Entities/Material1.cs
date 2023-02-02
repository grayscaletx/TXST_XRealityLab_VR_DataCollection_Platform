using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class Material1
    {
        public Material1()
        {
            HistoryOrders = new HashSet<HistoryOrder>();
            HistoryPallets = new HashSet<HistoryPallet>();
            Orders = new HashSet<Order>();
            PackagingLineForms = new HashSet<PackagingLineForm>();
            Pallets = new HashSet<Pallet>();
        }

        public int MaterialId { get; set; }
        public string MaterialSapid { get; set; }
        public string Language { get; set; }
        public string MaterialDescription { get; set; }
        public string PackagingLineDescription { get; set; }
        public int CountryId { get; set; }
        public string Gtin { get; set; }
        public string Seasonability { get; set; }
        public string Customer { get; set; }
        public int LagerDays { get; set; }
        public int ExpirationDays { get; set; }
        public int PalletFactor { get; set; }
        public double GrossWeight { get; set; }
        public double NetWeight { get; set; }
        public double Height { get; set; }
        public double PalletCost { get; set; }
        public double? FinalPrice { get; set; }
        public string Name1 { get; set; }
        public int? PiecesPerCarton { get; set; }
        public string Eanupc { get; set; }
        public string Bun { get; set; }
        public int? ProductFamilyId { get; set; }
        public string CustomerMaterialNumber { get; set; }

        public virtual Country Country { get; set; }
        public virtual ProductFamily ProductFamily { get; set; }
        public virtual ICollection<HistoryOrder> HistoryOrders { get; set; }
        public virtual ICollection<HistoryPallet> HistoryPallets { get; set; }
        public virtual ICollection<Order> Orders { get; set; }
        public virtual ICollection<PackagingLineForm> PackagingLineForms { get; set; }
        public virtual ICollection<Pallet> Pallets { get; set; }
    }
}

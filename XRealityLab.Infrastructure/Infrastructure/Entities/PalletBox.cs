using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class PalletBox
    {
        public PalletBox()
        {
            Pallets = new HashSet<Pallet>();
        }

        public int PalletBoxId { get; set; }
        public string PalletBoxCode { get; set; }
        public string Description { get; set; }
        public int? TrayTypeId { get; set; }
        public int BrandTypeId { get; set; }
        public string SupplementaryInfo { get; set; }
        public int ProductFamily1Id { get; set; }
        public int? CountryId { get; set; }
        public int RevisedTrayId { get; set; }
        public int OldTrayId { get; set; }
        public int SappalletCode { get; set; }
        public double Weight { get; set; }
        public int ProductFamily2Id { get; set; }
        public DateTime RegistrationDateTime { get; set; }
        public DateTime? WithdrawalDateTime { get; set; }

        public virtual BrandType BrandType { get; set; }
        public virtual Tray OldTray { get; set; }
        public virtual Tray RevisedTray { get; set; }
        public virtual TrayType TrayType { get; set; }
        public virtual ICollection<Pallet> Pallets { get; set; }
    }
}

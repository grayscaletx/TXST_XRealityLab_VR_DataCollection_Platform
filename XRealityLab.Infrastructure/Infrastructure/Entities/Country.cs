using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class Country
    {
        public Country()
        {
            Customer1s = new HashSet<Customer1>();
            Customers = new HashSet<Customer>();
            HistoryPallets = new HashSet<HistoryPallet>();
            Material1s = new HashSet<Material1>();
            Materials = new HashSet<Material>();
            Pallets = new HashSet<Pallet>();
        }

        public int CountryId { get; set; }
        public string NameEl { get; set; }
        public string Code { get; set; }
        public string NameEn { get; set; }

        public virtual ICollection<Customer1> Customer1s { get; set; }
        public virtual ICollection<Customer> Customers { get; set; }
        public virtual ICollection<HistoryPallet> HistoryPallets { get; set; }
        public virtual ICollection<Material1> Material1s { get; set; }
        public virtual ICollection<Material> Materials { get; set; }
        public virtual ICollection<Pallet> Pallets { get; set; }
    }
}

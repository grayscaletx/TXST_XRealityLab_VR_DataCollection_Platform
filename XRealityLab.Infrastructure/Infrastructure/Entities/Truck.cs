using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class Truck
    {
        public Truck()
        {
            TruckOrderPallets = new HashSet<TruckOrderPallet>();
        }

        public int TruckId { get; set; }
        public DateTime CreationDateTime { get; set; }
        public DateTime? CompletionDateTime { get; set; }
        public int IsAuthorized { get; set; }
        public string PalletCardLidl { get; set; }
        public string DriverDocuments { get; set; }
        public string Cmrdocuments { get; set; }
        public string LidlordersHeaders { get; set; }
        public string LidlordersLines { get; set; }

        public virtual ICollection<TruckOrderPallet> TruckOrderPallets { get; set; }
    }
}

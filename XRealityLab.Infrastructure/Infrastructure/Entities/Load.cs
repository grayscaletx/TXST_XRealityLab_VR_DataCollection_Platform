using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class Load
    {
        public Load()
        {
            LoadOrderPallets = new HashSet<LoadOrderPallet>();
            LoadOrders = new HashSet<LoadOrder>();
            LoadPallets = new HashSet<LoadPallet>();
        }

        public int LoadId { get; set; }
        public DateTime CreationDateTime { get; set; }
        public byte IsMatch { get; set; }
        public int Status { get; set; }
        public string PalletCardsLidl { get; set; }
        public string DriverDocuments { get; set; }
        public string Cmrdocuments { get; set; }
        public string LidlorderHeaders { get; set; }
        public string LidlorderLines { get; set; }

        public virtual ICollection<LoadOrderPallet> LoadOrderPallets { get; set; }
        public virtual ICollection<LoadOrder> LoadOrders { get; set; }
        public virtual ICollection<LoadPallet> LoadPallets { get; set; }
    }
}

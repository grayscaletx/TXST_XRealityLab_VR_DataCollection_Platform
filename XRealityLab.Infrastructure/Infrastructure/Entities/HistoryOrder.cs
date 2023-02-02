using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class HistoryOrder
    {
        public HistoryOrder()
        {
            HistoryLoadOrderPallets = new HashSet<HistoryLoadOrderPallet>();
        }

        public int OrderId { get; set; }
        public string RefrenceNumber { get; set; }
        public int MaterialId { get; set; }
        public int? ShipToParty { get; set; }
        public string ShipToPartyDescription { get; set; }
        public int? SoldToParty { get; set; }
        public string SoldToPartyDescription { get; set; }
        public DateTime DocumentDate { get; set; }
        public DateTime PurchaseOrderDate { get; set; }
        public DateTime RequestedDeliveryDate { get; set; }
        public int Quantity { get; set; }
        public string FileName { get; set; }
        public DateTime InsertionDatetime { get; set; }
        public DateTime LastModificationDateTime { get; set; }

        public virtual Material1 Material { get; set; }
        public virtual ICollection<HistoryLoadOrderPallet> HistoryLoadOrderPallets { get; set; }
    }
}

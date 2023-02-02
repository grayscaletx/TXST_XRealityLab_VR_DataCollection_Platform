using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class Order
    {
        public Order()
        {
            LoadOrderPallets = new HashSet<LoadOrderPallet>();
            LoadOrders = new HashSet<LoadOrder>();
            TruckOrderPallets = new HashSet<TruckOrderPallet>();
        }

        public int OrderId { get; set; }
        public string ReferenceNumber { get; set; }
        public int MaterialId { get; set; }
        public int ShipToParty { get; set; }
        public int SoldToParty { get; set; }
        public DateTime DocumentDate { get; set; }
        public DateTime PurchaseOrderDate { get; set; }
        public DateTime RequestedDeliveryDate { get; set; }
        public int Quantity { get; set; }
        public string FileName { get; set; }
        public DateTime InsertionDateTime { get; set; }
        public int Status { get; set; }
        public DateTime? LastModificationDateTime { get; set; }

        public virtual Material1 Material { get; set; }
        public virtual ICollection<LoadOrderPallet> LoadOrderPallets { get; set; }
        public virtual ICollection<LoadOrder> LoadOrders { get; set; }
        public virtual ICollection<TruckOrderPallet> TruckOrderPallets { get; set; }
    }
}

using ARI.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace ARI.PackagingLine
{
    public class PalletCardLidlDTO
    {
        public string Supplier { get; set; }
        public string OrderNumber { get; set; }
        public MaterialDTO Material { get; set; }
        public PalletDTO Pallet { get; set; }
        public EDIOrderDTO Order { get; set; }
        public CustomerDTO ShipToParty { get; set; }
        public CustomerDTO SoldToParty { get; set; }
        public int PalletNumber { get; set; }
        public int SumPallet { get; set; }
        public DateTime DeliveryDate { get; set; }
        public DateTime BestBefore { get; set; }

        public string DeliveryNoteNo {get;set;}

        public string Warehouse { get; set; }
        public string Logistics { get; set; }
        public string SSCC { get; set; }
    }
}

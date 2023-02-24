using System;
using System.Collections.Generic;
using System.Text;

namespace XRealityLab.Models
{
    public class TruckDTO
    {
        public int TruckId { get; set; }
        public string PassToken { get; set; }
        public DateTime CreationDateTime { get; set; }
        public int NoOrders { get; set; }
        public int NoPallets { get; set; }
        public int IsAuthorized { get; set; }
        public string PalletCardLIDL { get; set; }
        public string DriverDocuments { get; set; }
        public string CMRDocuments { get; set; }
        public string LIDLOrdersHeaders { get; set; }
        public string LIDLOrdersLines { get; set; }
        public DateTime? CompletionDateTime { get; set; }
    }
}

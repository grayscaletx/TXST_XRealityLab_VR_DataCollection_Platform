using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class SapZarmatinfo
    {
        public string Material { get; set; }
        public string MaterialDescription { get; set; }
        public string Customer { get; set; }
        public string CustomerMaterialNumber { get; set; }
        public string ProductFamily { get; set; }
        public string Seasonality { get; set; }
        public string Name1 { get; set; }
        public string Land1 { get; set; }
        public string PalletFactor { get; set; }
        public string PiecesPerCarton { get; set; }
        public string Netweight { get; set; }
        public string Grweight { get; set; }
        public string Height { get; set; }
        public string Gtin { get; set; }
        public string Eanupc { get; set; }
        public string ΤιμήΠαλέτας { get; set; }
        public string ValidTo { get; set; }
    }
}

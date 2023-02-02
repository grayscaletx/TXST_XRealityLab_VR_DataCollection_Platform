using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class SapKonp
    {
        public string Knumh { get; set; }
        public string LoevmKo { get; set; }
        public string Kunnr { get; set; }
        public string Bomat { get; set; }
        public decimal? Kbetr { get; set; }
    }
}

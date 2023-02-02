using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class SapZgtinTable
    {
        public string CustomerNumberKunnr { get; set; }
        public string Gtin { get; set; }
        public string ValidTo { get; set; }
        public int? Matnr { get; set; }
    }
}

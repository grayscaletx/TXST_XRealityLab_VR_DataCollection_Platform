using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class SapStko
    {
        public int? Matnr { get; set; }
        public int? Stlal { get; set; }
        public int? Stlnr { get; set; }
        public string Stlty { get; set; }
        public string Bmeng { get; set; }
    }
}

using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class SapMast
    {
        public int? Matnr { get; set; }
        public int? Stlnr { get; set; }
        public int? Stlal { get; set; }
        public string Werks { get; set; }
        public string Stlan { get; set; }
    }
}

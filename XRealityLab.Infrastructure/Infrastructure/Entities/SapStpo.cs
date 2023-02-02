using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class SapStpo
    {
        public int? Stlnr { get; set; }
        public int? Idnrk { get; set; }
        public string Stlty { get; set; }
        public string Menge { get; set; }
    }
}

using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class SapA602
    {
        public string KunnrV { get; set; }
        public string Knumh { get; set; }
        public int? Matnr { get; set; }
        public DateTime? Datbi { get; set; }
    }
}

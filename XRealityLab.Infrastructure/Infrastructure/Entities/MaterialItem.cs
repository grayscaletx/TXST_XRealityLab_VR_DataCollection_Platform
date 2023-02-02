using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class MaterialItem
    {
        public int? Material { get; set; }
        public int? Item { get; set; }
        public int? Menge { get; set; }
        public decimal? ItemsNumber { get; set; }
        public string MaterialDescription { get; set; }
        public string ItemDescription { get; set; }
    }
}

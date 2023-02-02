using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class ProductFamily
    {
        public ProductFamily()
        {
            Material1s = new HashSet<Material1>();
            Materials = new HashSet<Material>();
        }

        public int ProductFamilyId { get; set; }
        public string Name { get; set; }

        public virtual ICollection<Material1> Material1s { get; set; }
        public virtual ICollection<Material> Materials { get; set; }
    }
}

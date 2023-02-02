using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class Product
    {
        public Product()
        {
            Pallets = new HashSet<Pallet>();
        }

        public int ProductId { get; set; }
        public string Name { get; set; }
        public DateTime ExpirationDateTime { get; set; }

        public virtual ICollection<Pallet> Pallets { get; set; }
    }
}

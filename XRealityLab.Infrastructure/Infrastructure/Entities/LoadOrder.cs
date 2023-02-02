using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class LoadOrder
    {
        public int LoadOrderId { get; set; }
        public int LoadId { get; set; }
        public int OrderId { get; set; }

        public virtual Load Load { get; set; }
        public virtual Order Order { get; set; }
    }
}

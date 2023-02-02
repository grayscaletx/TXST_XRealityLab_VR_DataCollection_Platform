using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class TruckOrder
    {
        public int TruckOrderId { get; set; }
        public int TruckId { get; set; }
        public int OrderId { get; set; }
    }
}

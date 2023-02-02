using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class OrderAriorder
    {
        public int OrderAriorderId { get; set; }
        public int OrderAriid { get; set; }
        public int OrderId { get; set; }

        public virtual Order Order { get; set; }
        public virtual OrderAri OrderAriorderNavigation { get; set; }
    }
}

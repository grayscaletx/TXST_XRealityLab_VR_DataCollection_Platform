using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class OrderAri
    {
        public int OrderArid { get; set; }
        public DateTime CreationDateTime { get; set; }
        public int Status { get; set; }

        public virtual OrderAriorder OrderAriorder { get; set; }
    }
}

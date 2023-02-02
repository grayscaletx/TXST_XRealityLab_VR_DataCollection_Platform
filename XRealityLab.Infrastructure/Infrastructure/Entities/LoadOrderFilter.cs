using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class LoadOrderFilter
    {
        public int LoadOrderFilterId { get; set; }
        public int UserId { get; set; }
        public string Text { get; set; }
        public DateTime? DocumentFrom { get; set; }
        public DateTime? DocumentUntil { get; set; }
        public DateTime? PurchaseFrom { get; set; }
        public DateTime? PurchaseUntil { get; set; }
        public DateTime? RequestedFrom { get; set; }
        public DateTime? RequestedUntil { get; set; }

        public virtual User User { get; set; }
    }
}

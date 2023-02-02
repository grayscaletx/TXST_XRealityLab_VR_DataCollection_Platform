using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class Warehouse
    {
        public int WarehouseId { get; set; }
        public int? PalletId { get; set; }
        public int LoadingAreaId { get; set; }
        public int LoadingLine { get; set; }
        public int LoadingPosition { get; set; }
        public DateTime? ModificationDateTime { get; set; }
    }
}

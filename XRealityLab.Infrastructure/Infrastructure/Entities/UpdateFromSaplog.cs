using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class UpdateFromSaplog
    {
        public DateTime? Date { get; set; }
        public string Text { get; set; }
    }
}

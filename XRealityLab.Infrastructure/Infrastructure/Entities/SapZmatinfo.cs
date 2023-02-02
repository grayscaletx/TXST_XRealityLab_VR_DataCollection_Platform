using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class SapZmatinfo
    {
        public string PiecesPerCartonPcspercar { get; set; }
        public string Netweight { get; set; }
        public string Grweight { get; set; }
        public string Height { get; set; }
        public int? MaterialMatnr { get; set; }
    }
}

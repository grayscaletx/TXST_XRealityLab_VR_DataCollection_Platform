using System;
using System.Collections.Generic;
using System.Text;

namespace ARI.Models
{
    public class ExportPalletDTO
    {
        public int PackagingLineId { get; set; }
        public string MaterialDescription { get; set; }
        public int PalletLOT { get; set; }
        public string MaterialSAPID { get; set; }
        public DateTime CreationDateTime { get; set; }
        public int PalletNo { get; set; }
        public DateTime ExpirationDate { get; set; }
    }
}

using ARI.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace ARI.Models
{
    public class PalletQRInfoDTO
    {
        public PalletDTO palletDTO { get; set; }
        public EDIOrderDTO eDIOrderDTO  { get; set; }
    }
}

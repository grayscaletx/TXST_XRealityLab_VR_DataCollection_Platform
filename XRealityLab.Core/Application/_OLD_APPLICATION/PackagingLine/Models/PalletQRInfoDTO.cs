using XRealityLab.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace XRealityLab.Models
{
    public class PalletQRInfoDTO
    {
        public PalletDTO palletDTO { get; set; }
        public EDIOrderDTO eDIOrderDTO  { get; set; }
    }
}

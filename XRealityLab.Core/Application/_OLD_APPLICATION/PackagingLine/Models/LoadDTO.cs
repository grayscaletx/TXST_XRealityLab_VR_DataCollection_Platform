using Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace XRealityLab.Models
{
    public class LoadDTO
    {
        public int LoadId { get; set; }
        public string PassToken { get; set; }
        public DateTime CreationDateTime { get; set; }
        public int StatusId { get; set; }
        public string SelectedStatus { get; set; }
        public bool IsMatch { get; set; }
        public List<EDIOrderDTO> EDIOrderDTOs { get; set; }
        public List<GroupOrderDTO> GroupOrderDTOs { get; set; }
        public LoadMetricsDTO LoadMetricsDTO { get; set; }
        //TEMP
        public WarehouseDTO WarehouseDTO { get; set; }
        public string PalletCardsLIDL { get; set; }
        public string DriverDocuments { get; set; }
        public string CMRDocuments { get; set; }

        public string LIDLOrderHeaders { get; set; }
        public string LIDLOrderLines { get; set; }

    }

    public class LoadMetricsDTO
    {
        public int NoOrders { get; set; }
        public int NoOrderPallets { get; set; }
        public int NoPallets { get; set; }
        public bool IsIdentified { get; set; }
        public string Label { get; set; }
    }


}

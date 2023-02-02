using System;
using System.Collections.Generic;
using System.Text;

namespace ARI.Models
{
    public class WarehouseSuppliesDTO
    {
        public MaterialDTO MaterialDTO { get; set; }
        public DateTime CreationDate { get; set; }
        public List<SuppliesMetricsDTO> suppliesMetricsDTOs { get; set; }
    }

    public class SuppliesMetricsDTO
    {
        public int Sala1 { get; set; }
        public int Sala1Psigeio { get; set; }
        public int Proektasi { get; set; }
        public int Sala2Deksia { get; set; }
        public int Sala2Aristera { get; set; }
        public int Sala2Dwmatio { get; set; }
        public int Makio { get; set; }
        public int Sum { get; set; }
        public int Status { get; set; }
    }
}

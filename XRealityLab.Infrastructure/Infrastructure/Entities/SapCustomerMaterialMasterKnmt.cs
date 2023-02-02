using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class SapCustomerMaterialMasterKnmt
    {
        public string CustomerKunnr { get; set; }
        public string CustomerMaterialNumberKdmat { get; set; }
        public string BaseUnitOfMeasureMeins { get; set; }
        public int? MaterialMatnr { get; set; }
    }
}

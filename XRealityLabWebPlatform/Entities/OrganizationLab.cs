using System;
using System.Collections.Generic;

#nullable disable

namespace XRealityLab.Entities
{
    public partial class OrganizationLab
    {
        public int OrganizationLabId { get; set; }
        public int OrganizationId { get; set; }
        public int LabId { get; set; }

        public virtual Lab Lab { get; set; }
        public virtual Organization Organization { get; set; }
    }
}

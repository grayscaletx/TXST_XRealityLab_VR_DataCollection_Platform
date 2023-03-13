using System;
using System.Collections.Generic;

namespace XRealityLab.Entities
{
    public partial class Lab
    {
        public Lab()
        {
            LabUser = new HashSet<LabUser>();
            OrganizationLab = new HashSet<OrganizationLab>();
        }

        public int LabId { get; set; }
        public string FullName { get; set; }
        public int LabDirectorId { get; set; }
        public string Description { get; set; }
        public string Color { get; set; }
        public string PhotoUrl { get; set; }
        public DateTime Registered { get; set; }

        public virtual User LabDirector { get; set; }
        public virtual ICollection<LabUser> LabUser { get; set; }
        public virtual ICollection<OrganizationLab> OrganizationLab { get; set; }
    }
}

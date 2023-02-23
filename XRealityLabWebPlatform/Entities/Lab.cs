using System;
using System.Collections.Generic;

#nullable disable

namespace XRealityLab.Entities
{
    public partial class Lab
    {
        public Lab()
        {
            LabUsers = new HashSet<LabUser>();
            OrganizationLabs = new HashSet<OrganizationLab>();
        }

        public int LabId { get; set; }
        public string FullName { get; set; }
        public int LabDirectorId { get; set; }
        public string Description { get; set; }
        public string Color { get; set; }
        public DateTime Registered { get; set; }

        public virtual User LabDirector { get; set; }
        public virtual ICollection<LabUser> LabUsers { get; set; }
        public virtual ICollection<OrganizationLab> OrganizationLabs { get; set; }
    }
}

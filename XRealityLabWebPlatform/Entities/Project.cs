using System;
using System.Collections.Generic;

#nullable disable

namespace XRealityLab.Entities
{
    public partial class Project
    {
        public Project()
        {
            ProjectResearches = new HashSet<ProjectResearch>();
            ProjectUsers = new HashSet<ProjectUser>();
        }

        public int ProjectId { get; set; }
        public string FullName { get; set; }
        public string Description { get; set; }
        public string PhotoUrl { get; set; }
        public string Color { get; set; }
        public DateTime Registered { get; set; }

        public virtual ICollection<ProjectResearch> ProjectResearches { get; set; }
        public virtual ICollection<ProjectUser> ProjectUsers { get; set; }
    }
}

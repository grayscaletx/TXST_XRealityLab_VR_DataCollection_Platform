using System;
using System.Collections.Generic;

namespace Infrastructure.Entities
{
    public partial class Project
    {
        public Project()
        {
            ProjectResearch = new HashSet<ProjectResearch>();
            ProjectSession = new HashSet<ProjectSession>();
            ProjectUser = new HashSet<ProjectUser>();
        }

        public int ProjectId { get; set; }
        public string FullName { get; set; }
        public string Description { get; set; }
        public string PhotoUrl { get; set; }
        public string Color { get; set; }
        public DateTime Registered { get; set; }

        public virtual ICollection<ProjectResearch> ProjectResearch { get; set; }
        public virtual ICollection<ProjectSession> ProjectSession { get; set; }
        public virtual ICollection<ProjectUser> ProjectUser { get; set; }
    }
}

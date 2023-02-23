using System;
using System.Collections.Generic;

#nullable disable

namespace XRealityLab.Entities
{
    public partial class Research
    {
        public Research()
        {
            ProjectResearches = new HashSet<ProjectResearch>();
        }

        public int ResearchId { get; set; }
        public string Title { get; set; }
        public int ResearchMethodId { get; set; }
        public DateTime Registered { get; set; }

        public virtual ICollection<ProjectResearch> ProjectResearches { get; set; }
    }
}

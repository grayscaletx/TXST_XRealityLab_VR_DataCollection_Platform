using System;
using System.Collections.Generic;

namespace Infrastructure.Entities
{
    public partial class Research
    {
        public Research()
        {
            ProjectResearch = new HashSet<ProjectResearch>();
        }

        public int ResearchId { get; set; }
        public string Title { get; set; }
        public int ResearchMethodId { get; set; }
        public DateTime Registered { get; set; }

        public virtual ICollection<ProjectResearch> ProjectResearch { get; set; }
    }
}

using System;
using System.Collections.Generic;

namespace XRealityLab.Entities
{
    public partial class ProjectResearch
    {
        public int ProjectReseachId { get; set; }
        public int ProjectId { get; set; }
        public int ResearchId { get; set; }

        public virtual Project Project { get; set; }
        public virtual Research Research { get; set; }
    }
}

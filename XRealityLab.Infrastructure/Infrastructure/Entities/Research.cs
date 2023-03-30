using System;
using System.Collections.Generic;

namespace Infrastructure.Entities
{
    public partial class Research
    {
        public Research()
        {
            OfflineResult = new HashSet<OfflineResult>();
            ParticipantResult = new HashSet<ParticipantResult>();
            ProjectResearch = new HashSet<ProjectResearch>();
            QualtricsResult = new HashSet<QualtricsResult>();
        }

        public int ResearchId { get; set; }
        public string Title { get; set; }
        public int ResearchMethodId { get; set; }
        public DateTime Registered { get; set; }

        public virtual ICollection<OfflineResult> OfflineResult { get; set; }
        public virtual ICollection<ParticipantResult> ParticipantResult { get; set; }
        public virtual ICollection<ProjectResearch> ProjectResearch { get; set; }
        public virtual ICollection<QualtricsResult> QualtricsResult { get; set; }
    }
}

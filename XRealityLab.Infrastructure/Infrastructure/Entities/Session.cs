using System;
using System.Collections.Generic;

namespace Infrastructure.Entities
{
    public partial class Session
    {
        public Session()
        {
            ParticipantResult = new HashSet<ParticipantResult>();
            ProjectSession = new HashSet<ProjectSession>();
            SessionParticipant = new HashSet<SessionParticipant>();
        }

        public int SessionId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime Registered { get; set; }

        public virtual ICollection<ParticipantResult> ParticipantResult { get; set; }
        public virtual ICollection<ProjectSession> ProjectSession { get; set; }
        public virtual ICollection<SessionParticipant> SessionParticipant { get; set; }
    }
}

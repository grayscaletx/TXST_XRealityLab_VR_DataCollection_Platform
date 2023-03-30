using System;
using System.Collections.Generic;

namespace Infrastructure.Entities
{
    public partial class Participant
    {
        public Participant()
        {
            OfflineResult = new HashSet<OfflineResult>();
            ParticipantResult = new HashSet<ParticipantResult>();
            QualtricsResult = new HashSet<QualtricsResult>();
            SessionParticipant = new HashSet<SessionParticipant>();
        }

        public int ParticipantId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public DateTime Registered { get; set; }

        public virtual ICollection<OfflineResult> OfflineResult { get; set; }
        public virtual ICollection<ParticipantResult> ParticipantResult { get; set; }
        public virtual ICollection<QualtricsResult> QualtricsResult { get; set; }
        public virtual ICollection<SessionParticipant> SessionParticipant { get; set; }
    }
}

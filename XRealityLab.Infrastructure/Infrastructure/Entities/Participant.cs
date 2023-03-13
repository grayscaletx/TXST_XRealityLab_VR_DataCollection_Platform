using System;
using System.Collections.Generic;

namespace Infrastructure.Entities
{
    public partial class Participant
    {
        public Participant()
        {
            SessionParticipant = new HashSet<SessionParticipant>();
        }

        public int ParticipantId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public DateTime Registered { get; set; }

        public virtual ICollection<SessionParticipant> SessionParticipant { get; set; }
    }
}

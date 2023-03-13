using System;
using System.Collections.Generic;

namespace Infrastructure.Entities
{
    public partial class SessionParticipant
    {
        public int SessionParticipantId { get; set; }
        public int SessionId { get; set; }
        public int ParticipantId { get; set; }

        public virtual Participant Participant { get; set; }
        public virtual Session Session { get; set; }
    }
}

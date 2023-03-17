using System;
using System.Collections.Generic;

namespace Infrastructure.Entities
{
    public partial class ParticipantResult
    {
        public int ParticipantResultId { get; set; }
        public int ParticipantId { get; set; }
        public int SessionId { get; set; }
        public TimeSpan Time { get; set; }
        public int NoOfMistakes { get; set; }
        public DateTime Registered { get; set; }
        public string Notes { get; set; }

        public virtual Participant Participant { get; set; }
        public virtual Session Session { get; set; }
    }
}

using System;
using System.Collections.Generic;

namespace Infrastructure.Entities
{
    public partial class OfflineResult
    {
        public int OfflineResultId { get; set; }
        public int ParticipantId { get; set; }
        public int SessionId { get; set; }
        public int ResearchId { get; set; }
        public string DocumentUrl { get; set; }
        public string Notes { get; set; }

        public virtual Participant Participant { get; set; }
        public virtual Research Research { get; set; }
        public virtual Session Session { get; set; }
    }
}

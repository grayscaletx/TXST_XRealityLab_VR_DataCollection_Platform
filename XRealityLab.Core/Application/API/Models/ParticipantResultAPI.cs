using System;
using System.Collections.Generic;
using System.Text;

namespace XRealityLab.API
{
    public class ParticipantResultAPI
    {
        public int ParticipantResultId { get; set; }
        public int ParticipantId { get; set; }
        public int SessionId { get; set; }
        public TimeSpan Time { get; set; }
        public int NoOfMistakes { get; set; }
        public DateTime Registered { get; set; }
        public string Notes { get; set; }

    }
}

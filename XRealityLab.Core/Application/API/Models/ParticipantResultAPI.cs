using System;
using System.Collections.Generic;
using System.Text;

namespace XRealityLab.API
{
    public class ParticipantResultAPI
    {
        public int ParticipantResultId { get; set; }
        public string ParticipantPassToken { get; set; }
        public string SessionPassToken { get; set; }
        public string ResearchPassToken { get; set; }
        public TimeSpan Time { get; set; }
        public int NoOfMistakes { get; set; }
        public string Notes { get; set; }
    }
}

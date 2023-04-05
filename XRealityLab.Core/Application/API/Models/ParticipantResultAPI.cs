using System;
using System.Collections.Generic;
using System.Text;

namespace XRealityLab.API
{
    public class ParticipantResultAPI
    {
        public int ParticipantResultId { get; set; }
        public string ParticipantMD5 { get; set; }
        public string SessionMD5 { get; set; }
        public string ResearchMD5 { get; set; }
        public TimeSpan Time { get; set; }
        public int NoOfMistakes { get; set; }
        public string Notes { get; set; }
    }
}

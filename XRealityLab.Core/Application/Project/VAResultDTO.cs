using System;
using System.Collections.Generic;
using System.Text;

namespace XRealityLab.Models
{
    public class VAResultDTO
    {
        public string Title { get; set; }
        public string ResearchPassToken { get; set; }
        public string ResearchMD5 { get; set; }
        public int ParticipantResultId { get; set; }
        public int ParticipantId { get; set; }
        public int SessionId { get; set; }
        public int ResearchId { get; set; }
        public TimeSpan Time { get; set; }
        public DateTime Registered { get; set; }
        public int NoOfMistakes { get; set; }
        public string Notes { get; set; }
    }
}

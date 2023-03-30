using System;
using System.Collections.Generic;
using System.Text;

namespace XRealityLab.Models
{
    public class QualtricsResultDTO
    {
        public string Title { get; set; }
        public string ResearchPassToken { get; set; }
        public int QualtricsResultId { get; set; }
        public int ParticipantId { get; set; }
        public int SessionId { get; set; }
        public int ResearchId { get; set; }
        public string DocumentUrl { get; set; }
        public string Notes { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace XRealityLab.API
{
    public class QualtricsResultAPI
    {
        public int QualtricsResultId { get; set; }
        public string ParticipantMD5 { get; set; }
        public string SessionMD5 { get; set; }
        public string ResearchMD5 { get; set; }
        public string DocumentUrl { get; set; }
        public string Notes { get; set; }
    }
}

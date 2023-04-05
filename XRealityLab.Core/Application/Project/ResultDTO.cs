using System;
using System.Collections.Generic;
using System.Text;

namespace XRealityLab.Models
{
    public class ResultDTO
    {
        public string SessionPassToken { get; set; }
        public string SessionMD5 { get; set; }
        public string ParticipantPassToken { get; set; }
        public string ParticipantMD5{ get; set; }
        public string Title { get; set; }
        public string Notes { get; set; }
        public ParticipantDTO ParticipantDTO { get; set; }
        public List<ResearchDTO> ResearchDTOs { get; set; }
        public List<VAResultDTO> VAResultDTOs { get; set; }
        public List<QualtricsResultDTO> QualtricsResultDTOs { get; set; }
        public List<OfflineResultDTO> OfflineResultDTOs { get; set; }
    }
}

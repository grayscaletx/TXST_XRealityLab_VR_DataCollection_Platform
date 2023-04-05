using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace XRealityLab.Models
{
    public class ResearchDTO
    {
        public int ResearchId { get; set; }
        public string ResearchPassToken { get; set; }
        public string ResearchMD5 { get; set; }
        public string Title { get; set; }
        public int ResearchMethodId { get; set; }
        public string TypeId { get; set; }
        public DateTime Registered { get; set; }
        public List<VAResultDTO> VAResultDTOs { get; set; }
        public QualtricsResultDTO QualtricsResultDTO { get; set; }
        public OfflineResultDTO OfflineResultDTO { get; set; }

    }
}

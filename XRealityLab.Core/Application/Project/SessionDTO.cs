using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace XRealityLab.Models
{
    public class SessionDTO
    {
        public int SessionId { get; set; }
        public string PassToken { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime Registered { get; set; }
        public string ProjectPassToken { get; set; }
        public int ParticipantsCount { get; set; }
        public List<ParticipantDTO> participantDTOs { get; set; }
        public List<ResearchDTO> researchDTOs { get; set; }
    }
}

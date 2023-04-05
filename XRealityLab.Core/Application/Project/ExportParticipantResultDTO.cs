using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace XRealityLab.Models
{
    public class ExportParticipantResultDTO
    {
        public int ParticipantId { get; set; }
        public string ParticipantPassToken { get; set; }
        public string SessionPassToken { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public DateTime Registered { get; set; }
        List<VAResultDTO> vAResultDTOs { get; set; }
        List<QualtricsResultDTO> qualtricsResultDTOs { get; set; }
        List<OfflineResultDTO> offlineResultDTOs { get; set; }
    }
}

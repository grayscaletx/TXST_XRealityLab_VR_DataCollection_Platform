using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace XRealityLab.Models
{
    public class ParticipantDTO
    {
        public int ParticipantId { get; set; }
        public string PassToken { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public DateTime Registered { get; set; }
        public string SessionPassToken { get; set; }
    }
}

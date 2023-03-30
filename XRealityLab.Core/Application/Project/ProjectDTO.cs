using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace XRealityLab.Models
{
    public class ProjectDTO
    {
        public int ProjectId { get; set; }
        public string PassToken { get; set; }
        public string FullName { get; set; }
        public string PhotoUrl { get; set; }
        public string PhotoBase64 { get; set; }
        public string Description { get; set; }
        public string Color { get; set; }
        public int ResearchMethodId { get; set; }
        public string Title { get; set; }
        public DateTime Registered { get; set; }
        public List<UserDTO> UserDTOs { get; set; }
        public List<ResearchDTO> ResearchDTOs { get; set; }
        public List<SessionDTO> SessionDTOs { get; set; }
    }
}

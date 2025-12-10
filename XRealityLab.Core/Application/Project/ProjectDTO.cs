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
        [Required(ErrorMessage = "Project name is required")]
        [StringLength(50, ErrorMessage = "Project name can't exceed 50 characters")]
        public string FullName { get; set; }
        public string PhotoUrl { get; set; }
        public string PhotoBase64 { get; set; }
        [Required(ErrorMessage = "Description is required")]
        [StringLength(250, ErrorMessage = "Description can't exceed 250 characters")]
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

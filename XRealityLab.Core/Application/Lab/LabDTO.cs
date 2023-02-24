using System;

namespace XRealityLab.Models
{
    public class LabDTO
    {
        public int LabId { get; set; }
        public string PassToken { get; set; }
        public string FullName { get; set; }
        public int LabDirectorId { get; set; }
        public string Description { get; set; }
        public string PhotoUrl { get; set; }
        public string Color { get; set; }
        public DateTime Registered { get; set; }
        public UserDTO LabDirector { get; set; }

    }
}

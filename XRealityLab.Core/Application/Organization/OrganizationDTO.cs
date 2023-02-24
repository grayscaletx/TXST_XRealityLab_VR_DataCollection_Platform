using System;

namespace XRealityLab.Models
{
    public class OrganizationDTO
    {
        public int OrganizationId { get; set; }
        public string PassToken { get; set; }
        public string FullName { get; set; }
        public int ITDirectorId { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string ZIPCode { get; set; }
        public string Color { get; set; }
        public string PhotoUrl { get; set; }
        public int NoAllowedLabs { get; set; }
        public DateTime Registered { get; set; }
        public DateTime SubscriptionStarted { get; set; }
        public DateTime SubscriptionExpiration { get; set; }
        public UserDTO ITDirector { get; set; }
        public LabDTO LabDTO { get; set; }
    }
}

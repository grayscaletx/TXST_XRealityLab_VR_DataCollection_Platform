using System;
using System.Collections.Generic;
using System.Text;

namespace XRealityLab.API
{
    public class OrganizationAPI
    {
        public int OrganizationId { get; set; }
        public string FullName { get; set; }
        public string Telephone { get; set; }
        public int MaxUsers { get; set; }
        public string PhotoUrl { get; set; }
        public DateTime Registered { get; set; }
        public DateTime SubscriptionStarted { get; set; }
        public DateTime SubscriptionExpiration { get; set; }
        public byte? Active { get; set; }
    }
}

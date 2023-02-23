using System;
using System.Collections.Generic;

#nullable disable

namespace XRealityLab.Entities
{
    public partial class Organization
    {
        public Organization()
        {
            OrganizationLabs = new HashSet<OrganizationLab>();
        }

        public int OrganizationId { get; set; }
        public string FullName { get; set; }
        public int ItdirectorId { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string Zipcode { get; set; }
        public string Color { get; set; }
        public string PhotoUrl { get; set; }
        public int NoAllowedLabs { get; set; }
        public DateTime Registered { get; set; }
        public DateTime SubscriptionStarted { get; set; }
        public DateTime SubscriptionExpiration { get; set; }

        public virtual User Itdirector { get; set; }
        public virtual ICollection<OrganizationLab> OrganizationLabs { get; set; }
    }
}

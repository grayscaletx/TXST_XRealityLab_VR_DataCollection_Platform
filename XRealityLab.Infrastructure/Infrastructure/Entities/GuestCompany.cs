using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class GuestCompany
    {
        public GuestCompany()
        {
            Guests = new HashSet<Guest>();
        }

        public int GuestCompanyId { get; set; }
        public string Name { get; set; }

        public virtual ICollection<Guest> Guests { get; set; }
    }
}

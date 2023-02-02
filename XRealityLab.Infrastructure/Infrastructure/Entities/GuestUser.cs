using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class GuestUser
    {
        public GuestUser()
        {
            Guests = new HashSet<Guest>();
        }

        public int GuestUserId { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string Type { get; set; }

        public virtual ICollection<Guest> Guests { get; set; }
    }
}

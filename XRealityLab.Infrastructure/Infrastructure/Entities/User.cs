using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class User
    {
        public User()
        {
            CompanyCarReservations = new HashSet<CompanyCarReservation>();
            LoadOrderFilters = new HashSet<LoadOrderFilter>();
        }

        public int UserId { get; set; }
        public string IdentityId { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string Telephone { get; set; }
        public byte Active { get; set; }
        public DateTime RegistrationDateTime { get; set; }
        public DateTime? LastConnectionDateTime { get; set; }
        public byte Blocked { get; set; }
        public int RoleId { get; set; }

        public virtual Role Role { get; set; }
        public virtual ICollection<CompanyCarReservation> CompanyCarReservations { get; set; }
        public virtual ICollection<LoadOrderFilter> LoadOrderFilters { get; set; }
    }
}

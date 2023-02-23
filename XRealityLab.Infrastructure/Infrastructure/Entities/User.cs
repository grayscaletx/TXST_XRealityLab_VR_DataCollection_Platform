using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class User
    {
        public User()
        {
            LabUsers = new HashSet<LabUser>();
            Labs = new HashSet<Lab>();
            Organizations = new HashSet<Organization>();
            ProjectUsers = new HashSet<ProjectUser>();
        }

        public int UserId { get; set; }
        public string IdentityId { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string Telephone { get; set; }
        public string PhotoUrl { get; set; }
        public int RoleId { get; set; }
        public byte IsActive { get; set; }
        public DateTime RegistrationDateTime { get; set; }

        public virtual Role Role { get; set; }
        public virtual ICollection<LabUser> LabUsers { get; set; }
        public virtual ICollection<Lab> Labs { get; set; }
        public virtual ICollection<Organization> Organizations { get; set; }
        public virtual ICollection<ProjectUser> ProjectUsers { get; set; }
    }
}

using System;
using System.Collections.Generic;

namespace Infrastructure.Entities
{
    public partial class User
    {
        public User()
        {
            Lab = new HashSet<Lab>();
            LabUser = new HashSet<LabUser>();
            Organization = new HashSet<Organization>();
            ProjectUser = new HashSet<ProjectUser>();
            UserAuthToken = new HashSet<UserAuthToken>();
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
        public virtual ICollection<Lab> Lab { get; set; }
        public virtual ICollection<LabUser> LabUser { get; set; }
        public virtual ICollection<Organization> Organization { get; set; }
        public virtual ICollection<ProjectUser> ProjectUser { get; set; }
        public virtual ICollection<UserAuthToken> UserAuthToken { get; set; }
    }
}

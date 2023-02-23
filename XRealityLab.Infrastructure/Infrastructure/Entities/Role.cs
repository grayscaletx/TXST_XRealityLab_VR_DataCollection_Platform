using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class Role
    {
        public Role()
        {
            LabUsers = new HashSet<LabUser>();
            ProjectUsers = new HashSet<ProjectUser>();
            Users = new HashSet<User>();
        }

        public int RoleId { get; set; }
        public string Type { get; set; }

        public virtual ICollection<LabUser> LabUsers { get; set; }
        public virtual ICollection<ProjectUser> ProjectUsers { get; set; }
        public virtual ICollection<User> Users { get; set; }
    }
}

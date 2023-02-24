using System;
using System.Collections.Generic;

namespace Infrastructure.Entities
{
    public partial class Role
    {
        public Role()
        {
            LabUser = new HashSet<LabUser>();
            ProjectUser = new HashSet<ProjectUser>();
            User = new HashSet<User>();
        }

        public int RoleId { get; set; }
        public string Type { get; set; }

        public virtual ICollection<LabUser> LabUser { get; set; }
        public virtual ICollection<ProjectUser> ProjectUser { get; set; }
        public virtual ICollection<User> User { get; set; }
    }
}

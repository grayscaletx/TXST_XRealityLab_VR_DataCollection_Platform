using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class Role
    {
        public Role()
        {
            Users = new HashSet<User>();
        }

        public int RoleId { get; set; }
        public string Type { get; set; }
        public int SeverityLevel { get; set; }

        public virtual ICollection<User> Users { get; set; }
    }
}

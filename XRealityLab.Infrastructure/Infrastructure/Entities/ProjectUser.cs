using System;
using System.Collections.Generic;

namespace Infrastructure.Entities
{
    public partial class ProjectUser
    {
        public int ProjectUserId { get; set; }
        public int ProjectId { get; set; }
        public int UserId { get; set; }
        public int RoleId { get; set; }

        public virtual Project Project { get; set; }
        public virtual Role Role { get; set; }
        public virtual User User { get; set; }
    }
}

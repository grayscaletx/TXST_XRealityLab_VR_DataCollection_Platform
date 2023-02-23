using System;
using System.Collections.Generic;

#nullable disable

namespace XRealityLab.Entities
{
    public partial class LabUser
    {
        public int LabUserId { get; set; }
        public int LabId { get; set; }
        public int UserId { get; set; }
        public int RoleId { get; set; }

        public virtual Lab Lab { get; set; }
        public virtual Role Role { get; set; }
        public virtual User User { get; set; }
    }
}

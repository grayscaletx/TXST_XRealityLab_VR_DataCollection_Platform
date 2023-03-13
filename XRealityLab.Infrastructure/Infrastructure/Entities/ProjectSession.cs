using System;
using System.Collections.Generic;

namespace Infrastructure.Entities
{
    public partial class ProjectSession
    {
        public int ProjectSessionId { get; set; }
        public int ProjectId { get; set; }
        public int SessionId { get; set; }

        public virtual Project Project { get; set; }
        public virtual Session Session { get; set; }
    }
}

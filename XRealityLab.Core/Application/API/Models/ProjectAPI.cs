using System;
using System.Collections.Generic;
using System.Text;

namespace XRealityLab.API
{
    public class ProjectAPI
    {
        public int ProjectId { get; set; }
        public string FullName { get; set; }
        public string Description { get; set; }
        public DateTime Registered { get; set; }
        public List<UserAPI> projectPIs { get; set; }
    }
}

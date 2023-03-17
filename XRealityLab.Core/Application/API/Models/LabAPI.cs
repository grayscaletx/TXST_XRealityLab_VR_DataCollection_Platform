using System;
using System.Collections.Generic;
using System.Text;

namespace XRealityLab.API
{
    public class LabAPI
    {
        public int LabId { get; set; }
        public string FullName { get; set; }
        public string Description { get; set; }
        public DateTime Registered { get; set; }
    }
}

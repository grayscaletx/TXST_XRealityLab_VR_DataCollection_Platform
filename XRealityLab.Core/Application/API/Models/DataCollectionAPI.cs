using System;
using System.Collections.Generic;
using System.Text;

namespace XRealityLab.API
{
    public class DataCollectionAPI
    {
        public int SessionId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime Registered { get; set; }
        public int participantsNo { get; set; }
    }
}

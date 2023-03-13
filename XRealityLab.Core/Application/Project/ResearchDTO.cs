using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace XRealityLab.Models
{
    public class ResearchDTO
    {
        public int ResearchId { get; set; }
        public string Title { get; set; }
        public int ResearchMethodId { get; set; }
        public string TypeId { get; set; }
        public DateTime Resgistered { get; set; }

    }
}

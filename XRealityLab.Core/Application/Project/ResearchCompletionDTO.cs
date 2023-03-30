using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace XRealityLab.Models
{
    public class ResearchCompletionDTO
    {
        public int ResearchId { get; set; }
        public string Title { get; set; }
        public int ResearchMethodId { get; set; }
        public string TypeId { get; set; }
        public DateTime Resgistered { get; set; }
        public bool Completed { get; set; }
    }
}

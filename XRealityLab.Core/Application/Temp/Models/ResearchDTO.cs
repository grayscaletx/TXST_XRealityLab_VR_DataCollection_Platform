using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ARI.Models
{
    public class ResearchDTO
    {
        public int ResearchId { get; set; }
        public string PassToken { get; set; }
        public string Title { get; set; }
        public int ResearchMethodId { get; set; }
        public DateTime Registered { get; set; }
    }
}

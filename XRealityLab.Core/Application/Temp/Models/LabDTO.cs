using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ARI.Models
{
    public class LabDTO
    {
        public int LabId { get; set; }
        public string PassToken { get; set; }
        public string FullName { get; set; }
        public int LabDirectorId { get; set; }
        public string Description { get; set; }
        public string Color { get; set; }
        public DateTime Registered { get; set; }
    }
}

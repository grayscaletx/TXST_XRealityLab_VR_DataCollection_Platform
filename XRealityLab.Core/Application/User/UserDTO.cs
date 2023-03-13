using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace XRealityLab.Models
{
    public class UserDTO
    {
        public int UserId { get; set; }
        public string Password { get; set; }
        public string PassToken { get; set; }
        [Display(Name = "First name")]
        public string Firstname { get; set; }
        [Display(Name = "Last name")]
        public string Lastname { get; set; }
        [Display(Name = "Email address")]
        public string Email { get; set; }
        [Display(Name = "Telephone")]
        [MinLength(6, ErrorMessage = "Value is too short"), MaxLength(14, ErrorMessage = "Value is too long")]
        [RegularExpression("^[0-9+ ]+$", ErrorMessage = "Value is not acceptable.")]
        [DataType(DataType.PhoneNumber)] public string Telephone { get; set; }        
        public string PhotoUrl { get; set; }
        public int RoleId { get; set; }
        [Display(Name = "Active")]
        public bool IsActive { get; set; }
        [Display(Name = "Registered")]
        public DateTime RegistrationDateTime { get; set; }

        public IEnumerable<SelectListItem> RoleListItems { get; set; }
        [Display(Name = "Ρόλος")]
        public string SelectedRole { get; set; }
        public bool IsInProject { get; set; }
    }
}

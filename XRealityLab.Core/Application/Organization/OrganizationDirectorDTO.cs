using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using XRealityLab.Models;

namespace XRealityLab.Models
{
    public class OrganizationDirectorDTO
    {
        public int OrganizationId { get; set; }
        public string PassToken { get; set; }
        public string FullName { get; set; }
        public int ITDirectorId { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string State { get; set; }
        public string ZIPCode { get; set; }
        public string Color { get; set; }
        public string PhotoUrl { get; set; }
        public string PhotoBase64 { get; set; }
        public int NoAllowedLabs { get; set; }
        public DateTime Registered { get; set; }
        public DateTime SubscriptionStarted { get; set; }
        public DateTime SubscriptionExpiration { get; set; }
        public UserDTO ITDirector { get; set; }
        public LabDTO LabDTO { get; set; }

        public int UserId { get; set; }
        public string UserPassToken { get; set; }
        [Display(Name = "Rank")]
        public int Rank { get; set; }
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
        public string UserPhotoUrl { get; set; }
        public int RoleId { get; set; }
        [Display(Name = "Active")]
        public bool IsActive { get; set; }
        [Display(Name = "Registered")]
        public DateTime RegistrationDateTime { get; set; }
    }
}

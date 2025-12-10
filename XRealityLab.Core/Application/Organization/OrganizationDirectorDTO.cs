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
        [Required(ErrorMessage = "Organization name is required")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Organization name must be between 2 and 50 characters")]
        [Display(Name = "Organization name")]
        public string FullName { get; set; }

        public int ITDirectorId { get; set; }

        [Required(ErrorMessage = "Street is required")]
        [StringLength(50, ErrorMessage = "Street can't be longer than 50 characters")]
        public string Street { get; set; }

        [Required(ErrorMessage = "City is required")]
        [StringLength(50, ErrorMessage = "City can't be longer than 50 characters")]
        public string City { get; set; }

        [Required(ErrorMessage = "Country is required")]
        [StringLength(50, ErrorMessage = "Country can't be longer than 50 characters")]
        public string Country { get; set; }
        [Required(ErrorMessage = "State is required")]
        [StringLength(50)]
        public string State { get; set; }

        [Required(ErrorMessage = "ZIP code is required")]
        [StringLength(10, ErrorMessage = "ZIP code can't be longer than 10 characters")]
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
        [Required(ErrorMessage = "Rank is required")]
        public int Rank { get; set; }

        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, MinimumLength = 2)]
        [Display(Name = "First name")]
        public string Firstname { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, MinimumLength = 2)]
        [Display(Name = "Last name")]
        public string Lastname { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        [Display(Name = "Password")]
        public string Password { get; set; }
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

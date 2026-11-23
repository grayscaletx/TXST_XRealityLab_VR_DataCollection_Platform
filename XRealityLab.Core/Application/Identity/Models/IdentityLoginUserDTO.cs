using System.ComponentModel.DataAnnotations;

namespace XRealityLab.Application
{
    public class IdentityLoginUserDTO
    {
        [Display(Name = "Email address")]
        [Required(ErrorMessage = "Email address is a required field.")]
        [MinLength(2), MaxLength(64)]
        [DataType(DataType.EmailAddress)]
        [EmailAddress]
        public string Email { get; set; }
        [Display(Name = "Password")]
        [Required(ErrorMessage = "Password is a required field.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
        // reCAPTCHA token: only present (and only checked) when reCAPTCHA keys are configured
        public string Token { get; set; }
    }
}

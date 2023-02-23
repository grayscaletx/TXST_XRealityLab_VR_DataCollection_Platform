using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ARI.Models
{
    public class UserDTO
    {
        public int UserId { get; set; }
        public string PassToken { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string Email { get; set; }
        public string Telephone { get; set; }        
        public string PhotoUrl { get; set; }
        public int RoleId { get; set; }
        public byte IsActive { get; set; }
        public DateTime RegistrationDateTime { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace XRealityLab.API
{
    public class UserAPI
    {
        public int UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string AuthToken { get; set; }
        public byte IsActive { get; set; }
        public int RoleId { get; set; }
        public DateTime RegistrationDateTime { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace XRealityLab.API
{
    public class ParticipantsAPI
    {
        public int ParticipantId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public DateTime Registered { get; set; }
    }
}

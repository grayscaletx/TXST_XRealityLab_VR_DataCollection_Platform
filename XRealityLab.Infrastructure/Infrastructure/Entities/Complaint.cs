using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class Complaint
    {
        public int ComplaintId { get; set; }
        public int SubjectId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
        public string Telephone { get; set; }
        public string Description { get; set; }
        public DateTime IssuedDateTime { get; set; }
        public string Documents { get; set; }

        public virtual ComplaintSubject Subject { get; set; }
    }
}

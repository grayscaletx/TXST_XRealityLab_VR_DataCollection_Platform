using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class ComplaintSubject
    {
        public ComplaintSubject()
        {
            Complaints = new HashSet<Complaint>();
        }

        public int ComplaintSubjectId { get; set; }
        public string Name { get; set; }

        public virtual ICollection<Complaint> Complaints { get; set; }
    }
}

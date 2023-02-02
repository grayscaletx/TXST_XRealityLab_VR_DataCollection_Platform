using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class Guest
    {
        public int GuestId { get; set; }
        public int GuestUserId { get; set; }
        public int GuestCompanyId { get; set; }
        public string VisitationPurpose { get; set; }
        public string LicensePlate { get; set; }
        public string Telephone { get; set; }
        public DateTime ArrivalDateTime { get; set; }
        public DateTime? DepartureDateTime { get; set; }
        public string Sign { get; set; }
        public byte IsIncorrect { get; set; }

        public virtual GuestCompany GuestCompany { get; set; }
        public virtual GuestUser GuestUser { get; set; }
    }
}

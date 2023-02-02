using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class CompanyCar
    {
        public CompanyCar()
        {
            CompanyCarReservations = new HashSet<CompanyCarReservation>();
        }

        public int CompanyCarId { get; set; }
        public string Model { get; set; }
        public string LicensePlate { get; set; }

        public virtual ICollection<CompanyCarReservation> CompanyCarReservations { get; set; }
    }
}

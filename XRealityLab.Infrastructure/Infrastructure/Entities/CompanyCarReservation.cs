using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class CompanyCarReservation
    {
        public int CompanyCarReservationId { get; set; }
        public int? UserId { get; set; }
        public int CompanyCarId { get; set; }
        public DateTime ArrivalDateTime { get; set; }
        public DateTime? DepartureDateTime { get; set; }
        public string Sign { get; set; }
        public byte IsIncorrect { get; set; }
        public int? InKilometers { get; set; }
        public int? OutKilometers { get; set; }
        public double? FuelLitre { get; set; }
        public double? FuelCost { get; set; }
        public string Observations { get; set; }
        public string MaintenanceType { get; set; }
        public double? MaintenanceCost { get; set; }
        public DateTime RegistrationDateTime { get; set; }

        public virtual CompanyCar CompanyCar { get; set; }
        public virtual User User { get; set; }
    }
}

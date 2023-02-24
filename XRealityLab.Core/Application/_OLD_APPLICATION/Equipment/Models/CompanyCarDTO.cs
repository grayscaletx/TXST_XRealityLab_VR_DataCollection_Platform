using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace XRealityLab.Models
{
    public class CompanyCarReservationDTO
    {
        public int CompanyCarReservationId { get; set; }
        public string PassToken { get; set; }
        [Display(Name = "Όνομα")]
        public string Firstname { get; set; }
        [Display(Name = "Έπώνυμο")]
        public string Lastname { get; set; }
        public int CompanyCarId { get; set; }
        [Display(Name = "Μοντέλο Αυτοκινήτου")]
        public string CarModel { get; set; }
        [Display(Name = "Πινακίδα")]
        public string CarLicensePlate { get; set; }
        [Required(ErrorMessage = "To πεδίο Ημέρα και ώρα άφιξης είναι υποχρεωτικό πεδίο")]
        [Display(Name = "Ημέρα και ώρα χρέωσης")]
        [DisplayFormat(DataFormatString = "{0:dd MMM yyyy}")]
        [DataType(DataType.DateTime)]
        public DateTime ArrivalDateTime { get; set; }
        [Display(Name = "Ημέρα και ώρα παράδοσης")]
        [DisplayFormat(DataFormatString = "{0:dd MMM yyyy}")]
        [DataType(DataType.DateTime)]
        public DateTime? DepartureDateTime { get; set; }
        [Display(Name = "Χρόνος κράτησης")]
        public int VisitingDuration { get; set; }
        public bool IsIncorrect { get; set; }
        [Display(Name = "Υπογραφή")]
        public string Sign { get; set; }
        [Display(Name = "Χιλιόμετρα χρέωσης")]
        public int InKilometers { get; set; }
        [Display(Name = "Χιλιόμετρα παράδοσης")]
        public int OutKilometers { get; set; }
        //[MinLength(2, ErrorMessage = "Η τιμή δεν είναι αποδεκτή"), MaxLength(5, ErrorMessage = "Η τιμή δεν είναι αποδεκτή")]
        //[RegularExpression("^[0-9.+ ]+$", ErrorMessage = "Η τιμή δεν είναι αποδεκτή")]
        [Display(Name = "Λίτρα Καυσίμου")]
        public double FuelLitre { get; set; }
        //[MinLength(2, ErrorMessage = "Η τιμή δεν είναι αποδεκτή"), MaxLength(5, ErrorMessage = "Η τιμή δεν είναι αποδεκτή")]
        //[RegularExpression("^[0-9+ ]+$", ErrorMessage = "Η τιμή δεν είναι αποδεκτή")]
        [Display(Name = "Κόστος Καυσίμου")]
        public double FuelCost { get; set; }
        [Display(Name = "Είδος συντήρησης")]
        public string MaintenanceType { get; set; }
        //[MinLength(2, ErrorMessage = "Η τιμή δεν είναι αποδεκτή"), MaxLength(5, ErrorMessage = "Η τιμή δεν είναι αποδεκτή")]
        //[RegularExpression("^[0-9.+ ]+$", ErrorMessage = "Η τιμή δεν είναι αποδεκτή")]
        [Display(Name = "Κόστος συντήρησης")]
        public double MaintenanceCost { get; set; }
        [Display(Name = "Παρατηρήσεις")]
        public string Observations { get; set; }
        public DateTime RegistrationDateTime { get; set; }


        public IEnumerable<SelectListItem> UserListItems { get; set; }
        [Display(Name = "Χρήστης")]
        public string SelectedUser { get; set; }

        public IEnumerable<SelectListItem> CarListItems { get; set; }
        [Display(Name = "Εταιρικά αυτοκίνητα")]
        public string SelectedCar { get; set; }
    }
}

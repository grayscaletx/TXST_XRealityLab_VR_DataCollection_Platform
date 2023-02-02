using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ARI.Models
{
    public class GuestDTO
    {
        public int GuestId { get; set; }
        public string PassToken { get; set; }
        [Display(Name = "Όνομα")]
        [Required(ErrorMessage = "To πεδίο Όνομα είναι υποχρεωτικό πεδίο")]
        [MinLength(2, ErrorMessage = "Το πεδίο Όνομα περιέχει πολύ λίγους χαρακτήρες"), MaxLength(15, ErrorMessage = "Το πεδίο Όνομα περιέχει πολλούς χαρακτήρες")]
        //[RegularExpression("[\u0370-\u03ff\u1f00-\u1fffa-zA-Z_ ]+", ErrorMessage = "Το πεδίο Όνομα περιέχει μη αποδεκτούς χαρακτήρες")]
        public string FirstName { get; set; }
        [Display(Name = "Έπώνυμο")]
        [Required(ErrorMessage = "To πεδίο Επώνυμο είναι υποχρεωτικό πεδίο")]
        [MinLength(2, ErrorMessage = "Το πεδίο Επώνυμο περιέχει πολύ λίγους χαρακτήρες"), MaxLength(18, ErrorMessage = "Το πεδίο Επώνυμο περιέχει πολλούς χαρακτήρες")]
        //[RegularExpression("[\u0370-\u03ff\u1f00-\u1fffa-zA-Z_ ]+", ErrorMessage = "Το πεδίο Επώνυμο περιέχει μη αποδεκτούς χαρακτήρες")]
        public string LastName { get; set; }
        [Display(Name = "Εταιρία")]
        public string CompanyName { get; set; }
        [Display(Name = "Σκοπός επίσκεψης")]
        //[MinLength(2, ErrorMessage = "Το πεδίο Σκοπός επίσκεψης περιέχει πολύ λίγους χαρακτήρες"), MaxLength(30, ErrorMessage = "Το πεδίο Επώνυμο περιέχει πολλούς χαρακτήρες")]
        //[Required(ErrorMessage = "To πεδίο Σκοπό επίσκεψης είναι υποχρεωτικό πεδίο")]
        public string VisitationPurpose { get; set; }
        [Display(Name = " ")]
        public string Type { get; set; }
        [Display(Name = "Τηλέφωνο επικοινωνίας")]
        [MinLength(8, ErrorMessage = "Η τιμή δεν είναι αποδεκτή"), MaxLength(13, ErrorMessage = "Η τιμή δεν είναι αποδεκτή")]
        [RegularExpression("^[0-9+ ]+$", ErrorMessage = "Η τιμή δεν είναι αποδεκτή")]
        //[Required(ErrorMessage = "To τηλέφωνο είναι υποχρεωτικό πεδίο")]
        [DataType(DataType.PhoneNumber)]
        public string Telephone { get; set; }
        [Display(Name = "Πινακίδα")]
        [MinLength(4, ErrorMessage = "Η τιμή δεν είναι αποδεκτή"), MaxLength(9, ErrorMessage = "Η τιμή δεν είναι αποδεκτή")]
        public string LicensePlate { get; set; }
        [Display(Name = "Ημέρα και ώρα άφιξης")]
        [DisplayFormat(DataFormatString = "{0:dd MMM yyyy}")]
        [DataType(DataType.DateTime)]
        public DateTime ArrivalDateTime { get; set; }
        [Display(Name = "Ημέρα και ώρα αναχώρησης")]
        [DisplayFormat(DataFormatString = "{0:dd MMM yyyy}")]
        [DataType(DataType.DateTime)]
        public DateTime? DepartureDateTime { get; set; }
        [Display(Name = "Χρόνος εντός της εταιρίας")]
        public int VisitingDuration { get; set; }
        public bool IsIncorrect { get; set; }
        [Display(Name = "Υπογραφή")]
        public string Sign { get; set; }

        public IEnumerable<SelectListItem> CompanyListItems { get; set; }
        [Display(Name = "Εταιρίες")]
        public string SelectedCompany { get; set; }
    }
}

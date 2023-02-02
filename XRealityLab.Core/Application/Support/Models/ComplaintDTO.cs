using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text;

namespace ARI.Application
{
    public class RequestDTO
    {
        public int RequestId { get; set; }
        public string PassToken { get; set; }
        public string Token { get; set; }
        [Required(ErrorMessage = "Το πεδίο ονοματεπώνυμο είναι υποχρεωτικό πεδίο")]
        [MinLength(2, ErrorMessage = "Το πεδίο ονοματεπώνυμο περιέχει πολύ λίγους χαρακτήρες"), MaxLength(30, ErrorMessage = "Το πεδίο ονοματεπώνυμο περιέχει πολλούς χαρακτήρες")]
        [Display(Name = "Ονοματεπώνυμο")]
        public string FullName { get; set; }
        [Required(ErrorMessage = "Το πεδίο ηλεκτρονική διεύθυνση είναι υποχρεωτικό πεδίο")]
        [DataType(DataType.EmailAddress)]
        [Display(Name = "Ηλεκτρονική διεύθυνση")]
        public string Email { get; set; }
        [Display(Name = "Διεύθυνση")]
        public string Street { get; set; }
        [RegularExpression("^[0-9+ ]+$", ErrorMessage = "Η τιμή δεν είναι αποδεκτή")]
        [Display(Name = "Πόλη")]
        public string City { get; set; }
        [Display(Name = "Θέμα")]
        public string ComplaintSubject { get; set; }
        [Display(Name = "Τηλέφωνο επικοινωνίας")]
        [RegularExpression("^[0-9+ ]+$", ErrorMessage = "Η τιμή δεν είναι αποδεκτή")]
        [DataType(DataType.PhoneNumber)]
        public string Telephone { get; set; }
        [Required(ErrorMessage = "Το πεδίο περιγραφή είναι υποχρεωτικό πεδίο")]
        [MinLength(2, ErrorMessage = "Το πεδίο περιγραφή περιέχει πολύ λίγους χαρακτήρες"), MaxLength(999, ErrorMessage = "Το πεδίο περιγραφή περιέχει πολλούς χαρακτήρες")]
        [Display(Name = "Περιγραφή")]
        public string Description { get; set; }
        [Display(Name = "Ημέρα και ώρα αιτήματος")]
        public DateTime SubmissionDateTime { get; set; }
        [Display(Name = "Έγγραφο")]
        public IFormFile iFormFile { get; set; }

        [Display(Name = "Έγγραφο")]
        public string FilePath { get; set; }

        public IEnumerable<SelectListItem> ComplaintSubjectListItems { get; set; }
        [Display(Name = "Είδος παραπόνου")]
        public string SelectedComplaintSubject { get; set; }
    }
}

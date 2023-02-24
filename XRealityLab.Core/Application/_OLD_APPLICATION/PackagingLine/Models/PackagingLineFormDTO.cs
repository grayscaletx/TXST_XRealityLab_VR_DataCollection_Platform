using XRealityLab.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace XRealityLab.Models
{
    public class PackagingLineFormDTO : ValidationAttribute
    {
        public int PackagingLineId { get; set; }
        public string PassToken { get; set; }
        public MaterialDTO Material { get; set; }
        public int MaterialId { get; set; }
        [Display(Name = "Αριθμός παλετών για δημιουργία")]
        [Range(1, 100, ErrorMessage = "Ο μέγιστος αριθμός παλετών για δημιουργία ανά παραγωγή είναι 100")]
        [RegularExpression("([0-9]+)", ErrorMessage = "Ο μέγιστος αριθμός παλετών για δημιουργία ανά παραγωγή είναι 100")]
        public int NoPalletToCreated { get; set; }
        public int NoCompletedPallets { get; set; }
        public string MaterialDescription { get; set; }
        public string PackagingLineDescription { get; set; }
        public int MaterialLOT { get; set; }
        public string CountryCode { get; set; }
        public int NoPalletBox { get; set; }
        public string Notes { get; set; }
        public string QRCodeLink { get; set; }
        public string BarCodeLink { get; set; }
        public string PalletCardAriLink { get; set; }
        public DateTime FormDate { get; set; }
        public DateTime RegistrationDateTime { get; set; }
        public DateTime? CompletionDateTime { get; set; }
        public List<PalletDTO> palletDTOs { get; set; }
        
        public Byte[] _byte { get; set; }

        public IEnumerable<SelectListItem> StatusListItems { get; set; }
        [Display(Name = "Status")]
        public string SelectedStatus { get; set; }
        public int StatusId { get; set; }

        public IEnumerable<SelectListItem> MaterialListItems { get; set; }
        [Display(Name = "Material")]
        public string SelectedMaterial { get; set; }

        public string ActionMessage { get; set; }
    }
}

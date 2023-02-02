using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ARI.Models
{
    public class PalletDTO
    {
        public int PalletId{ get; set; }
        public string PassToken { get; set; }
        public string CountryCode { get; set; }
        public MaterialDTO Material { get; set; }
        public int MaterialId { get; set; }
        public int NoPalletBox { get; set; }
        public int PalletLOT { get; set; }
        public int PackagingLinePalletNo { get; set; }
        public bool IsVirtual { get; set; }
        public string QRCodeLink { get; set; }
        public string BarCodeLink { get; set; }
        public string PalletCardLink { get; set; }
        public string PalletCardLIDLLink { get; set; }
        public string SSCC { get; set; }
        public string CreationDateTime { get; set; }
        
        public IEnumerable<SelectListItem> StatusListItems { get; set; }
        [Display(Name = "Status")]
        public string SelectedStatus { get; set; }
        public int StatusId { get; set; }
        public int PackagingLineId { get; set; }

        public int LoadingAreaID { get; set; }
        public int LoadingLine { get; set; }
        public int LoadingPosition { get; set; }

        public bool IsSelectedForLoad { get; set; }
    }
}

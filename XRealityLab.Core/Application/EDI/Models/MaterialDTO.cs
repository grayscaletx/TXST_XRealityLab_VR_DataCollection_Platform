using System;
using System.Collections.Generic;
using System.Text;

namespace ARI.Models
{
    public class MaterialDTO
    {
        public int MaterialId { get; set; }
        public string MaterialSAPID { get; set; }
        public string PassToken { get; set; }
        public string MaterialDescription { get; set; }
        public string PackagingLineDescription { get; set; }
        public int MaterialLOT { get; set; }
        public string CustomerMaterialNumber { get; set; }
        public string ProductFamilyName { get; set; }
        public string Seasonality { get; set; }
        public string Customer { get; set; }
        public string LanguageCode { get; set; }
        public string Name { get; set; }
        public int PalletFactor { get; set; }
        public string BUn { get; set; }
        public string GTIN { get; set; }
        public string EANUPC { get; set; }
        public int PiecesPerCarton { get; set; }
        public double GrossWeight { get; set; }
        public double NetWeight { get; set; }
        public double Height { get; set; }
        public double PalletCost { get; set; }
        public double FirstTransport { get; set; }
        public double SecondTransport { get; set; }
        public double ThirdTransport { get; set; }
        public double FourthTransport { get; set; }
        public double FifthTransport { get; set; }
        public double FinalPrice { get; set; }
        public int LagerDays { get; set; }
        public int ExpirationDays { get; set; }
        public DateTime ExpirationDate { get; set; }
        public DateTime MaxDeliveryDate { get; set; }
    }
}

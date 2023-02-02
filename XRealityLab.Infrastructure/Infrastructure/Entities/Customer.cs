using System;
using System.Collections.Generic;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class Customer
    {
        public int CustomerId { get; set; }
        public string CustomerSapid { get; set; }
        public string Name1 { get; set; }
        public string Name2 { get; set; }
        public string SearchTermsCode { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
        public int? CountryId { get; set; }
        public string LocationNo1 { get; set; }
        public string LocationNo2 { get; set; }
        public string CheckDigit { get; set; }
        public string TaxNumber1 { get; set; }
        public string TaxNumber2 { get; set; }
        public string VatregNo { get; set; }
        public string PostalCode { get; set; }
        public int? Address { get; set; }

        public virtual Country Country { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace ARI.Models
{
    public class CustomerDTO
    {
        public int CustomerId { get; set; }
        public string CustomerSAPID { get; set; }
        public string Name1 { get; set; }
        public string Name2 { get; set; }
        public string SearchTermCode { get; set; }
       
        public string Street { get; set; }
        public string Address { get; set; }
        public string PostalCode { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string GTIN { get; set; }
    }
}

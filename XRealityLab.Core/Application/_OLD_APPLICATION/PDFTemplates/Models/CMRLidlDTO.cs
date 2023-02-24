using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace XRealityLab.Models
{
    public class CMRLidlDTO
    {
        public SenderDTO Sender { get; set; }
        public ConsigneeDTO Consignee { get; set; }
        public PlaceOfDeliveryDTO PlaceOfDelivery { get; set; }
        public PlaceAndDateDTO placeAndDate { get; set; }
        public string Carrier { get; set; }
        public string DeliveryNote { get; set; }
        public string Reference { get; set; }
        public IEnumerable<ProductItem> ProductItems { get; set; }
        public float Total { get; set; }
        public string TemporaryStorageTo { get; set; }
        public string TransportTemperature { get; set; }
        public string EstablishedIn { get; set; }
        public DateTime EstablishedOn { get; set; }
    }

    public class SenderDTO
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public string Area { get; set; }
        public string Country { get; set; }
    }

    public class ConsigneeDTO
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public string Area { get; set; }
        public string Country { get; set; }
    }

    public class PlaceOfDeliveryDTO
    {
        public string Place { get; set; }
        public string Area { get; set; }
        public string Country { get; set; }
    }

    public class PlaceAndDateDTO
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public string Area { get; set; }
        public string Country { get; set; }
    }

    public class MarksAndNosDTO
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public string Area { get; set; }
        public string Country { get; set; }
    }

    public class ProductItem
    {
        public string MarksAndNos { get; set; }
        public int NumberOfCartons { get; set; }
        public int NumberOfPallets { get; set; }
        public float GrossWeight { get; set; }
    }
}

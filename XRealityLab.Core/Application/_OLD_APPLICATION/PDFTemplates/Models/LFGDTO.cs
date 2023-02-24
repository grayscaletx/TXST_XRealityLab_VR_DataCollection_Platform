using System;
using System.Collections.Generic;
using System.Text;

namespace XRealityLab.PDFTemplates.Models
{
    public class LFGDTO
    {
        public SupplierDTO Supplier {get;set;}
        public GoodsRecipientDTO goodsRecipient { get; set; }
        public DateTime Date { get; set; }
        public string DeliveryDateNo { get; set; }
        public string OrderNo  { get; set; }
        public DateTime DeliveryDate { get; set; }
        public List<DeliveryProductItem> ProductItems { get; set; }
        public string PalletNotes { get; set; }
        public string AccountingDepartment { get; set; }
    }

    public class SupplierDTO
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public string Area { get; set; }
        public string VAT { get; set; }
        public string Tel { get; set; }
        public string Fax { get; set; }
        public string EBEX { get; set; }
        public int SupplierNumber { get; set; }
    }

    public class GoodsRecipientDTO
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public string Area { get; set; }
        public string Country { get; set; }
    }

    public class DeliveryProductItem
    {
        public int ItemId { get; set; }
        public List<DeliverySubProductItem>  DeliveryProductSubItem {get;set;}
        public string ItemDescription { get; set; }
        public int CaseSize { get; set; }
        public int BatchNo { get; set; }
        public DateTime BestBefore { get; set; }
        public int CaseQuantity { get; set; }
        public int PalletAmount { get; set; }
        public int PalletFactor { get; set; }
        public float NetWeight { get; set; }
        public float GrossWeightPallet { get; set; }
        public float GrossWeightTotal { get; set; }
    }

    public class DeliverySubProductItem
    {
        public string ItemDescription { get; set; }
        public int CaseSize { get; set; }
        public int CaseQuantity { get; set; }
    }
}

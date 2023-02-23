using Application.Security;
using ARI.Common;
using ARI.Models;
using AzureStorage;
using Common.Constants;
using EnumsNET;
using Infrastructure.Entities;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ZXing.QrCode;

namespace ARI.Helpers
{
    public class ClassConverters
    {
        //Palletcard Dialogs
        /*public static PalletDTO GetPalletInfo(ARIContext _context, CustomIDataProtection _protector, int _palletId)
        {
            var packagingLineFormId = _context.PackagingLineFormPallets.Include(k => k.PackagingLineForm).FirstOrDefault(l => l.PalletId == _palletId).PackagingLineFormId;
            var formCreationDate = _context.PackagingLineForms.FirstOrDefault(k => k.PackagingLineFormId == packagingLineFormId).FormCreationDate;

            return _context.Pallets.Include(k => k.Material).Include(k => k.Country).Where(k => k.PalletId == _palletId).Select(k => new PalletDTO
            {
                PalletId = k.PalletId,
                PassToken = _protector.Decode(k.PalletId.ToString()),
                Material = new MaterialDTO
                {
                    MaterialSAPID = k.Material.MaterialSapid.TrimStart('0'),
                    MaterialLOT = ClassConverters.GetLot(formCreationDate),
                    MaterialDescription = k.Material.MaterialDescription,
                    PackagingLineDescription = k.Material.PackagingLineDescription,
                    MaxDeliveryDate = ClassConverters.GetMaxDeliveryDate(formCreationDate, k.Material.LagerDays),
                    ExpirationDate = ClassConverters.GetExpirationDate(formCreationDate, k.Material.ExpirationDays)
                },
                CountryCode = k.Material.Country.Code,
                PackagingLinePalletNo = k.PackagingLinePalletNo,
                IsVirtual = k.IsVirtual == 1 ? true : false,
                QRCodeLink = k.Qrcode,
                PalletLOT = k.PalletLot,
                BarCodeLink = k.BarCode,
                PalletCardLink = k.PalletCard,
                PalletCardLIDLLink = k.PalletCardLidl,
                NoPalletBox = k.Material.PalletFactor,
                StatusId = k.Status,
                SelectedStatus = ((PalletStatus)k.Status).AsString(EnumFormat.Description),
                CreationDateTime = k.PalletCreationDateTime.Value.ToString("HH:mm")
            }).FirstOrDefault();
        }
        //Pallet card ARI
        public static async void GenerateQRCode(IHostingEnvironment _environment, string scheme, HostString host,string pathbase, 
            string _palletPassToken, string _randomIndex)
        {
            QRCodeGenerator _qrCode = new QRCodeGenerator();
            QRCodeData _qrCodeData = _qrCode.CreateQrCode($"{scheme}://{host}{pathbase}" + "/PackagingLine/PalletQRInfo?passToken=" + _palletPassToken, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(_qrCodeData);
            System.Drawing.Bitmap qrCodeImage = qrCode.GetGraphic(20);
            string randomNewName = "ari_qrcode_" + _randomIndex + ".jpg";
            qrCodeImage.Save(Path.Combine(_environment.WebRootPath, "images", "temp-qr-codes") + $@"\" + randomNewName);
            bool isUploaded = await ContainerService.UploadOperationAsync(Path.Combine(_environment.WebRootPath, "images", "temp-qr-codes") + $@"\", randomNewName, AzureGPV2Credentials.CONTAINER_QR_CODES);
        }
        public static async void GenerateBarCode(IHostingEnvironment _environment, CustomIDataProtection _protector, string _palletPassToken, string _randomIndex)
        {
            var width = 400;
            var height = 800;
            var barcodeWriterPixelData = new ZXing.BarcodeWriterPixelData
            {
                Format = ZXing.BarcodeFormat.CODE_128,
                Options = new QrCodeEncodingOptions
                {
                    Height = height,
                    Width = width,
                    PureBarcode = false,
                    Margin = 10,
                }
            };

            var pixelData = barcodeWriterPixelData.Write("(00)" + _protector.Encode(_palletPassToken));
            using (var bitmap = new System.Drawing.Bitmap(pixelData.Width, pixelData.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb))
            {
                using (var memoryStream = new MemoryStream())
                {
                    var bitmapData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, pixelData.Width, pixelData.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
                    try
                    {
                        System.Runtime.InteropServices.Marshal.Copy(pixelData.Pixels, 0, bitmapData.Scan0, pixelData.Pixels.Length);
                    }
                    finally
                    {
                        bitmap.UnlockBits(bitmapData);
                    }
                    string randomNewName = "ari_barcode_" + _randomIndex + ".jpg";
                    bitmap.Save(Path.Combine(_environment.WebRootPath, "images", "temp-bar-codes") + $@"\" + randomNewName);
                    bool isUploaded = await ContainerService.UploadOperationAsync(Path.Combine(_environment.WebRootPath, "images", "temp-bar-codes") + $@"\", randomNewName, AzureGPV2Credentials.CONTAINER_BAR_CODES);
                }
            }
        }
        public static async void GeneratePalletCardARI(ARIContext _context, IHostingEnvironment _environment, CustomIDataProtection _protector,
            string _packagingLinePassToken, string _palletPassToken, string _randomIndex, int _packagingLineStep)
        {
            var packagingLineForm = _context.PackagingLineForms.Include(k => k.Material).ThenInclude(k => k.Country).Where(k => k.PackagingLineFormId == int.Parse(_protector.Encode(_packagingLinePassToken))).FirstOrDefault();
            string PDFNewFileName = "pallet_card_ari_" + _randomIndex + ".pdf";
            string templateDirectory = Path.Combine(_environment.WebRootPath, "pdf-templates") + $@"\" + PDFTemplateUrl.PALLET_CARD_ARI_NAME;
            string filePathDirectory = Path.Combine(_environment.WebRootPath, "pdf-files") + $@"\" + PDFNewFileName;
            try
            {
                using (var templateFileStream = new System.IO.FileStream(templateDirectory, System.IO.FileMode.Open))
                {
                    // Open existing PDF
                    var pdfReader = new PdfReader(templateFileStream);
                    Rectangle size = pdfReader.GetPageSizeWithRotation(1);
                    iTextSharp.text.Document document = new iTextSharp.text.Document(size);

                    // open the writer
                    FileStream fs = new FileStream(filePathDirectory, FileMode.Create, FileAccess.Write);
                    PdfWriter writer = PdfWriter.GetInstance(document, fs);
                    document.Open();

                    // the pdf content
                    PdfContentByte cb = writer.DirectContent;

                    // create the new page and add it to the pdf
                    PdfImportedPage page = writer.GetImportedPage(pdfReader, 1);
                    cb.AddTemplate(page, 0, 0);

                    //Add QR code image
                    string randomQRNewName = "ari_qrcode_" + _randomIndex + ".jpg";
                    iTextSharp.text.Image jpgQRCode = iTextSharp.text.Image.GetInstance(Path.Combine(_environment.WebRootPath, "images", "temp-qr-codes") + $@"\" + randomQRNewName);
                    jpgQRCode.SetAbsolutePosition(102.5f, 58);
                    jpgQRCode.ScaleAbsolute(new iTextSharp.text.Rectangle(91, 91));
                    cb.AddImage(jpgQRCode);

                    //Add QR code image
                    string randomNewName = "ari_barcode_" + _randomIndex + ".jpg";
                    iTextSharp.text.Image jpgBarRCode = iTextSharp.text.Image.GetInstance(Path.Combine(_environment.WebRootPath, "images", "temp-bar-codes") + $@"\" + randomNewName);
                    jpgBarRCode.SetAbsolutePosition(40, 16);
                    jpgBarRCode.ScaleAbsolute(new iTextSharp.text.Rectangle(200, 30));
                    cb.AddImage(jpgBarRCode);

                    // select the font properties
                    BaseFont bf = BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                    BaseFont nf = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                    cb.SetColorFill(BaseColor.DARK_GRAY);

                    //Sender
                    cb.BeginText();
                    cb.SetFontAndSize(bf, 9);
                    cb.ShowTextAligned(0, "Material:", 13, 182, 0);
                    cb.ShowTextAligned(0, "LOT:", 13, 165, 0);
                    cb.ShowTextAligned(0, "Pallet boxes:", 63, 165, 0);
                    cb.ShowTextAligned(0, "Country:", 141, 165, 0);
                    cb.ShowTextAligned(0, "Expires:", 205, 165, 0);

                    cb.SetFontAndSize(nf, 9);
                    cb.ShowTextAligned(0, packagingLineForm.Material.MaterialDescription, 53, 182, 0);

                    cb.ShowTextAligned(0, (GetLot(packagingLineForm.FormCreationDate)).ToString(), 38, 165, 0);
                    cb.ShowTextAligned(0, packagingLineForm.Material.PalletFactor.ToString(), 122, 165, 0);
                    cb.ShowTextAligned(0, packagingLineForm.Material.Country.Code, 181, 165, 0);
                    cb.ShowTextAligned(0, GetExpirationDate(packagingLineForm.FormCreationDate, packagingLineForm.Material.ExpirationDays).ToString("dd/MM/yyyy"), 244, 165, 0);
                    cb.SetFontAndSize(bf, 10);
                    cb.ShowTextAligned(0, _packagingLineStep.ToString() + "/" + packagingLineForm.NoPalletToCreated, 258, 182, 0);

                    cb.SetFontAndSize(bf, 7);
                    cb.ShowTextAligned(0, "ARI plaform code:", 192, 52, 0);
                    cb.SetFontAndSize(nf, 7);
                    cb.ShowTextAligned(0, "KES" + packagingLineForm.PackagingLineFormId.ToString() + "P" + _protector.Encode(_palletPassToken), 255, 52, 0);

                    cb.EndText();
                    document.Close();
                    fs.Close();
                    writer.Close();
                    pdfReader.Close();
                }
            }
            catch (Exception ex)
            {

            }

            //Upload to cloud
            bool isUploaded = await ContainerService.UploadOperationAsync(Path.Combine(_environment.WebRootPath, "pdf-files") + $@"\", PDFNewFileName, AzureGPV2Credentials.CONTAINER_PDF_FILES);
        }

        //Production
        public static int GetLot(DateTime formDateTime)
        {
            DateTime referenceDate = new DateTime(2021, 1, 1);
            TimeSpan difference = formDateTime - referenceDate;
            return difference.Days + 466;
        }
        public static DateTime GetExpirationDate(DateTime? formDateTime, int expirationDays)
        {
            return formDateTime.HasValue? formDateTime.Value.AddDays(expirationDays - 1) : DateTime.Now.AddYears(-20);
        }
        public static DateTime GetMaxDeliveryDate(DateTime? formDateTime, int lagerDays)
        {
            return formDateTime.HasValue? formDateTime.Value.AddDays(lagerDays - 1) : DateTime.Now.AddYears(-20);
        }

        //Utils
        public static string RandomString(int length)
        {
            Random random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }*/
    }
}

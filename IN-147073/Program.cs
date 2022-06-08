using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Accusoft.SmartZoneOCRSdk;
using Accusoft.ScanFixXpressSdk;
using System.Drawing;
using ImageGear.Formats;
using ImageGear.Formats.PDF;
using ImageGear.Core;
using ImageGear.Processing;
using Accusoft.PdfXpressSdk;

namespace IN_147073
{
    internal class Program
    {

        static DirectoryInfo input = new DirectoryInfo(@"..\..\..\input\");
        static DirectoryInfo output = new DirectoryInfo(@"..\..\..\output\");
        static DirectoryInfo outputOCR = new DirectoryInfo(@"..\..\..\output\ocr\");
        static string creditCardRegEx = @"\d{3}-?\d{2}-?\d{4}";
        static string uSPhoneNumerRegEx = @"(\(?\d{3}\)?-?)?\d{3}-?\d{4}";
        static string ccRegEx = @"(\d{4} ){3}\d{4}";
        static string oCRDataPath;
        static string cMapPath;
        static string fontPath;
        static void Main(string[] args)
        {
            ImGearLicense.SetSolutionName("VeritasTechnologiesLLC");
            ImGearLicense.SetSolutionKey(-568136380, 1920905082, 1287454532, -98288769);

            ImGearCommonFormats.Initialize();

            ImGearFileFormats.Filters.Insert(0, ImGearPDF.CreatePDFFormat());

            ImGearPDF.Initialize();

            ImGearPDFDocument imGearPDFDocument = null;

            ImGearPage imGearPage = null;

            foreach (FileInfo file in input.GetFiles())
                switch (file.Extension)
                {
                    case ".pdf":
                        using (FileStream fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            imGearPDFDocument = ImGearFileFormats.LoadDocument(fs, 0, -1) as ImGearPDFDocument;
                            foreach (int page in Enumerable.Range(0, imGearPDFDocument.Pages.Count))
                            {
                                using (ImGearPDFPage imGearPDFPage = imGearPDFDocument.Pages[page] as ImGearPDFPage)
                                {
                                    ImGearRasterPage imGearRasterPage = imGearPDFPage.Rasterize(imGearPDFPage.DIB.BitDepth, 300, 300);

                                    using (FileStream outputStream = new FileStream($"{output}{Path.GetFileNameWithoutExtension(file.FullName)} - {page}.bmp", FileMode.Create))
                                        ImGearFileFormats.SavePage(imGearRasterPage, outputStream, ImGearSavingFormats.BMP_UNCOMP);
                                }
                            }
                        }
                        imGearPDFDocument?.Dispose();
                        ProcessFiles();
                        break;
                    case ".png":
                        using (FileStream fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
                            imGearPage = ImGearFileFormats.LoadPage(fs, 0);

                        using (FileStream outputStream = new FileStream($"{output}{Path.GetFileNameWithoutExtension(file.FullName)}.bmp", FileMode.Create))
                            ImGearFileFormats.SavePage(imGearPage, outputStream, ImGearSavingFormats.BMP_UNCOMP);
                        ProcessFiles();
                        break;
                    default:
                        break;
                }

            ImGearPDF.Terminate();
        }

        private static void ProcessFiles()
        {
            using (SmartZoneOCR smartZone = new SmartZoneOCR())
            {
                smartZone.Licensing.SetSolutionName("Accusoft");

                smartZone.Reader.CharacterSet = CharacterSet.AllCharacters;

                smartZone.Reader.CharacterSet.Language = Language.English;

                foreach (FileInfo file in output.GetFiles())
                {
                    using (Bitmap bitmap = (Bitmap)Image.FromFile(file.FullName))
                    {
                        smartZone.Reader.Area = new Rectangle(0, 0, bitmap.Width, bitmap.Height);

                        var fileNameOCR = $"{outputOCR}{Path.GetFileNameWithoutExtension(file.FullName)}.txt";

                        using (StreamWriter writer = new StreamWriter(fileNameOCR, false))
                        {
                            TextBlockResult result = smartZone.Reader.AnalyzeField(bitmap);
                            writer.Write(result.Text);
                        }
                    }
                }
            }
        }


        private static void ProccessFiles_SmartZone()
        {

            using (PdfXpress pdf = new PdfXpress())
            {
                pdf.Licensing.SetSolutionName("");
                pdf.Licensing.SetSolutionKey(000000, 000001, 0000002, 0000003);
                pdf.Licensing.SetOEMLicenseKey("000000");
                pdf.Initialize(fontPath, cMapPath);

                OpenOptions openOptions = new OpenOptions();
                using (SmartZoneOCR smrtZoneOCR = new SmartZoneOCR())
                {
                    smrtZoneOCR.Licensing.SetSolutionName("");
                    smrtZoneOCR.Licensing.SetSolutionKey(000000, 000001, 0000002, 0000003);
                    smrtZoneOCR.Licensing.SetOEMLicenseKey("");
                    smrtZoneOCR.OCRDataPath = oCRDataPath;


                    var pdfdoc = pdf.Documents.Add("fileName");
                    RenderOptions renderOptions = new RenderOptions();
                    TextFinderOptions finderOptions = new TextFinderOptions();
                    ExtractImageOptions extractOptions = new ExtractImageOptions();
                    var doc = pdf.Documents[pdfdoc];


                    renderOptions.ResolutionX = 300;
                    renderOptions.ResolutionY = 300;

                    for (int pageNumber = 0; pageNumber < pdf.Documents[pdfdoc].PageCount; pageNumber++)
                    {
                        using (Bitmap bitmap = pdf.Documents[pdfdoc].RenderPageToBitmap(pageNumber, renderOptions))
                        {

                            smrtZoneOCR.Reader.CharacterSet = CharacterSet.AllCharacters;
                            smrtZoneOCR.Reader.CharacterSet.Language = Language.English;
                            smrtZoneOCR.Reader.Area = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
                            smrtZoneOCR.Reader.SetRegularExpression(creditCardRegEx);
                            smrtZoneOCR.Reader.SetRegularExpression(uSPhoneNumerRegEx);
                            smrtZoneOCR.Reader.SetRegularExpression(ccRegEx);

                            var result = smrtZoneOCR.Reader.AnalyzeField(bitmap);



                        }
                    }
                }


            }
        }
    }
}

using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using iText.Kernel.Geom;
using iText.Layout.Properties;
using iText.Kernel.Colors;
using iText.Kernel.Pdf.Canvas;

namespace SistemaCalidad.Api.Services;

public interface IWatermarkService
{
    byte[] ApplyWatermark(byte[] pdfContent, string userName, string info);
}

public class WatermarkService : IWatermarkService
{
    public byte[] ApplyWatermark(byte[] pdfContent, string userName, string info)
    {
        using (var msInput = new MemoryStream(pdfContent))
        using (var msOutput = new MemoryStream())
        {
            var pdfReader = new PdfReader(msInput);
            var pdfWriter = new PdfWriter(msOutput);
            var pdfDoc = new PdfDocument(pdfReader, pdfWriter);
            var document = new Document(pdfDoc);

            var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var watermarkText = $"COPIA NO CONTROLADA - Usuario: {userName} - {info} - Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}";

            int n = pdfDoc.GetNumberOfPages();
            for (int i = 1; i <= n; i++)
            {
                var page = pdfDoc.GetPage(i);
                var pageSize = page.GetPageSize();
                float x = pageSize.GetWidth() / 2;
                float y = pageSize.GetHeight() - 20;

                // Agregar marca de agua en la parte superior
                new Canvas(page, pageSize)
                    .SetFont(font)
                    .SetFontSize(8)
                    .SetFontColor(DeviceGray.GRAY)
                    .ShowTextAligned(new Paragraph(watermarkText), x, y, i, TextAlignment.CENTER, VerticalAlignment.TOP, 0)
                    .Close();
                
                // Marca de agua diagonal en el centro (opcional, pero profesional)
                var canvas = new PdfCanvas(page);
                canvas.SaveState();
                canvas.SetFillColor(DeviceGray.GRAY);
                canvas.SetExtGState(new iText.Kernel.Pdf.Extgstate.PdfExtGState().SetFillOpacity(0.2f));
                
                new Canvas(canvas, pageSize)
                    .SetFont(font)
                    .SetFontSize(40)
                    .ShowTextAligned(new Paragraph("SGC - CONFIDENCIAL"), x, pageSize.GetHeight() / 2, i, TextAlignment.CENTER, VerticalAlignment.MIDDLE, (float)Math.PI / 4)
                    .Close();
                
                canvas.RestoreState();
            }

            document.Close();
            return msOutput.ToArray();
        }
    }
}

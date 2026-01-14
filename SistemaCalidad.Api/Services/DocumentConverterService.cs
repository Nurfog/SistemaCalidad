using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Html2pdf;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using Mammoth;
using System.Text;

namespace SistemaCalidad.Api.Services;

public interface IDocumentConverterService
{
    byte[] ConvertToPdf(byte[] content, string extension);
    string ExtractText(byte[] content, string extension);
}

public class DocumentConverterService : IDocumentConverterService
{
    public byte[] ConvertToPdf(byte[] content, string extension)
    {
        return extension.ToLower() switch
        {
            ".docx" => ConvertDocxToPdf(content),
            ".txt" => ConvertTxtToPdf(content),
            ".pdf" => content,
            _ => throw new NotSupportedException($"La conversión para la extensión {extension} no está soportada.")
        };
    }

    public string ExtractText(byte[] content, string extension)
    {
        return extension.ToLower() switch
        {
            ".docx" => ExtractTextFromDocx(content),
            ".pdf" => ExtractTextFromPdf(content),
            ".txt" => Encoding.UTF8.GetString(content),
            _ => string.Empty
        };
    }

    private string ExtractTextFromDocx(byte[] content)
    {
        using (var msInput = new MemoryStream(content))
        {
            var converter = new DocumentConverter();
            var result = converter.ExtractRawText(msInput);
            return result.Value;
        }
    }

    private string ExtractTextFromPdf(byte[] content)
    {
        using (var msInput = new MemoryStream(content))
        using (var reader = new PdfReader(msInput))
        using (var pdfDoc = new PdfDocument(reader))
        {
            var text = new StringBuilder();
            for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
            {
                var page = pdfDoc.GetPage(i);
                var strategy = new iText.Kernel.Pdf.Canvas.Parser.Listener.LocationTextExtractionStrategy();
                var currentText = iText.Kernel.Pdf.Canvas.Parser.PdfTextExtractor.GetTextFromPage(page, strategy);
                text.AppendLine(currentText);
            }
            return text.ToString();
        }
    }

    private byte[] ConvertDocxToPdf(byte[] content)
    {
        using (var msInput = new MemoryStream(content))
        using (var msOutput = new MemoryStream())
        {
            var converter = new DocumentConverter();
            var result = converter.ConvertToHtml(msInput);
            var htmlContent = result.Value;

            // Convertir HTML a PDF usando iText7.pdfHTML
            HtmlConverter.ConvertToPdf(htmlContent, msOutput);
            
            return msOutput.ToArray();
        }
    }

    private byte[] ConvertTxtToPdf(byte[] content)
    {
        var text = Encoding.UTF8.GetString(content);
        using (var msOutput = new MemoryStream())
        {
            var writer = new PdfWriter(msOutput);
            writer.SetCloseStream(false); // Mantener stream abierto para ToArray seguro
            var pdf = new PdfDocument(writer);
            var document = new Document(pdf);
            
            // Usar fuente estándar explícita para evitar errores de fuente no encontrada
            var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            document.SetFont(font);

            var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            foreach (var line in lines)
            {
                // Reemplazar caracteres nulos o problemáticos
                var safeLine = line.Replace("\0", ""); 
                document.Add(new Paragraph(safeLine).SetFontSize(11));
            }

            document.Close();
            msOutput.Position = 0; // Rewind por si acaso
            return msOutput.ToArray();
        }
    }
}

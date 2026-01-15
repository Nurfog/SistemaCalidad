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
    string DetectExtension(byte[] content);
}

public class DocumentConverterService : IDocumentConverterService
{
    public byte[] ConvertToPdf(byte[] content, string extension)
    {
        return extension.ToLower() switch
        {
            ".docx" => ConvertDocxToPdf(content),
            ".doc" => ConvertTxtToPdf(content), // Fallback: DOC es binario complejo, lo tratamos como posible texto extraído si se pudiere
            ".xlsx" => ConvertExcelToPdf(content),
            ".xls" => ConvertExcelToPdf(content),
            ".txt" => ConvertTxtToPdf(content),
            ".pdf" => content,
            _ => throw new NotSupportedException($"La conversión visual para la extensión {extension} no está soportada.")
        };
    }

    public string ExtractText(byte[] content, string extension)
    {
        try 
        {
            var ext = extension.ToLower();
            
            // Lista de extensiones conocidas
            var knownExtensions = new HashSet<string> { ".docx", ".doc", ".pdf", ".xlsx", ".xls", ".txt", ".rtf" };

            // Si es desconocida o vacía, intentar detectar por Magic Bytes
            if (string.IsNullOrWhiteSpace(ext) || !knownExtensions.Contains(ext))
            {
                var detected = DetectExtension(content);
                if (!string.IsNullOrEmpty(detected))
                {
                    ext = detected;
                    // Console.WriteLine($"[Converter] Extensión corregida de '{extension}' a '{ext}'"); // Debug
                }
            }

            return ext switch
            {
                ".docx" => ExtractTextFromDocx(content),
                ".doc" => ExtractTextFromDocLegacy(content),
                ".pdf" => ExtractTextFromPdf(content),
                ".xlsx" => ExtractTextFromExcel(content),
                ".xls" => ExtractTextFromExcel(content),
                ".txt" => Encoding.UTF8.GetString(content),
                ".rtf" => ExtractTextFromRtf(content),
                _ => string.Empty
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Converter] Error extrayendo texto de {extension}: {ex.Message}");
            return string.Empty; 
        }
    }

    public string DetectExtension(byte[] content)
    {
        if (content == null || content.Length < 4) return "";

        // PDF: %PDF (25 50 44 46)
        if (content[0] == 0x25 && content[1] == 0x50 && content[2] == 0x44 && content[3] == 0x46)
            return ".pdf";

        // OLE (Legacy Office: .doc, .xls, .ppt): D0 CF 11 E0
        if (content[0] == 0xD0 && content[1] == 0xCF && content[2] == 0x11 && content[3] == 0xE0)
        {
            // Difícil distinguir DOC de XLS por header puro sin parsear,
            // pero podemos intentar ExcelReader primero, si falla probar DocLegacy
            // Por defecto retornamos .doc para que caiga en el fallback, o .xls
            // Vamos a retornar un marcador interno o probar ambos.
            // Para simplificar, asumiremos .doc (Legacy) que usa el extractor de texto ASCII,
            // pero si es Excel binario (.xls), el extractor ASCII sacará basura.
            // Mejor: Intentaremos leer como Excel.
            try 
            {
                 // Check rápido si es xls válido
                 using (var ms = new MemoryStream(content))
                 using (var reader = ExcelDataReader.ExcelReaderFactory.CreateBinaryReader(ms))
                 {
                     return ".xls";
                 }
            }
            catch {}
            return ".doc";
        }

        // ZIP (OpenXML: .docx, .xlsx): 50 4B 03 04
        if (content[0] == 0x50 && content[1] == 0x4B && content[2] == 0x03 && content[3] == 0x04)
        {
             // Diferenciar docx de xlsx buscando [Content_Types].xml o similar es costoso.
             // Probaremos abrir como Excel, si funciona es xlsx, sino docx.
            try 
            {
                 using (var ms = new MemoryStream(content))
                 using (var reader = ExcelDataReader.ExcelReaderFactory.CreateOpenXmlReader(ms))
                 {
                     return ".xlsx";
                 }
            }
            catch {}
            return ".docx";
        }

        // TXT (Heurística simple: no tiene nulos y caracteres legibles)
        if (!content.Take(50).Any(b => b == 0)) return ".txt";

        // RTF: {\rtf (7B 5C 72 74 66)
        if (content.Length >= 5 && content[0] == 0x7B && content[1] == 0x5C && content[2] == 0x72 && content[3] == 0x74 && content[4] == 0x66)
            return ".rtf";

        return "";
    }

    private string ExtractTextFromDocx(byte[] content)
    {
        using var msInput = new MemoryStream(content);
        var converter = new DocumentConverter();
        var result = converter.ExtractRawText(msInput);
        return result.Value;
    }
    
    private string ExtractTextFromDocLegacy(byte[] content)
    {
        try 
        {
             return Encoding.ASCII.GetString(content.Where(b => b >= 32 && b <= 126).ToArray());
        }
        catch { return ""; }
    }

    private string ExtractTextFromRtf(byte[] content)
    {
        try 
        {
            var rtf = Encoding.UTF8.GetString(content);
            // Extracción muy básica usando Regex para quitar tags, mejor que nada.
            return System.Text.RegularExpressions.Regex.Replace(rtf, @"\\[\w]+|[{}]|\\\n|\\\r", "").Trim();
        }
        catch { return ""; }
    }

    private string ExtractTextFromPdf(byte[] content)
    {
        using var msInput = new MemoryStream(content);
        using var reader = new PdfReader(msInput);
        using var pdfDoc = new PdfDocument(reader);
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

    private string ExtractTextFromExcel(byte[] content)
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        using var stream = new MemoryStream(content);
        using var reader = ExcelDataReader.ExcelReaderFactory.CreateReader(stream);
        
        var text = new StringBuilder();
        do
        {
            while (reader.Read())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var val = reader.GetValue(i)?.ToString();
                    if (!string.IsNullOrWhiteSpace(val))
                    {
                        text.Append(val + " ");
                    }
                }
                text.AppendLine();
            }
        } while (reader.NextResult()); // Siguiente hoja

        return text.ToString();
    }

    private byte[] ConvertDocxToPdf(byte[] content)
    {
        using var msInput = new MemoryStream(content);
        using var msOutput = new MemoryStream();
        var converter = new DocumentConverter();
        var result = converter.ConvertToHtml(msInput);
        var htmlContent = result.Value;
        HtmlConverter.ConvertToPdf(htmlContent, msOutput);
        return msOutput.ToArray();
    }
    
    private byte[] ConvertExcelToPdf(byte[] content)
    {
        // Generar PDF simple de Excel volcando texto
        var text = ExtractTextFromExcel(content);
        return ConvertTxtToPdf(Encoding.UTF8.GetBytes(text));
    }

    private byte[] ConvertTxtToPdf(byte[] content)
    {
        var text = Encoding.UTF8.GetString(content);
        using var msOutput = new MemoryStream();
        var writer = new PdfWriter(msOutput);
        writer.SetCloseStream(false);
        var pdf = new PdfDocument(writer);
        var document = new Document(pdf);
        var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
        document.SetFont(font);

        var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        foreach (var line in lines)
        {
            var safeLine = line.Replace("\0", ""); 
            document.Add(new Paragraph(safeLine).SetFontSize(10));
        }

        document.Close();
        msOutput.Position = 0;
        return msOutput.ToArray();
    }
}

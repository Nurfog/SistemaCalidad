using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace SistemaCalidad.Api.Services;

public class TemplateService
{
    private static readonly Regex TagRegex = new Regex(@"\{\{([\w\s.#]+)\}\}", RegexOptions.Compiled);

    public List<string> ExtractTags(Stream docxStream)
    {
        var tags = new HashSet<string>();
        using (var document = WordprocessingDocument.Open(docxStream, false))
        {
            var body = document.MainDocumentPart?.Document.Body;
            if (body != null)
            {
                var text = body.InnerText;
                var matches = TagRegex.Matches(text);
                foreach (Match match in matches)
                {
                    tags.Add(match.Groups[1].Value);
                }
            }
        }
        return tags.ToList();
    }

    public byte[] GenerateDocument(byte[] templateBytes, Dictionary<string, string> values)
    {
        using (var memStream = new MemoryStream())
        {
            memStream.Write(templateBytes, 0, templateBytes.Length);
            using (var document = WordprocessingDocument.Open(memStream, true))
            {
                var mainPart = document.MainDocumentPart;
                if (mainPart != null)
                {
                    // Reemplazar en el cuerpo del documento
                    ReplaceInElement(mainPart.Document.Body, values);

                    // Reemplazar en encabezados y pies de página
                    foreach (var headerPart in mainPart.HeaderParts)
                    {
                        ReplaceInElement(headerPart.Header, values);
                    }
                    foreach (var footerPart in mainPart.FooterParts)
                    {
                        ReplaceInElement(footerPart.Footer, values);
                    }

                    mainPart.Document.Save();
                }
            }
            return memStream.ToArray();
        }
    }

    private void ReplaceInElement(DocumentFormat.OpenXml.OpenXmlElement element, Dictionary<string, string> values)
    {
        if (element == null) return;

        // Una técnica común en OpenXML para evitar que las etiquetas se dividan en múltiples "Runs"
        // es buscar y reemplazar en el texto completo.
        var paragraphs = element.Descendants<Paragraph>();
        foreach (var p in paragraphs)
        {
            var text = p.InnerText;
            if (TagRegex.IsMatch(text))
            {
                string newText = text;
                foreach (var val in values)
                {
                    newText = newText.Replace("{{" + val.Key + "}}", val.Value);
                }

                // Si el texto cambió, limpiamos el párrafo y agregamos un nuevo Run con el texto reemplazado
                // NOTA: Esto pierde formato si el párrafo tenía múltiples estilos.
                // Una implementación más robusta requiere recorrer los Runs cuidadosamente.
                // Para SGC básico, esto suele ser suficiente si las plantillas son simples.
                
                // Implementación mejorada que intenta preservar algo de estructura:
                var texts = p.Descendants<Text>().ToList();
                if (texts.Count > 0)
                {
                    // Consolidamos texto en el primer elemento Text y vaciamos el resto
                    for (int i = 1; i < texts.Count; i++) texts[i].Text = "";
                    texts[0].Text = newText;
                }
            }
        }
    }
}

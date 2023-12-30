using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace BotSharp.Plugin.KnowledgeBase.Services;

public class PigPdf2TextConverter : IPdf2TextConverter
{
    public async Task<string> ConvertPdfToText(string filePath, int? startPageNum, int? endPageNum)
    {
        return await OpenPdfDocumentAsync(filePath, startPageNum, endPageNum);
    }

    private async Task<string> OpenPdfDocumentAsync(string filePath, int? startPageNum, int? endPageNum)
    {
        var document = PdfDocument.Open(filePath);
        var content = "";
        foreach (Page page in document.GetPages())
        {
            if (startPageNum.HasValue && page.Number < startPageNum.Value)
            {
                continue;
            }

            if (endPageNum.HasValue && page.Number > endPageNum.Value)
            {
                continue;
            }
            content += page.Text;
        }
        return content;
    }
}

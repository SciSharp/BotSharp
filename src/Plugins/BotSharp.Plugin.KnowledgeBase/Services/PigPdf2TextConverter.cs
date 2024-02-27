using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace BotSharp.Plugin.KnowledgeBase.Services;

public class PigPdf2TextConverter : IPdf2TextConverter
{
    public Task<string> ConvertPdfToText(string filePath, int? startPageNum, int? endPageNum)
    {
        // since PdfDocument.Open is not async, we dont need to make this method async
        // if you need this method to be async, consider wrapping the call in Task.Run for CPU-bound work
        return Task.FromResult(OpenPdfDocument(filePath, startPageNum, endPageNum));
    }

    private string OpenPdfDocument(string filePath, int? startPageNum, int? endPageNum)
    {
        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var document = PdfDocument.Open(fileStream);
        var content = new StringBuilder();
        foreach (Page page in document.GetPages())
        {
            if (startPageNum.HasValue && page.Number < startPageNum.Value) continue;
            if (endPageNum.HasValue && page.Number > endPageNum.Value) continue;
            content.Append(page.Text);
        }
        return content.ToString();
    }
}

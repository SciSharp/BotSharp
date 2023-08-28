using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Http;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace BotSharp.Core.Plugins.Knowledges.Services;

public class PigPdf2TextConverter : IPdf2TextConverter
{
    public async Task<string> ConvertPdfToText(IFormFile formFile, int? startPageNum, int? endPageNum)
    {
        return await OpenPdfDocumentAsync(formFile, startPageNum, endPageNum);
    }

    private async Task<string> OpenPdfDocumentAsync(IFormFile formFile, int? startPageNum, int? endPageNum)
    {
        if (formFile.Length <= 0)
        {
            return await Task.FromResult(string.Empty);
        }

        var filePath = Path.GetTempFileName();

        using (var stream = System.IO.File.Create(filePath))
        {
            await formFile.CopyToAsync(stream);
        }

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

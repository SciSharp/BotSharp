using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace BotSharp.Abstraction.Knowledges
{
    public interface IPdf2TextConverter
    {
        Task<string> ConvertPdfToText(IFormFile formFile, int? startPageNum, int? endPageNum);
    }
}
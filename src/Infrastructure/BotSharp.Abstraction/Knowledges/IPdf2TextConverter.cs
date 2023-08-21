using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace BotSharp.Abstraction.Knowledges
{
    public interface IPdf2TextConverter
    {
        Task<string> ConvertPdfToText(IFormFile formFile, int? startPageNum, int? endPageNum, bool paddleModel);
        Task<string> OpenPdfDocumentAsync(IFormFile formFile, int? startPageNum, int? endPageNum);
        Task<string> LocalImageToTextsAsync();
        Task ConvertPdfToLocalImagesAsync(IFormFile formFile, int? startPageNum, int? endPageNum);
        void ConvertPdfToLocalImages(IFormFile formFile, int? startPageNum, int? endPageNum);
        void DeleteTempFolder(string filePath = "");
    }
}
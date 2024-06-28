using BotSharp.Abstraction.Files.Converters;
using PdfiumViewer;
using System.IO;

namespace BotSharp.Core.Files.Converters;

public class PdfiumConverter : IPdf2ImageConverter
{
    public async Task<IEnumerable<string>> ConvertPdfToImages(string pdfLocation, string imageFolderLocation)
    {
        var paths = new List<string>();
        if (string.IsNullOrWhiteSpace(imageFolderLocation)) return paths;

        if (Directory.Exists(imageFolderLocation))
        {
            Directory.Delete(imageFolderLocation, true);
        }
        Directory.CreateDirectory(imageFolderLocation);

        var guid = Guid.NewGuid().ToString();
        using (var document = PdfDocument.Load(pdfLocation))
        {
            var pages = document.PageCount;

            for (var page = 0; page < pages; page++)
            {
                var size = document.PageSizes[page];
                using (var image = document.Render(page, (int)size.Width, (int)size.Height, 96, 96, true))
                {
                    var imagePath = Path.Combine(imageFolderLocation, $"{guid}_pg_{page + 1}.png");
                    image.Save(imagePath, System.Drawing.Imaging.ImageFormat.Png);
                    paths.Add(imagePath);
                }
            }
        }

        return await Task.FromResult(paths);
    }
}

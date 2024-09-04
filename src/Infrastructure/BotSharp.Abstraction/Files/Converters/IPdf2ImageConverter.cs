namespace BotSharp.Abstraction.Files.Converters;

public interface IPdf2ImageConverter
{
    public string Provider { get; }

    /// <summary>
    /// Convert pdf pages to images, and return a list of image file paths
    /// </summary>
    /// <param name="pdfLocation">Pdf file location</param>
    /// <param name="imageFolderLocation">Image folder location</param>
    /// <returns></returns>
    Task<IEnumerable<string>> ConvertPdfToImages(string pdfLocation, string imageFolderLocation);
}

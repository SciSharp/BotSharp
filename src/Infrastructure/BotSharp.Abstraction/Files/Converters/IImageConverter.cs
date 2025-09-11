namespace BotSharp.Abstraction.Files.Converters;

public interface IImageConverter
{
    public string Provider { get; }

    /// <summary>
    /// Convert pdf pages to images, and return a list of image file paths
    /// </summary>
    /// <param name="pdfLocation">Pdf file location</param>
    /// <param name="imageFolderLocation">Image folder location</param>
    /// <returns></returns>
    Task<IEnumerable<string>> ConvertPdfToImages(string pdfLocation, string imageFolderLocation) => throw new NotImplementedException();

    /// <summary>
    /// Convert an image to PNG with RGBA
    /// </summary>
    /// <param name="binary"></param>
    /// <returns></returns>
    Task<BinaryData> ConvertImageToRgbaPng(BinaryData binary) => throw new NotImplementedException();
}

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;
using System.IO;

namespace BotSharp.Plugin.FileHandler.Converters;

public class FileHandlerImageConverter : IImageConverter
{
    private readonly IServiceProvider _services;
    private readonly ILogger<FileHandlerImageConverter> _logger;

    public FileHandlerImageConverter(
        IServiceProvider services,
        ILogger<FileHandlerImageConverter> logger)
    {
        _services = services;
        _logger = logger;
    }

    public string Provider => "file-handler";

    public async Task<BinaryData> ConvertImageToRgbaPng(BinaryData binary)
    {
        try
        {
            using var image = Image.Load<Rgba32>(binary.ToArray());
            using var memoryStream = new MemoryStream();

            image.SaveAsPng(memoryStream, new PngEncoder
            {
                ColorType = PngColorType.RgbWithAlpha
            });
            var convertedBinary = BinaryData.FromBytes(memoryStream.ToArray());
            return await Task.FromResult(convertedBinary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when converting image to RGBA png in {Provider}.");
            return binary;
        }
    }
}

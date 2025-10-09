using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;
using System.IO;

namespace BotSharp.Plugin.ImageHandler.Converters;

public class ImageHandlerImageConverter : IImageConverter
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ImageHandlerImageConverter> _logger;

    public ImageHandlerImageConverter(
        IServiceProvider services,
        ILogger<ImageHandlerImageConverter> logger)
    {
        _services = services;
        _logger = logger;
    }

    public string Provider => "image-handler";

    public async Task<BinaryData> ConvertImage(BinaryData binary, ImageConvertOptions? options = null)
    {
        try
        {
            using var image = Image.Load<Rgba32>(binary.ToArray());
            using var memoryStream = new MemoryStream();

            if (options?.ImageType == "png")
            {
                var colorType = PngColorType.RgbWithAlpha;
                switch (options?.ColorType)
                {
                    case "grayscale":
                        colorType = PngColorType.Grayscale;
                        break;
                    case "grayscaleWithAlpha":
                        colorType = PngColorType.GrayscaleWithAlpha;
                        break;
                    case "rgb":
                        colorType = PngColorType.Rgb;
                        break;
                    case "palette":
                        colorType = PngColorType.Palette;
                        break;
                }

                image.SaveAsPng(memoryStream, new PngEncoder
                {
                    ColorType = colorType
                });
            }
            else
            {
                image.SaveAsPng(memoryStream, new PngEncoder
                {
                    ColorType = PngColorType.RgbWithAlpha
                });
            }
            
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

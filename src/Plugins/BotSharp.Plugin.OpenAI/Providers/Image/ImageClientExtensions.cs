#pragma warning disable OPENAI001
using OpenAI.Images;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Reflection;

namespace BotSharp.Plugin.OpenAI.Providers.Image;

/// <summary>
/// Extension methods for ImageClient to support multiple image composition
/// </summary>
public static class ImageClientExtensions
{
    /// <summary>
    /// Generates image edits with multiple input images for composition
    /// </summary>
    /// <param name="client">The ImageClient instance</param>
    /// <param name="images">Array of image streams to compose</param>
    /// <param name="imageFileNames">Array of corresponding file names for the images</param>
    /// <param name="prompt">The prompt describing the desired composition</param>
    /// <param name="imageCount">Number of images to generate (default: 1)</param>
    /// <param name="options">Optional image edit options</param>
    /// <returns>ClientResult containing the generated image collection</returns>
    public static ClientResult<GeneratedImageCollection> GenerateImageEdits(
        this ImageClient client,
        Stream[] images,
        string[] imageFileNames,
        string prompt,
        int? imageCount = null,
        ImageEditOptions options = null)
    {
        if (client == null)
            throw new ArgumentNullException(nameof(client));

        if (images == null || images.Length == 0)
            throw new ArgumentException("At least one image is required", nameof(images));

        if (imageFileNames == null || imageFileNames.Length != images.Length)
            throw new ArgumentException("Image file names array must match images array length", nameof(imageFileNames));

        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt cannot be null or empty", nameof(prompt));

        // Get the pipeline from the client
        var pipeline = client.Pipeline;
        using var message = pipeline.CreateMessage();

        // Build the request
        BuildMultipartRequest(message, images, imageFileNames, prompt, imageCount, options);

        // Send the request
        pipeline.Send(message);

        if (message.Response.IsError)
        {
            throw new InvalidOperationException($"API request failed with status {message.Response.Status}: {message.Response.ReasonPhrase} \r\n{message.Response.Content}");
        }

        // Parse the response
        var generatedImages = ParseResponse(message.Response, options?.ResponseFormat);

        return ClientResult.FromValue(generatedImages, message.Response);
    }

    private static void BuildMultipartRequest(
        PipelineMessage message,
        Stream[] images,
        string[] imageFileNames,
        string prompt,
        int? imageCount,
        ImageEditOptions options)
    {
        message.Request.Method = "POST";

        // Use the endpoint from the client or default to OpenAI
        var endpoint = "https://api.openai.com";
        message.Request.Uri = new Uri($"{endpoint.TrimEnd('/')}/v1/images/edits");

        // Create multipart form data
        var boundary = $"----WebKitFormBoundary{Guid.NewGuid():N}";
        var contentBuilder = new MemoryStream();

        // Add prompt
        WriteFormField(contentBuilder, boundary, "prompt", prompt);

        WriteFormField(contentBuilder, boundary, "model", "gpt-image-1-mini");

        // Add image count
        WriteFormField(contentBuilder, boundary, "n", imageCount.Value.ToString() ?? "1");

        for (var i = 0; i < images.Length; i++)
        {
            WriteFormField(contentBuilder, boundary, "image[]", imageFileNames[i], images[i], "image/png");
        }

        // Add optional parameters supported by OpenAI image edits API
        if (options.Quality.HasValue)
        {
            WriteFormField(contentBuilder, boundary, "quality", options.Quality.ToString() ?? "auto");
        }

        if (options.Size.HasValue)
        {
            WriteFormField(contentBuilder, boundary, "size", ConvertImageSizeToString(options.Size.Value));
        }

        if (options.Background.HasValue)
        {
            WriteFormField(contentBuilder, boundary, "background", options.Background.ToString() ?? "auto");
        }

        WriteFormField(contentBuilder, boundary, "output_format", "png");

        if (!string.IsNullOrEmpty(options.EndUserId))
        {
            WriteFormField(contentBuilder, boundary, "user", options.EndUserId);
        }

        WriteFormField(contentBuilder, boundary, "moderation", "auto");

        // Write closing boundary
        var closingBoundary = Encoding.UTF8.GetBytes($"--{boundary}--\r\n");
        contentBuilder.Write(closingBoundary, 0, closingBoundary.Length);

        // Set the content
        contentBuilder.Position = 0;
        message.Request.Content = BinaryContent.Create(BinaryData.FromStream(contentBuilder));

        // Set content type header
        message.Request.Headers.Set("Content-Type", $"multipart/form-data; boundary={boundary}");
    }

    private static void WriteFormField(MemoryStream stream, string boundary, string name, string value)
    {
        var header = $"--{boundary}\r\nContent-Disposition: form-data; name=\"{name}\"\r\n";
        var body = $"{header}\r\n{value}\r\n";
        var bytes = Encoding.UTF8.GetBytes(body);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static void WriteFormField(MemoryStream stream, string boundary, string name, string fileName, Stream fileStream, string contentType)
    {
        var header = $"--{boundary}\r\nContent-Disposition: form-data; name=\"{name}\"; filename=\"{fileName}\"\r\nContent-Type: {contentType}\r\n\r\n";
        var headerBytes = Encoding.UTF8.GetBytes(header);
        stream.Write(headerBytes, 0, headerBytes.Length);

        // Copy file stream
        if (fileStream.CanSeek)
        {
            fileStream.Position = 0;
        }
        fileStream.CopyTo(stream);

        var newLine = Encoding.UTF8.GetBytes("\r\n");
        stream.Write(newLine, 0, newLine.Length);
    }

    #region Helper Methods

    private static string GetEndpoint(PipelineMessage message)
    {
        // Try to get the endpoint from the request URI if already set
        return message.Request.Uri?.GetLeftPart(UriPartial.Authority);
    }

    private static GeneratedImageCollection ParseResponse(PipelineResponse response, GeneratedImageFormat? format)
    {
        try
        {
            // Try to use ModelReaderWriter to deserialize the response
            var modelReaderWriter = ModelReaderWriter.Read<GeneratedImageCollection>(response.Content);
            if (modelReaderWriter != null)
            {
                return modelReaderWriter;
            }
        }
        catch (Exception ex)
        {
            // Log the error but continue to fallback methods
            Console.WriteLine($"ModelReaderWriter failed: {ex.Message}");
        }

        // Fallback: Try to find and invoke internal deserialization methods
        try
        {
            // Look for FromResponse or similar static methods on GeneratedImageCollection
            var fromResponseMethod = typeof(GeneratedImageCollection).GetMethod(
                "FromResponse",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                null,
                [typeof(PipelineResponse)],
                null);

            if (fromResponseMethod != null)
            {
                var result = fromResponseMethod.Invoke(null, new object[] { response });
                if (result != null)
                {
                    return (GeneratedImageCollection)result;
                }
            }

            // Try DeserializeGeneratedImageCollection method
            var deserializeMethod = typeof(GeneratedImageCollection).GetMethod(
                "DeserializeGeneratedImageCollection",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            if (deserializeMethod != null)
            {
                var jsonDocument = JsonDocument.Parse(response.Content);
                var result = deserializeMethod.Invoke(null, new object[] { jsonDocument.RootElement });
                if (result != null)
                {
                    return (GeneratedImageCollection)result;
                }
            }
        }
        catch (Exception ex)
        {
            var innerMessage = ex.InnerException?.Message ?? ex.Message;
            throw new InvalidOperationException($"Failed to deserialize GeneratedImageCollection using reflection: {innerMessage}. Response content: {response.Content.ToString().Substring(0, Math.Min(200, response.Content.ToString().Length))}", ex);
        }

        throw new InvalidOperationException($"Unable to parse response into GeneratedImageCollection. No suitable deserialization method found. Available methods on GeneratedImageCollection: {string.Join(", ", typeof(GeneratedImageCollection).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).Select(m => m.Name))}");
    }

    private static string ConvertImageSizeToString(GeneratedImageSize size)
    {
        // Map GeneratedImageSize enum to string values
        if (size == GeneratedImageSize.W256xH256) return "256x256";
        if (size == GeneratedImageSize.W512xH512) return "512x512";
        if (size == GeneratedImageSize.W1024xH1024) return "1024x1024";
        if (size == GeneratedImageSize.W1024xH1792) return "1024x1792";
        if (size == GeneratedImageSize.W1792xH1024) return "1792x1024";
        if (size == GeneratedImageSize.W1024xH1536) return "1024x1536";
        if (size == GeneratedImageSize.W1536xH1024) return "1536x1024";
        
        return "1024x1024"; // default
    }

    #endregion
}


#pragma warning disable OPENAI001
using OpenAI.Images;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Net.Http;
using System.Net.Http.Headers;
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
    /// <param name="model">The LLM model</param>
    /// <param name="images">Array of image streams to compose</param>
    /// <param name="imageFileNames">Array of corresponding file names for the images</param>
    /// <param name="prompt">The prompt describing the desired composition</param>
    /// <param name="imageCount">Number of images to generate (default: 1)</param>
    /// <param name="options">Optional image edit options</param>
    /// <returns>ClientResult containing the generated image collection</returns>
    public static ClientResult<GeneratedImageCollection> GenerateImageEdits(
        this ImageClient client,
        string model,
        Stream[] images,
        string[] imageFileNames,
        string prompt,
        int? imageCount = null,
        ImageEditOptions? options = null)
    {
        if (client == null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        if (images.IsNullOrEmpty())
        {
            throw new ArgumentException("At least one image is required", nameof(images));
        }

        if (imageFileNames == null || imageFileNames.Length != images.Length)
        {
            throw new ArgumentException("Image file names array must match images array length", nameof(imageFileNames));
        }

        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException("Prompt cannot be null or empty", nameof(prompt));
        }

        // Get the pipeline from the client
        var pipeline = client.Pipeline;
        using var message = pipeline.CreateMessage();

        // Build the request
        BuildMultipartRequest(message, model, images, imageFileNames, prompt, imageCount, options);

        // Send the request
        pipeline.Send(message);

        if (message.Response == null || message.Response.IsError)
        {
            throw new InvalidOperationException($"API request failed with status {message.Response?.Status}: {message.Response?.ReasonPhrase} \r\n{message.Response?.Content}");
        }

        // Parse the response
        var generatedImages = ParseResponse(message.Response);

        return ClientResult.FromValue(generatedImages, message.Response);
    }

    private static void BuildMultipartRequest(
        PipelineMessage message,
        string model,
        Stream[] images,
        string[] imageFileNames,
        string prompt,
        int? imageCount,
        ImageEditOptions? options)
    {
        message.Request.Method = "POST";

        // Use the endpoint from the client or default to OpenAI
        var endpoint = "https://api.openai.com";
        message.Request.Uri = new Uri($"{endpoint.TrimEnd('/')}/v1/images/edits");

        var boundary = $"----WebKitFormBoundary{Guid.NewGuid():N}";
        using var form = new MultipartFormDataContent(boundary)
        {
            { new StringContent(prompt), "prompt" },
            { new StringContent(model ?? "gpt-image-1-mini"), "model" }
        };

        if (imageCount.HasValue)
        {
            form.Add(new StringContent(imageCount.Value.ToString()), "n");
        }
        else
        {
            form.Add(new StringContent("1"), "n");
        }

        if (options != null)
        {
            if (options.Quality.HasValue)
            {
                form.Add(new StringContent(options.Quality.ToString()), "quality");
            }

            if (options.Size.HasValue)
            {
                form.Add(new StringContent(ConvertImageSizeToString(options.Size.Value)), "size");
            }

            if (options.Background.HasValue)
            {
                form.Add(new StringContent(options.Background.ToString()), "background");
            }

            if (options.ResponseFormat.HasValue)
            {
                form.Add(new StringContent(options.ResponseFormat.ToString()), "response_format");
            }
        }

        for (var i = 0; i < images.Length; i++)
        {
            if (images[i].CanSeek)
            {
                images[i].Position = 0;
            }
            var fileContent = new StreamContent(images[i]);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");

            if (images.Length > 1)
            {
                form.Add(fileContent, name: "image[]", fileName: imageFileNames[i]);
            }
            else
            {
                form.Add(fileContent, name: "image", fileName: imageFileNames[i]);
            }
        }

        using var ms = new MemoryStream();
        form.CopyTo(ms, null, CancellationToken.None);
        ms.Position = 0;

        message.Request.Headers.Set("Content-Type", form.Headers.ContentType.ToString());
        message.Request.Content = BinaryContent.Create(BinaryData.FromStream(ms));
    }

    #region Private Methods
    private static GeneratedImageCollection ParseResponse(PipelineResponse response)
    {
        try
        {
            // Try to use ModelReaderWriter to deserialize the response
            var result = ModelReaderWriter.Read<GeneratedImageCollection>(response.Content);
            if (result != null)
            {
                return result;
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
                    return result as GeneratedImageCollection;
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
                    return result as GeneratedImageCollection;
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


namespace BotSharp.Plugin.MetaGLM.Modules;

//public enum ModelPortal
//{
//    Regular,
//    Character,
//}

public class Chat
{
    private string _apiKey;
    private string _baseAddress;

    private static readonly int API_TOKEN_TTL_SECONDS = 60 * 5;

    private static readonly HttpClient client = new();

    //private static readonly Dictionary<ModelPortal, string> PORTAL_URLS = new()
    //{
    //    { ModelPortal.Regular , "https://open.bigmodel.cn/api/paas/v4/chat/completions"},
    //};

    private static readonly JsonSerializerOptions JsonOptions = new ()
    {
        Converters =
        {
            new MessageItemConverter()
        },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    public Chat(string apiKey, string basicAddress = "https://open.bigmodel.cn/api/paas/v4/")
    {
        this._apiKey = apiKey;
        this._baseAddress = basicAddress.TrimEnd('/');
    }

    private async IAsyncEnumerable<string> CompletionBase(TextRequestBase textRequestBody,string apiKey)
    {
        var json = JsonSerializer.Serialize(textRequestBody, JsonOptions);
        var data = new StringContent(json, Encoding.UTF8, "application/json");
        var api_key = AuthenticationUtils.GenerateToken(apiKey, API_TOKEN_TTL_SECONDS);

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{_baseAddress}/chat/completions"),
            Content = data,
            Headers =
            {
                { "Authorization", api_key }
            },

        };

        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        var stream = await response.Content.ReadAsStreamAsync();
        byte[] buffer = new byte[8192];
        int bytesRead;

        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            yield return Encoding.UTF8.GetString(buffer, 0, bytesRead);
        }
    }

    public async Task<ResponseBase> Completion(TextRequestBase textRequestBody)
    {
        textRequestBody.stream = false;
        var sb = new StringBuilder();
        await foreach (var str in CompletionBase(textRequestBody, _apiKey))
        {
            sb.Append(str);
        }

        return ResponseBase.FromJson(sb.ToString());
    }

    public async IAsyncEnumerable<ResponseBase> Stream(TextRequestBase textRequestBody )
    {
        textRequestBody.stream = true;
        var buffer = string.Empty;
        await foreach (var chunk in CompletionBase(textRequestBody, _apiKey))
        {

            buffer += chunk;

            while (true)
            {
                int startPos = buffer.IndexOf("data: ", StringComparison.Ordinal);
                if (startPos == -1)
                {
                    break;
                }

                int endPos = buffer.IndexOf("\n\n", startPos, StringComparison.Ordinal);

                if (endPos == -1)
                {
                    break;
                }

                startPos += "data: ".Length;

                string jsonString = buffer.Substring(startPos, endPos - startPos);
                if (jsonString.Equals("[DONE]"))
                {
                    break;
                }

                var response = ResponseBase.FromJson(jsonString);
                if (response != null)
                {
                    yield return response;
                }

                buffer = buffer.Substring(endPos + "\n\n".Length);
            }

        }

        var finalResponse = ResponseBase.FromJson(buffer.Trim());
        if (finalResponse != null)
        {
            yield return finalResponse;
        }
    }
}
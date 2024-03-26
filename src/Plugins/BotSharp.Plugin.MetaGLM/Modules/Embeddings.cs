namespace BotSharp.Plugin.MetaGLM.Modules;

public class Embeddings
{
    private string _apiKey;
    private static readonly int API_TOKEN_TTL_SECONDS = 60 * 5;
    static readonly HttpClient client = new();

    public Embeddings(string apiKey)
    {
        this._apiKey = apiKey;
    }

    private IEnumerable<string> ProcessBase(EmbeddingRequestBase requestBody, string apiKey)
    {
        var json = JsonSerializer.Serialize(requestBody);
        // Console.WriteLine(JsonSerializer.Serialize(requestBody));
        // Console.WriteLine("----1----");
        // Console.WriteLine(json);
        var data = new StringContent(json, Encoding.UTF8, "application/json");
        var api_key = AuthenticationUtils.GenerateToken(apiKey, API_TOKEN_TTL_SECONDS);

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri("https://open.bigmodel.cn/api/paas/v4/embeddings"),
            Content = data,
            Headers =
            {
                { "Authorization", api_key }
            },

        };

        var response = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).Result;
        var stream = response.Content.ReadAsStreamAsync().Result;
        byte[] buffer = new byte[8192];
        int bytesRead;

        while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            yield return Encoding.UTF8.GetString(buffer, 0, bytesRead);
        }
    }

    public EmbeddingResponseBase Process(EmbeddingRequestBase requestBody, string apiKey)
    {
        var sb = new StringBuilder();
        foreach (var str in ProcessBase(requestBody,apiKey))
        {
            sb.Append(str);
        }

        return EmbeddingResponseBase.FromJson(sb.ToString());
    }

}
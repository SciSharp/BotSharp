namespace BotSharp.Plugin.MetaGLM.Modules;

public class Images
{
    private string _apiKey;
    private string _baseAddress;
    private static readonly int API_TOKEN_TTL_SECONDS = 60 * 5;
    static readonly HttpClient client = new HttpClient();

    public Images(string apiKey, string basicAddress = "https://open.bigmodel.cn/api/paas/v4/")
    {
        this._apiKey = apiKey;
        this._baseAddress = basicAddress;
    }

    private IEnumerable<string> GenerateBase(ImageRequestBase requestBody)
    {
        var json = JsonSerializer.Serialize(requestBody);
        var data = new StringContent(json, Encoding.UTF8, "application/json");
        var api_key = AuthenticationUtils.GenerateToken(_apiKey, API_TOKEN_TTL_SECONDS);

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{_baseAddress}images/generations"),
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

    public ImageResponseBase Generation(ImageRequestBase requestBody, string apiKey)
    {
        StringBuilder sb = new StringBuilder();
        foreach (var str in GenerateBase(requestBody))
        {
            sb.Append(str);
        }
        return ImageResponseBase.FromJson(sb.ToString());
    }

}
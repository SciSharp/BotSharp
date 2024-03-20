using BotSharp.Abstraction.Google.Models;
using BotSharp.Abstraction.Google.Settings;
using BotSharp.Abstraction.Options;

namespace BotSharp.OpenAPI.Controllers;

[Authorize]
[ApiController]
public class AddressController : ControllerBase
{
    private readonly IServiceProvider _services;
    private readonly BotSharpOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;

    public AddressController(IServiceProvider services,
        IHttpClientFactory httpClientFactory,
        BotSharpOptions options)
    {
        _services = services;
        _options = options;
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("/address/options")]
    public async Task<GoogleAddressResult> GetAddressOptions([FromQuery] string address)
    {
        var result = new GoogleAddressResult();

        try
        {
            var settings = _services.GetRequiredService<GoogleApiSettings>();
            using var client = _httpClientFactory.CreateClient();
            var url = $"{settings.Endpoint}?key={settings.ApiKey}&" +
                $"components={settings.Components}&" +
                $"language={settings.Language}&" +
                $"address={address}";

            var response = await client.GetAsync(url);
            var responseStr = await response.Content.ReadAsStringAsync();
            result = JsonSerializer.Deserialize<GoogleAddressResult>(responseStr, _options.JsonSerializerOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error when calling google geocoding api... ${ex.Message}");
        }

        return result;
    }
}

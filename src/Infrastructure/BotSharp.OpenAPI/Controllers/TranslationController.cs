using BotSharp.Abstraction.Options;
using BotSharp.Abstraction.Translation;
using BotSharp.OpenAPI.ViewModels.Translations;

namespace BotSharp.OpenAPI.Controllers;

[Authorize]
[ApiController]
public class TranslationController : ControllerBase
{
    private readonly IServiceProvider _services;
    private readonly JsonSerializerOptions _jsonOptions;

    public TranslationController(IServiceProvider services,
        BotSharpOptions options)
    {
        _services = services;
        _jsonOptions = InitJsonOptions(options);
    }

    [HttpPost("/translate")]
    public async Task<TranslationResponseModel> Translate([FromBody] TranslationRequestModel model)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.LoadAgent(BuiltInAgentId.AIAssistant);
        var translator = _services.GetRequiredService<ITranslationService>();
        var text = await translator.Translate(agent, Guid.NewGuid().ToString(), model.Text, language: model.ToLang);
        return new TranslationResponseModel
        {
            Text = text
        };
    }

    [HttpPost("/translate/long-text")]
    public async Task SendMessageSse([FromBody] TranslationLongTextRequestModel model)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.LoadAgent(BuiltInAgentId.AIAssistant);
        var translator = _services.GetRequiredService<ITranslationService>();

        Response.StatusCode = 200;
        Response.Headers.Append(Microsoft.Net.Http.Headers.HeaderNames.ContentType, "text/event-stream");
        Response.Headers.Append(Microsoft.Net.Http.Headers.HeaderNames.CacheControl, "no-cache");
        Response.Headers.Append(Microsoft.Net.Http.Headers.HeaderNames.Connection, "keep-alive");

        foreach (var script in model.Texts)
        {
            var translatedText = await translator.Translate(agent, Guid.NewGuid().ToString(), script.Text, language: model.ToLang);

            var json = JsonSerializer.Serialize(new TranslationScriptTimestamp
            {
                Text = translatedText,
                Timestamp = script.Timestamp
            }, _jsonOptions);

            await OnChunkReceived(Response, json);
        }

        await OnEventCompleted(Response);
    }

    private async Task OnChunkReceived(HttpResponse response, string text)
    {
        var buffer = Encoding.UTF8.GetBytes($"data:{text}\n");
        await response.Body.WriteAsync(buffer, 0, buffer.Length);
        await Task.Delay(10);

        buffer = Encoding.UTF8.GetBytes("\n");
        await response.Body.WriteAsync(buffer, 0, buffer.Length);
    }

    private async Task OnEventCompleted(HttpResponse response)
    {
        var buffer = Encoding.UTF8.GetBytes("data:[DONE]\n");
        await response.Body.WriteAsync(buffer, 0, buffer.Length);

        buffer = Encoding.UTF8.GetBytes("\n");
        await response.Body.WriteAsync(buffer, 0, buffer.Length);
    }

    private JsonSerializerOptions InitJsonOptions(BotSharpOptions options)
    {
        var jsonOption = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            AllowTrailingCommas = true
        };

        if (options?.JsonSerializerOptions != null)
        {
            foreach (var option in options.JsonSerializerOptions.Converters)
            {
                jsonOption.Converters.Add(option);
            }
        }

        return jsonOption;
    }
}

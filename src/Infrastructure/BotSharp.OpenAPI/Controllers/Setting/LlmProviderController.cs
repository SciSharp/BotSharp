using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.MLTasks.Filters;
using BotSharp.Abstraction.MLTasks.Settings;

namespace BotSharp.OpenAPI.Controllers;

[Authorize]
[ApiController]
public class LlmProviderController : ControllerBase
{
    private readonly IServiceProvider _services;
    private readonly ILlmProviderService _llmProvider;

    public LlmProviderController(IServiceProvider services, ILlmProviderService llmProvider)
    {
        _services = services;
        _llmProvider = llmProvider;
    }

    [HttpGet("/llm-providers")]
    public IEnumerable<string> GetLlmProviders()
    {
        return _llmProvider.GetProviders();
    }

    // The two lists below are for choosing a model; any signed-in user can read them, so the api key and endpoint stay on the server.

    [HttpGet("/llm-provider/{provider}/models")]
    public IEnumerable<LlmModelSetting> GetLlmProviderModels([FromRoute] string provider, [FromQuery] LlmModelType modelType = LlmModelType.Chat)
    {
        var list = _llmProvider.GetProviderModels(provider);
        return list.Where(x => x.Type == modelType).Select(x => x.WithoutCredentials()).ToList();
    }

    [HttpGet("/llm-configs")]
    public List<LlmProviderSetting> GetLlmConfigs([FromQuery] LlmConfigFilter filter)
    {
        var configs = _llmProvider.GetLlmConfigs(filter);
        return configs.Select(x => x.WithoutCredentials()).ToList();
    }
}

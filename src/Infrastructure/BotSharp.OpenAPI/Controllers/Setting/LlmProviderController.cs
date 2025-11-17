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

    [HttpGet("/llm-provider/{provider}/models")]
    public IEnumerable<LlmModelSetting> GetLlmProviderModels([FromRoute] string provider, [FromQuery] LlmModelType modelType = LlmModelType.Chat)
    {
        var list = _llmProvider.GetProviderModels(provider);
        return list.Where(x => x.Type == modelType);
    }

    [HttpGet("/llm-configs")]
    public List<LlmProviderSetting> GetLlmConfigs([FromQuery] LlmConfigFilter filter)
    {
        var configs = _llmProvider.GetLlmConfigs(filter);
        return configs;
    }
}

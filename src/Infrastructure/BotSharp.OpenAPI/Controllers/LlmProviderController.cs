using BotSharp.Abstraction.MLTasks;
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
    public IEnumerable<LlmModelSetting> GetLlmProviderModels([FromRoute] string provider)
    {
        return _llmProvider.GetProviderModels(provider);
    }
}

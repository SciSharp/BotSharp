using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Translation;
using BotSharp.OpenAPI.ViewModels.Translations;

namespace BotSharp.OpenAPI.Controllers;

[Authorize]
[ApiController]
public class TranslationController : ControllerBase
{
    private readonly IServiceProvider _services;

    public TranslationController(IServiceProvider services)
    {
        _services = services;
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
}

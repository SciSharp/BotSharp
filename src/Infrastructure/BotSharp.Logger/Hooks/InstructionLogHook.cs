using BotSharp.Abstraction.Instructs.Models;
using BotSharp.Abstraction.Instructs.Settings;
using BotSharp.Abstraction.Loggers.Models;
using BotSharp.Abstraction.Users;

namespace BotSharp.Logger.Hooks;

public class InstructionLogHook : InstructHookBase
{
    private readonly IServiceProvider _services;
    private readonly ILogger<InstructionLogHook> _logger;
    private readonly IUserIdentity _user;

    public override string SelfId => string.Empty;

    public InstructionLogHook(
        IServiceProvider services,
        ILogger<InstructionLogHook> logger,
        IUserIdentity user)
    {
        _services = services;
        _logger = logger;
        _user = user;
    }

    public override async Task OnResponseGenerated(InstructResponseModel response)
    {
        var settings = _services.GetRequiredService<InstructionSettings>();
        if (response == null
            || string.IsNullOrWhiteSpace(response.AgentId)
            || settings == null
            || !settings.Logging.Enabled
            || settings.Logging.ExcludedAgentIds.Contains(response.AgentId))
        {
            return;
        }

        var db = _services.GetRequiredService<IBotSharpRepository>();
        var state = _services.GetRequiredService<IConversationStateService>();

        var user = db.GetUserById(_user.Id);
        var templateName = response.TemplateName ?? state.GetState("instruct_template_name") ?? null;

        db.SaveInstructionLogs(new List<InstructionLogModel>
        {
            new InstructionLogModel
            {
                AgentId = response.AgentId,
                Provider = response.Provider,
                Model = response.Model,
                TemplateName = !string.IsNullOrWhiteSpace(templateName) ? templateName : null,
                UserMessage = response.UserMessage,
                SystemInstruction = response.SystemInstruction,
                CompletionText = response.CompletionText,
                States = state.GetStates(),
                UserId = user?.Id
            }
        });

        await base.OnResponseGenerated(response);
    }
}

using BotSharp.Abstraction.Instructs.Models;
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
        if (response == null) return;

        var db = _services.GetRequiredService<IBotSharpRepository>();
        var state = _services.GetRequiredService<IConversationStateService>();

        var user = db.GetUserById(_user.Id);
        db.SaveInstructionLogs(new List<InstructionLogModel>
        {
            new InstructionLogModel
            {
                AgentId = response.AgentId,
                Provider = response.Provider,
                Model = response.Model,
                TemplateName = response.TemplateName,
                UserMessage = response.UserMessage,
                SystemInstruction = response.SystemInstruction,
                CompletionText = response.CompletionText,
                States = state.GetStates(),
                UserId = user?.Id
            }
        });
        return;
    }
}

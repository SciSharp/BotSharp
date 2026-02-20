using BotSharp.Abstraction.Coding.Contexts;
using BotSharp.Abstraction.Coding.Models;
using BotSharp.Abstraction.Instructs.Models;
using BotSharp.Abstraction.Instructs.Settings;
using BotSharp.Abstraction.Loggers.Models;
using BotSharp.Abstraction.Users;
using BotSharp.Abstraction.Utilities;
using System;

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
        if (response == null || !IsLoggingEnabled(response.AgentId))
        {
            return;
        }

        var db = _services.GetRequiredService<IBotSharpRepository>();
        var state = _services.GetRequiredService<IConversationStateService>();

        var user = await db.GetUserById(_user.Id);
        var templateName = response.TemplateName
                                   .IfNullOrEmptyAs(state.GetState("instruct_template_name"))
                                   .IfNullOrEmptyAs(null);

        await db.SaveInstructionLogs(new List<InstructionLogModel>
        {
            new InstructionLogModel
            {
                Id = response.LogId,
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

    public override async Task AfterCodeExecution(Agent agent, CodeExecutionContext context, CodeExecutionResponseModel response)
    {
        if (response == null || !IsLoggingEnabled(agent?.Id))
        {
            return;
        }

        var db = _services.GetRequiredService<IBotSharpRepository>();
        var codeScriptVersion = response.CodeScript?.UpdatedTime ?? DateTime.UtcNow;
        var user = await db.GetUserById(_user.Id);

        await db.SaveInstructionLogs(new List<InstructionLogModel>
        {
            new InstructionLogModel
            {
                AgentId = agent?.Id,
                Provider = response.CodeProcessor,
                Model = string.Empty,
                TemplateName = response.CodeScript?.Name,
                UserMessage = response.Text,
                SystemInstruction = $"Code script name: {response.CodeScript}, Version: {codeScriptVersion.ToString("o")}",
                CompletionText = response.ExecutionResult?.ToString() ?? string.Empty,
                States = response.Arguments?.ToDictionary() ?? [],
                UserId = user?.Id
            }
        });

        await base.AfterCodeExecution(agent, context, response);
    }

    private bool IsLoggingEnabled(string? agentId)
    {
        var settings = _services.GetRequiredService<InstructionSettings>();
        return !string.IsNullOrWhiteSpace(agentId)
            && settings != null
            && settings.Logging.Enabled
            && !settings.Logging.ExcludedAgentIds.Contains(agentId);
    }
}

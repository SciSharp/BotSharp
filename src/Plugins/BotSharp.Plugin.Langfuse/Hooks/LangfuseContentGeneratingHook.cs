using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Loggers;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Users;
using BotSharp.Plugin.Langfuse.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using zborek.Langfuse;

namespace BotSharp.Plugin.Langfuse.Hooks;

public class LangfuseContentGeneratingHook : IContentGeneratingHook
{
    private readonly IServiceProvider _services;
    private readonly ILogger<LangfuseContentGeneratingHook> _logger;
    private readonly LangfuseSettings _settings;

    public LangfuseContentGeneratingHook(
        IServiceProvider services,
        ILogger<LangfuseContentGeneratingHook> logger)
    {
        _services = services;
        _logger = logger;
        _settings = services.GetService<LangfuseSettings>() ?? new LangfuseSettings();

        if (_settings.Enabled)
        {
            _logger.LogInformation("Langfuse content generating hook initialized");
        }
        else
        {
            _logger.LogDebug("Langfuse is disabled");
        }
    }

    public Task BeforeGenerating(Agent agent, List<RoleDialogModel> conversations)
    {
        if (!IsEnabled() || !_settings.LogConversations) 
        {
            return Task.CompletedTask;
        }

        try
        {
            // TODO: Implement when Langfuse API issues are resolved
            // Currently there are compilation issues with LangfuseTrace type resolution
            _logger.LogDebug("Langfuse trace logging would start here for agent {AgentName}", agent.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Langfuse BeforeGenerating hook");
        }
        
        return Task.CompletedTask;
    }

    public Task AfterGenerated(RoleDialogModel message, TokenStatsModel tokenStats)
    {
        if (!IsEnabled() || !_settings.LogConversations) 
        {
            return Task.CompletedTask;
        }

        try
        {
            // TODO: Implement when Langfuse API issues are resolved
            _logger.LogDebug("Langfuse trace logging would record completion here for message {MessageId}", message.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Langfuse AfterGenerated hook");
        }
        
        return Task.CompletedTask;
    }

    public Task BeforeFunctionInvoked(RoleDialogModel message, TokenStatsModel tokenStats)
    {
        if (!IsEnabled() || !_settings.LogFunctions) 
        {
            return Task.CompletedTask;
        }

        try
        {
            // TODO: Implement when Langfuse API issues are resolved
            _logger.LogDebug("Langfuse trace logging would record function start here for {FunctionName}", message.FunctionName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Langfuse BeforeFunctionInvoked hook");
        }
        
        return Task.CompletedTask;
    }

    public Task AfterFunctionInvoked(RoleDialogModel message, TokenStatsModel tokenStats)
    {
        if (!IsEnabled() || !_settings.LogFunctions) 
        {
            return Task.CompletedTask;
        }

        try
        {
            // TODO: Implement when Langfuse API issues are resolved
            _logger.LogDebug("Langfuse trace logging would record function completion here for {FunctionName}", message.FunctionName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Langfuse AfterFunctionInvoked hook");
        }
        
        return Task.CompletedTask;
    }

    private bool IsEnabled()
    {
        return _settings.Enabled;
    }

    private string GetConversationId()
    {
        try
        {
            var state = _services.GetService<IConversationStateService>();
            return state?.GetConversationId() ?? Guid.NewGuid().ToString();
        }
        catch
        {
            return Guid.NewGuid().ToString();
        }
    }

    private string GetSessionId()
    {
        try
        {
            var identity = _services.GetService<IUserIdentity>();
            return identity?.Id ?? "anonymous";
        }
        catch
        {
            return "anonymous";
        }
    }

    private string GetUserId()
    {
        try
        {
            var identity = _services.GetService<IUserIdentity>();
            return identity?.Id ?? "anonymous";
        }
        catch
        {
            return "anonymous";
        }
    }

    private string GetModelName()
    {
        try
        {
            var routing = _services.GetService<IRoutingService>();
            return routing?.Router?.LlmConfig?.Model ?? "unknown";
        }
        catch
        {
            return "unknown";
        }
    }

    private string GetConversationInput(List<RoleDialogModel> conversations)
    {
        try
        {
            var recent = conversations.TakeLast(5).Select(c => $"{c.Role}: {c.Content}").ToArray();
            return string.Join("\n", recent);
        }
        catch
        {
            return "conversation_input_error";
        }
    }
}
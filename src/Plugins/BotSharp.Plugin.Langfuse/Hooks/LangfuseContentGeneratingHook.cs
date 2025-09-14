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

    public async Task BeforeGenerating(Agent agent, List<RoleDialogModel> conversations)
    {
        if (!IsEnabled() || !_settings.LogConversations) return;

        try
        {
            var trace = _services.GetService<LangfuseTrace>();
            if (trace != null)
            {
                var conversationId = GetConversationId();
                var sessionId = GetSessionId();
                
                trace.SetTraceName($"BotSharp Conversation - {agent.Name}");
                trace.SetInput(GetConversationInput(conversations));
                trace.SetUserId(GetUserId());
                trace.SetSessionId(sessionId);
                
                // Add metadata
                trace.AddMetadata("agent_id", agent.Id);
                trace.AddMetadata("agent_name", agent.Name);
                trace.AddMetadata("conversation_id", conversationId);
                trace.AddMetadata("message_count", conversations.Count.ToString());
                trace.AddMetadata("model", agent.LlmConfig?.Model ?? "unknown");
                trace.AddMetadata("provider", agent.LlmConfig?.Provider ?? "unknown");

                _logger.LogDebug("Started Langfuse trace for conversation {ConversationId}", conversationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Langfuse BeforeGenerating hook");
        }
    }

    public async Task AfterGenerated(RoleDialogModel message, TokenStatsModel tokenStats)
    {
        if (!IsEnabled() || !_settings.LogConversations) return;

        try
        {
            var trace = _services.GetService<LangfuseTrace>();
            if (trace != null)
            {
                var conversationId = GetConversationId();

                // Set the output and token usage
                trace.SetOutput(message.Content);
                
                if (_settings.LogTokenStats)
                {
                    trace.AddMetadata("prompt_tokens", tokenStats.PromptCount.ToString());
                    trace.AddMetadata("completion_tokens", tokenStats.CompletionCount.ToString());
                    trace.AddMetadata("total_tokens", tokenStats.TotalCount.ToString());
                }
                
                trace.AddMetadata("message_id", message.MessageId);
                trace.AddMetadata("role", message.Role);
                trace.AddMetadata("agent_id", message.CurrentAgentId ?? "unknown");

                _logger.LogDebug("Updated Langfuse trace for conversation {ConversationId}", conversationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Langfuse AfterGenerated hook");
        }
    }

    public async Task BeforeFunctionInvoked(RoleDialogModel message, TokenStatsModel tokenStats)
    {
        if (!IsEnabled() || !_settings.LogFunctions) return;

        try
        {
            var trace = _services.GetService<LangfuseTrace>();
            if (trace != null)
            {
                trace.AddMetadata($"function_{message.FunctionName}_start", DateTime.UtcNow.ToString("o"));
                trace.AddMetadata($"function_{message.FunctionName}_input", message.Content);
                
                _logger.LogDebug("Started Langfuse function tracking for {FunctionName}", message.FunctionName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Langfuse BeforeFunctionInvoked hook");
        }
    }

    public async Task AfterFunctionInvoked(RoleDialogModel message, TokenStatsModel tokenStats)
    {
        if (!IsEnabled() || !_settings.LogFunctions) return;

        try
        {
            var trace = _services.GetService<LangfuseTrace>();
            if (trace != null)
            {
                trace.AddMetadata($"function_{message.FunctionName}_end", DateTime.UtcNow.ToString("o"));
                trace.AddMetadata($"function_{message.FunctionName}_output", message.ExecutionResult?.ToString() ?? "");
                trace.AddMetadata($"function_{message.FunctionName}_success", (message.ExecutionResult != null).ToString());

                _logger.LogDebug("Completed Langfuse function tracking for {FunctionName}", message.FunctionName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Langfuse AfterFunctionInvoked hook");
        }
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
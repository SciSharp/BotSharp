using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Functions.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;

namespace BotSharp.Abstraction.Diagnostics.Telemetry;

public class TelemetryService : ITelemetryService
{
    private readonly IMachineInformationProvider _informationProvider;
    private readonly bool _isEnabled;
    private readonly ILogger<TelemetryService> _logger;
    private readonly List<KeyValuePair<string, object?>> _tagsList;
    private readonly SemaphoreSlim _initalizeLock = new(1);

    /// <summary>
    /// Task created on the first invocation of <see cref="InitializeAsync"/>.
    /// This is saved so that repeated invocations will see the same exception
    /// as the first invocation.
    /// </summary>
    private Task? _initalizationTask = null;

    private bool _initializationSuccessful;
    private bool _isInitialized;

    public ActivitySource Parent { get; }

    public TelemetryService(IMachineInformationProvider informationProvider,
        IOptions<BotSharpOTelOptions> options,
        ILogger<TelemetryService> logger)
    {
        _isEnabled = options.Value.IsTelemetryEnabled;
        _tagsList =
        [
            new(TelemetryConstants.TagName.BotSharpVersion, options.Value.Version),
        ];


        Parent = new ActivitySource(options.Value.Name, options.Value.Version);
        _informationProvider = informationProvider;
        _logger = logger;
    }

    /// <summary>
    /// TESTING PURPOSES ONLY: Gets the default tags used for telemetry.
    /// </summary>
    internal IReadOnlyList<KeyValuePair<string, object?>> GetDefaultTags()
    {
        if (!_isEnabled)
        {
            return [];
        }

        CheckInitialization();
        return [.. _tagsList];
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public Activity? StartActivity(string activityId) => StartActivity(activityId, null);

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public Activity? StartActivity(string activityId, Implementation? clientInfo)
    {
        if (!_isEnabled)
        {
            return null;
        }

        CheckInitialization();

        var activity = Parent.StartActivity(activityId);

        if (activity == null)
        {
            return activity;
        }

        if (clientInfo != null)
        {
            activity.AddTag(TelemetryConstants.TagName.ClientName, clientInfo.Name)
                .AddTag(TelemetryConstants.TagName.ClientVersion, clientInfo.Version);
        }

        activity.AddTag(TelemetryConstants.TagName.EventId, Guid.NewGuid().ToString());

        _tagsList.ForEach(kvp => activity.AddTag(kvp.Key, kvp.Value));

        return activity;
    }

    public Activity? StartTextCompletionActivity(Uri? endpoint, string modelName, string modelProvider, string prompt, IConversationStateService services)
    {
        if (!IsModelDiagnosticsEnabled())
        {
            return null;
        }

        const string OperationName = "text.completions";
        var activity = Parent.StartActivityWithTags(
            $"{OperationName} {modelName}",
            [
                new(TelemetryConstants.ModelDiagnosticsTags.Operation, OperationName),
                new(TelemetryConstants.ModelDiagnosticsTags.System, modelProvider),
                new(TelemetryConstants.ModelDiagnosticsTags.Model, modelName),
            ],
            ActivityKind.Client);

        if (endpoint is not null)
        {
            activity?.SetTags([
                // Skip the query string in the uri as it may contain keys
                new(TelemetryConstants.ModelDiagnosticsTags.Address, endpoint.GetLeftPart(UriPartial.Path)),
                new(TelemetryConstants.ModelDiagnosticsTags.Port, endpoint.Port),
            ]);
        }

        AddOptionalTags(activity, services);

        if (ActivityExtensions.s_enableSensitiveEvents)
        {
            activity?.AttachSensitiveDataAsEvent(
                TelemetryConstants.ModelDiagnosticsTags.UserMessage,
                [
                    new(TelemetryConstants.ModelDiagnosticsTags.EventName, prompt),
                    new(TelemetryConstants.ModelDiagnosticsTags.System, modelProvider),
                ]);
        }

        return activity;
    }

    public Activity? StartCompletionActivity(Uri? endpoint, string modelName, string modelProvider, List<RoleDialogModel> chatHistory, IConversationStateService conversationStateService)
    {
        if (!IsModelDiagnosticsEnabled())
        {
            return null;
        }

        const string OperationName = "chat.completions";
        var activity = Parent.StartActivityWithTags(
            $"{OperationName} {modelName}",
            [
                new(TelemetryConstants.ModelDiagnosticsTags.Operation, OperationName),
                new(TelemetryConstants.ModelDiagnosticsTags.System, modelProvider),
                new(TelemetryConstants.ModelDiagnosticsTags.Model, modelName),
            ],
            ActivityKind.Client);

        if (endpoint is not null)
        {
            activity?.SetTags([
                // Skip the query string in the uri as it may contain keys
                new(TelemetryConstants.ModelDiagnosticsTags.Address, endpoint.GetLeftPart(UriPartial.Path)),
                new(TelemetryConstants.ModelDiagnosticsTags.Port, endpoint.Port),
            ]);
        }

        AddOptionalTags(activity, conversationStateService);

        if (ActivityExtensions.s_enableSensitiveEvents)
        {
            foreach (var message in chatHistory)
            {
                var formattedContent = JsonSerializer.Serialize(ToGenAIConventionsFormat(message));
                activity?.AttachSensitiveDataAsEvent(
                    TelemetryConstants.ModelDiagnosticsTags.RoleToEventMap[message.Role],
                    [
                        new(TelemetryConstants.ModelDiagnosticsTags.EventName, formattedContent),
                        new(TelemetryConstants.ModelDiagnosticsTags.System, modelProvider),
                    ]);
            }
        }

        return activity;
    }

    public Activity? StartAgentInvocationActivity(string agentId, string agentName, string? agentDescription, Agent? agents, List<RoleDialogModel> messages)
    {
        if (!IsModelDiagnosticsEnabled())
        {
            return null;
        }

        const string OperationName = "invoke_agent";

        var activity = Parent.StartActivityWithTags(
            $"{OperationName} {agentName}",
            [
                new(TelemetryConstants.ModelDiagnosticsTags.Operation, OperationName),
                new(TelemetryConstants.ModelDiagnosticsTags.AgentId, agentId),
                new(TelemetryConstants.ModelDiagnosticsTags.AgentName, agentName)
            ],
            ActivityKind.Internal);

        if (!string.IsNullOrWhiteSpace(agentDescription))
        {
            activity?.SetTag(TelemetryConstants.ModelDiagnosticsTags.AgentDescription, agentDescription);
        }

        if (agents is not null && (agents.Functions.Count > 0 || agents.SecondaryFunctions.Count > 0))
        {
            List<FunctionDef> allFunctions = [];
            allFunctions.AddRange(agents.Functions);
            allFunctions.AddRange(agents.SecondaryFunctions);

            activity?.SetTag(
                TelemetryConstants.ModelDiagnosticsTags.AgentToolDefinitions,
                JsonSerializer.Serialize(messages.Select(m => ToGenAIConventionsFormat(m))));
        }

        if (IsSensitiveEventsEnabled())
        {
            activity?.SetTag(
                TelemetryConstants.ModelDiagnosticsTags.AgentInvocationInput,
                JsonSerializer.Serialize(messages.Select(m => ToGenAIConventionsFormat(m))));
        }

        return activity;
    }


    public void Dispose()
    {

    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public async Task InitializeAsync()
    {
        if (!_isEnabled)
        {
            return;
        }

        // Quick check if initialization already happened. Avoids
        // trying to get the lock.
        if (_initalizationTask == null)
        {
            // Get async lock for starting initialization
            await _initalizeLock.WaitAsync();

            try
            {
                // Check after acquiring lock to ensure we honor work
                // started while we were waiting.
                if (_initalizationTask == null)
                {
                    _initalizationTask = InnerInitializeAsync();
                }
            }
            finally
            {
                _initalizeLock.Release();
            }
        }

        // Await the response of the initialization work regardless of if
        // we or another invocation created the Task representing it. All
        // awaiting on this will give the same result to ensure idempotency.
        await _initalizationTask;

        async Task InnerInitializeAsync()
        {
            try
            {
                var macAddressHash = await _informationProvider.GetMacAddressHash();
                var deviceId = await _informationProvider.GetOrCreateDeviceId();

                _tagsList.Add(new(TelemetryConstants.TagName.MacAddressHash, macAddressHash));
                _tagsList.Add(new(TelemetryConstants.TagName.DevDeviceId, deviceId));

                _initializationSuccessful = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred initializing telemetry service.");
                throw;
            }
            finally
            {
                _isInitialized = true;
            }
        }
    }

    private void CheckInitialization()
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException(
                $"Telemetry service has not been initialized. Use {nameof(InitializeAsync)}() before any other operations.");
        }

        if (!_initializationSuccessful)
        {
            throw new InvalidOperationException("Telemetry service was not successfully initialized. Check logs for initialization errors.");
        }

    }

    internal bool IsModelDiagnosticsEnabled()
    {
        return (ActivityExtensions.s_enableDiagnostics || ActivityExtensions.s_enableSensitiveEvents) && Parent.HasListeners();
    }

    /// <summary>
    /// Check if sensitive events are enabled.
    /// Sensitive events are enabled if EnableSensitiveEvents is set to true and there are listeners.
    /// </summary>
    internal  bool IsSensitiveEventsEnabled() => ActivityExtensions.s_enableSensitiveEvents && Parent.HasListeners();

    private static void AddOptionalTags(Activity? activity, IConversationStateService conversationStateService)
    {
        if (activity is null)
        {
            return;
        }

        void TryAddTag(string key, string tag)
        {
            var value = conversationStateService.GetState(key);
            if (!string.IsNullOrEmpty(value))
            {
                activity.SetTag(tag, value);
            }
        }

        TryAddTag("max_tokens", TelemetryConstants.ModelDiagnosticsTags.MaxToken);
        TryAddTag("temperature", TelemetryConstants.ModelDiagnosticsTags.Temperature);
        TryAddTag("top_p", TelemetryConstants.ModelDiagnosticsTags.TopP);
    }

    /// <summary>
    /// Convert a chat message to a JSON object based on the OTel GenAI Semantic Conventions format
    /// </summary>
    private static object ToGenAIConventionsFormat(RoleDialogModel chatMessage)
    {
        return new
        {
            role = chatMessage.Role.ToString(),
            name = chatMessage.MessageId,
            content = chatMessage.Content,
            tool_calls = ToGenAIConventionsToolCallFormat(chatMessage),
        };
    }

    /// <summary>
    /// Helper method to convert tool calls to a list of JSON object based on the OTel GenAI Semantic Conventions format
    /// </summary>
    private static List<object> ToGenAIConventionsToolCallFormat(RoleDialogModel chatMessage)
    {
        List<object> toolCalls = [];
        if (chatMessage.Instruction is not null)
        {
            toolCalls.Add(new
            {
                id = chatMessage.ToolCallId,
                function = new
                {
                    name = chatMessage.Instruction.Function,
                    arguments = chatMessage.Instruction.Arguments
                },
                type = "function"
            });
        }
        return toolCalls;
    }
}
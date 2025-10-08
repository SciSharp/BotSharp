using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Functions.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Text.Json;

namespace BotSharp.Abstraction.Diagnostics;

/// <summary>
/// Model diagnostics helper class that provides a set of methods to trace model activities with the OTel semantic conventions.
/// This class contains experimental features and may change in the future.
/// To enable these features, set one of the following switches to true:
///     `BotSharp.Experimental.GenAI.EnableOTelDiagnostics`
///     `BotSharp.Experimental.GenAI.EnableOTelDiagnosticsSensitive`
/// Or set the following environment variables to true:
///    `BOTSHARP_EXPERIMENTAL_GENAI_ENABLE_OTEL_DIAGNOSTICS`
///    `BOTSHARP_EXPERIMENTAL_GENAI_ENABLE_OTEL_DIAGNOSTICS_SENSITIVE`
/// </summary>
//[System.Diagnostics.CodeAnalysis.Experimental("SKEXP0001")]
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public  static class ModelDiagnostics
{
    private static readonly string s_namespace = typeof(ModelDiagnostics).Namespace!;
    private static readonly ActivitySource s_activitySource = new(s_namespace);

    private const string EnableDiagnosticsSwitch = "BotSharp.Experimental.GenAI.EnableOTelDiagnostics";
    private const string EnableSensitiveEventsSwitch = "BotSharp.Experimental.GenAI.EnableOTelDiagnosticsSensitive";
    private const string EnableDiagnosticsEnvVar = "BOTSHARP_EXPERIMENTAL_GENAI_ENABLE_OTEL_DIAGNOSTICS";
    private const string EnableSensitiveEventsEnvVar = "BOTSHARP_EXPERIMENTAL_GENAI_ENABLE_OTEL_DIAGNOSTICS_SENSITIVE";

    private static readonly bool s_enableDiagnostics = AppContextSwitchHelper.GetConfigValue(EnableDiagnosticsSwitch, EnableDiagnosticsEnvVar);
    private static readonly bool s_enableSensitiveEvents = AppContextSwitchHelper.GetConfigValue(EnableSensitiveEventsSwitch, EnableSensitiveEventsEnvVar);

    /// <summary>
    /// Start a text completion activity for a given model.
    /// The activity will be tagged with the a set of attributes specified by the semantic conventions.
    /// </summary>
    public  static Activity? StartCompletionActivity(
        Uri? endpoint,
        string modelName,
        string modelProvider,
        string prompt, 
        IConversationStateService services
        ) 
    {
        if (!IsModelDiagnosticsEnabled())
        {
            return null;
        }

        const string OperationName = "text.completions";
        var activity = s_activitySource.StartActivityWithTags(
            $"{OperationName} {modelName}",
            [
                new(ModelDiagnosticsTags.Operation, OperationName),
                new(ModelDiagnosticsTags.System, modelProvider),
                new(ModelDiagnosticsTags.Model, modelName),
            ],
            ActivityKind.Client);

        if (endpoint is not null)
        {
            activity?.SetTags([
                // Skip the query string in the uri as it may contain keys
                new(ModelDiagnosticsTags.Address, endpoint.GetLeftPart(UriPartial.Path)),
                new(ModelDiagnosticsTags.Port, endpoint.Port),
            ]);
        }

        AddOptionalTags(activity, services);

        if (s_enableSensitiveEvents)
        {
            activity?.AttachSensitiveDataAsEvent(
                ModelDiagnosticsTags.UserMessage,
                [
                    new(ModelDiagnosticsTags.EventName, prompt),
                    new(ModelDiagnosticsTags.System, modelProvider),
                ]);
        }

        return activity;
    }

    /// <summary>
    /// Start a chat completion activity for a given model.
    /// The activity will be tagged with the a set of attributes specified by the semantic conventions.
    /// </summary>
    public  static Activity? StartCompletionActivity(
        Uri? endpoint,
        string modelName,
        string modelProvider,
        List<RoleDialogModel> chatHistory,
        IConversationStateService conversationStateService
        )
        
    {
        if (!IsModelDiagnosticsEnabled())
        {
            return null;
        }

        const string OperationName = "chat.completions";
        var activity = s_activitySource.StartActivityWithTags(
            $"{OperationName} {modelName}",
            [
                new(ModelDiagnosticsTags.Operation, OperationName),
                new(ModelDiagnosticsTags.System, modelProvider),
                new(ModelDiagnosticsTags.Model, modelName),
            ],
            ActivityKind.Client);

        if (endpoint is not null)
        {
            activity?.SetTags([
                // Skip the query string in the uri as it may contain keys
                new(ModelDiagnosticsTags.Address, endpoint.GetLeftPart(UriPartial.Path)),
                new(ModelDiagnosticsTags.Port, endpoint.Port),
            ]);
        }

        AddOptionalTags(activity, conversationStateService);

        if (s_enableSensitiveEvents)
        {
            foreach (var message in chatHistory)
            {
                var formattedContent = JsonSerializer.Serialize(ToGenAIConventionsFormat(message));
                activity?.AttachSensitiveDataAsEvent(
                    ModelDiagnosticsTags.RoleToEventMap[message.Role],
                    [
                        new(ModelDiagnosticsTags.EventName, formattedContent),
                        new(ModelDiagnosticsTags.System, modelProvider),
                    ]);
            }
        }

        return activity;
    }

    /// <summary>
    /// Start an agent invocation activity and return the activity.
    /// </summary>
    public  static Activity? StartAgentInvocationActivity(
        string agentId,
        string agentName,
        string? agentDescription,
        Agent? agents,
        List<RoleDialogModel> messages
        )
    {
        if (!IsModelDiagnosticsEnabled())
        {
            return null;
        }

        const string OperationName = "invoke_agent";

        var activity = s_activitySource.StartActivityWithTags(
            $"{OperationName} {agentName}",
            [
                new(ModelDiagnosticsTags.Operation, OperationName),
                new(ModelDiagnosticsTags.AgentId, agentId),
                new(ModelDiagnosticsTags.AgentName, agentName)
            ],
            ActivityKind.Internal);

        if (!string.IsNullOrWhiteSpace(agentDescription))
        {
            activity?.SetTag(ModelDiagnosticsTags.AgentDescription, agentDescription);
        }

        if (agents is not null && (agents.Functions.Count > 0 || agents.SecondaryFunctions.Count >0))
        {
            List<FunctionDef> allFunctions = [];
            allFunctions.AddRange(agents.Functions);
            allFunctions.AddRange(agents.SecondaryFunctions);

            activity?.SetTag(
                ModelDiagnosticsTags.AgentToolDefinitions,
                JsonSerializer.Serialize(messages.Select(m => ToGenAIConventionsFormat(m))));
        }

        if (IsSensitiveEventsEnabled())
        {
            activity?.SetTag(
                ModelDiagnosticsTags.AgentInvocationInput,
                JsonSerializer.Serialize(messages.Select(m => ToGenAIConventionsFormat(m))));
        }

        return activity;
    }

    /// <summary>
    /// Set the agent response for a given activity.
    /// </summary>
    public static void SetAgentResponse(this Activity activity, IEnumerable<RoleDialogModel>? responses)
    {
        if (!IsModelDiagnosticsEnabled() || responses is null)
        {
            return;
        }

        if (s_enableSensitiveEvents)
        {
            activity?.SetTag(
                ModelDiagnosticsTags.AgentInvocationOutput,
                JsonSerializer.Serialize(responses.Select(r => ToGenAIConventionsFormat(r))));
        }
    }

    ///// <summary>
    ///// End the agent streaming response for a given activity.
    ///// </summary>
    //internal static void EndAgentStreamingResponse(
    //    this Activity activity,
    //    IEnumerable<StreamingChatMessageContent>? contents)
    //{
    //    if (!IsModelDiagnosticsEnabled() || contents is null)
    //    {
    //        return;
    //    }

    //    Dictionary<int, List<StreamingKernelContent>> choices = [];
    //    foreach (var content in contents)
    //    {
    //        if (!choices.TryGetValue(content.ChoiceIndex, out var choiceContents))
    //        {
    //            choiceContents = [];
    //            choices[content.ChoiceIndex] = choiceContents;
    //        }

    //        choiceContents.Add(content);
    //    }

    //    var chatCompletions = choices.Select(choiceContents =>
    //        {
    //            var lastContent = (StreamingChatMessageContent)choiceContents.Value.Last();
    //            var chatMessage = choiceContents.Value.Select(c => c.ToString()).Aggregate((a, b) => a + b);
    //            return new ChatMessageContent(lastContent.Role ?? AuthorRole.Assistant, chatMessage, metadata: lastContent.Metadata);
    //        }).ToList();

    //    activity?.SetTag(
    //        ModelDiagnosticsTags.AgentInvocationOutput,
    //        JsonSerializer.Serialize(chatCompletions.Select(r => ToGenAIConventionsFormat(r))));
    //}

    ///// <summary>
    ///// Set the text completion response for a given activity.
    ///// The activity will be enriched with the response attributes specified by the semantic conventions.
    ///// </summary>
    //internal static void SetCompletionResponse(this Activity activity, IEnumerable<TextContent> completions, int? promptTokens = null, int? completionTokens = null)
    //    => SetCompletionResponse(activity, completions, promptTokens, completionTokens, ToGenAIConventionsChoiceFormat);

    ///// <summary>
    ///// Set the chat completion response for a given activity.
    ///// The activity will be enriched with the response attributes specified by the semantic conventions.
    ///// </summary>
    //internal static void SetCompletionResponse(this Activity activity, IEnumerable<ChatMessageContent> completions, int? promptTokens = null, int? completionTokens = null)
    //    => SetCompletionResponse(activity, completions, promptTokens, completionTokens, ToGenAIConventionsChoiceFormat);

    ///// <summary>
    ///// Notify the end of streaming for a given activity.
    ///// </summary>
    //internal static void EndStreaming(
    //    this Activity activity,
    //    IEnumerable<StreamingKernelContent>? contents,
    //    IEnumerable<FunctionCallContent>? toolCalls = null,
    //    int? promptTokens = null,
    //    int? completionTokens = null)
    //{
    //    if (IsModelDiagnosticsEnabled())
    //    {
    //        var choices = OrganizeStreamingContent(contents);
    //        SetCompletionResponse(activity, choices, toolCalls, promptTokens, completionTokens);
    //    }
    //}

    /// <summary>
    /// Set the response id for a given activity.
    /// </summary>
    /// <param name="activity">The activity to set the response id</param>
    /// <param name="responseId">The response id</param>
    /// <returns>The activity with the response id set for chaining</returns>
    internal static Activity SetResponseId(this Activity activity, string responseId) => activity.SetTag(ModelDiagnosticsTags.ResponseId, responseId);

    /// <summary>
    /// Set the input tokens usage for a given activity.
    /// </summary>
    /// <param name="activity">The activity to set the input tokens usage</param>
    /// <param name="inputTokens">The number of input tokens used</param>
    /// <returns>The activity with the input tokens usage set for chaining</returns>
    internal static Activity SetInputTokensUsage(this Activity activity, int inputTokens) => activity.SetTag(ModelDiagnosticsTags.InputTokens, inputTokens);

    /// <summary>
    /// Set the output tokens usage for a given activity.
    /// </summary>
    /// <param name="activity">The activity to set the output tokens usage</param>
    /// <param name="outputTokens">The number of output tokens used</param>
    /// <returns>The activity with the output tokens usage set for chaining</returns>
    internal static Activity SetOutputTokensUsage(this Activity activity, int outputTokens) => activity.SetTag(ModelDiagnosticsTags.OutputTokens, outputTokens);

    /// <summary>
    /// Check if model diagnostics is enabled
    /// Model diagnostics is enabled if either EnableModelDiagnostics or EnableSensitiveEvents is set to true and there are listeners.
    /// </summary>
    internal static bool IsModelDiagnosticsEnabled()
    {
        return (s_enableDiagnostics || s_enableSensitiveEvents) && s_activitySource.HasListeners();
    }

    /// <summary>
    /// Check if sensitive events are enabled.
    /// Sensitive events are enabled if EnableSensitiveEvents is set to true and there are listeners.
    /// </summary>
    internal static bool IsSensitiveEventsEnabled() => s_enableSensitiveEvents && s_activitySource.HasListeners();

    internal static bool HasListeners() => s_activitySource.HasListeners();

    #region Private
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

        TryAddTag("max_tokens", ModelDiagnosticsTags.MaxToken);
        TryAddTag("temperature", ModelDiagnosticsTags.Temperature);
        TryAddTag("top_p", ModelDiagnosticsTags.TopP);
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

    /// <summary>
    /// Convert a function metadata to a JSON object based on the OTel GenAI Semantic Conventions format
    /// </summary>
    private static object ToGenAIConventionsFormat(FunctionDef metadata)
    {
        var properties = metadata.Parameters?.Properties;
        var required = metadata.Parameters?.Required; 

        return new
        {
            type = "function",
            name = metadata.Name,
            description = metadata.Description,
            parameters = new
            {
                type = "object",
                properties,
                required,
            }
        };
    }

    /// <summary>
    /// Convert a chat model response to a JSON string based on the OTel GenAI Semantic Conventions format
    /// </summary>
    private static string ToGenAIConventionsChoiceFormat(RoleDialogModel chatMessage, int index)
    {
        var jsonObject = new
        {
            index,
            message = ToGenAIConventionsFormat(chatMessage),
            tool_calls = ToGenAIConventionsToolCallFormat(chatMessage) 
        };

        return JsonSerializer.Serialize(jsonObject);
    }

  

    /// <summary>
    /// Tags used in model diagnostics
    /// </summary>
    private static class ModelDiagnosticsTags
    {
        // Activity tags
        public const string System = "gen_ai.system";
        public const string Operation = "gen_ai.operation.name";
        public const string Model = "gen_ai.request.model";
        public const string MaxToken = "gen_ai.request.max_tokens";
        public const string Temperature = "gen_ai.request.temperature";
        public const string TopP = "gen_ai.request.top_p";
        public const string ResponseId = "gen_ai.response.id";
        public const string ResponseModel = "gen_ai.response.model";
        public const string FinishReason = "gen_ai.response.finish_reason";
        public const string InputTokens = "gen_ai.usage.input_tokens";
        public const string OutputTokens = "gen_ai.usage.output_tokens";
        public const string Address = "server.address";
        public const string Port = "server.port";
        public const string AgentId = "gen_ai.agent.id";
        public const string AgentName = "gen_ai.agent.name";
        public const string AgentDescription = "gen_ai.agent.description";
        public const string AgentInvocationInput = "gen_ai.input.messages";
        public const string AgentInvocationOutput = "gen_ai.output.messages";
        public const string AgentToolDefinitions = "gen_ai.tool.definitions";

        // Activity events
        public const string EventName = "gen_ai.event.content";
        public const string SystemMessage = "gen_ai.system.message";
        public const string UserMessage = "gen_ai.user.message";
        public const string AssistantMessage = "gen_ai.assistant.message";
        public const string ToolMessage = "gen_ai.tool.message";
        public const string Choice = "gen_ai.choice";
        public static readonly Dictionary<string, string> RoleToEventMap = new()
            {
                { AgentRole.System, SystemMessage },
                { AgentRole.User, UserMessage },
                { AgentRole.Assistant, AssistantMessage },
                { AgentRole.Function, ToolMessage }
            };
    }
    # endregion
}

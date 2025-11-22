using BotSharp.Abstraction.Conversations;
using ModelContextProtocol.Protocol;
using System.Diagnostics;

namespace BotSharp.Abstraction.Diagnostics.Telemetry;

public interface ITelemetryService : IDisposable
{
    ActivitySource Parent { get; }

    /// <summary>
    /// Creates and starts a new telemetry activity.
    /// </summary>
    /// <param name="activityName">Name of the activity.</param>
    /// <returns>An Activity object or null if there are no active listeners or telemetry is disabled.</returns>
    /// <exception cref="InvalidOperationException">If the service is not in an operational state or <see cref="InitializeAsync"/> was not invoked.</exception>
    Activity? StartActivity(string activityName);
  
    /// <summary>
    /// Creates and starts a new telemetry activity.
    /// </summary>
    /// <param name="activityName">Name of the activity.</param>
    /// <param name="clientInfo">MCP client information to add to the activity.</param>
    /// <returns>An Activity object or null if there are no active listeners or telemetry is disabled.</returns>
    /// <exception cref="InvalidOperationException">If the service is not in an operational state or <see cref="InitializeAsync"/> was not invoked.</exception>
    Activity? StartActivity(string activityName, Implementation? clientInfo);

    /// <summary>
    /// Creates and starts a new telemetry activity
    /// </summary>
    /// <param name="endpoint"></param>
    /// <param name="modelName"></param>
    /// <param name="modelProvider"></param>
    /// <param name="prompt"></param>
    /// <param name="services"></param>
    /// <returns></returns>
    Activity? StartTextCompletionActivity(Uri? endpoint, string modelName, string modelProvider, string prompt, IConversationStateService services);

    /// <summary>
    /// Creates and starts a new telemetry activity
    /// </summary>
    /// <param name="endpoint"></param>
    /// <param name="modelName"></param>
    /// <param name="modelProvider"></param>
    /// <param name="chatHistory"></param>
    /// <param name="conversationStateService"></param>
    /// <returns></returns>
    Activity? StartCompletionActivity(Uri? endpoint, string modelName, string modelProvider, List<RoleDialogModel> chatHistory, IConversationStateService conversationStateService);

    /// <summary>
    /// Creates and starts a new telemetry activity
    /// </summary>
    /// <param name="agentId"></param>
    /// <param name="agentName"></param>
    /// <param name="agentDescription"></param>
    /// <param name="agents"></param>
    /// <param name="messages"></param>
    /// <returns></returns>
    Activity? StartAgentInvocationActivity(string agentId, string agentName, string? agentDescription, Agent? agents, List<RoleDialogModel> messages);

    /// <summary>
    /// Performs any initialization operations before telemetry service is ready.
    /// </summary>
    /// <returns>A task that completes when initialization is complete.</returns>
    Task InitializeAsync();
}
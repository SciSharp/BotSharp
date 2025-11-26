namespace BotSharp.Abstraction.Diagnostics.Telemetry;

public static class TelemetryConstants
{
    /// <summary>
    /// Name of tags published.
    /// </summary>
    public static class TagName
    {
        public const string BotSharpVersion = "Version";
        public const string ClientName = "ClientName";
        public const string ClientVersion = "ClientVersion";
        public const string DevDeviceId = "DevDeviceId";
        public const string ErrorDetails = "ErrorDetails";
        public const string EventId = "EventId";
        public const string MacAddressHash = "MacAddressHash";
        public const string ToolName = "ToolName";
        public const string ToolArea = "ToolArea";
        public const string ServerMode = "ServerMode";
        public const string IsServerCommandInvoked = "IsServerCommandInvoked";
        public const string Transport = "Transport";
        public const string IsReadOnly = "IsReadOnly";
        public const string Namespace = "Namespace";
        public const string ToolCount = "ToolCount";
        public const string InsecureDisableElicitation = "InsecureDisableElicitation";
        public const string IsDebug = "IsDebug";
        public const string EnableInsecureTransports = "EnableInsecureTransports";
        public const string Tool = "Tool";
    }

    public static class ActivityName
    {
        public const string ListToolsHandler = "ListToolsHandler";
        public const string ToolExecuted = "ToolExecuted";
        public const string ServerStarted = "ServerStarted";
    }

    /// <summary>
    /// 工具输入输出参数键常量类
    /// </summary>
    public static class ToolParameterKeys
    {
        /// <summary>
        /// 输入参数键
        /// </summary>
        public const string Input = "input";

        /// <summary>
        /// 输出参数键
        /// </summary>
        public const string Output = "output";
    }

    /// <summary>
    /// Tags used in model diagnostics
    /// </summary>
    public static class ModelDiagnosticsTags
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
        public const string ToolName = "gen_ai.tool.name";
        public const string ToolMessage = "gen_ai.tool.message";
        public const string ToolDescription = "gen_ai.tool.description";
        public const string Choice = "gen_ai.choice";

        public static readonly Dictionary<string, string> RoleToEventMap = new()
        {
            { AgentRole.System, SystemMessage },
            { AgentRole.User, UserMessage },
            { AgentRole.Assistant, AssistantMessage },
            { AgentRole.Function, ToolMessage }
        };
    }
}

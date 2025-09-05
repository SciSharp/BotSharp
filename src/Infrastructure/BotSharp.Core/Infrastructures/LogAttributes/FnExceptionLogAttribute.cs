using BotSharp.Abstraction.Functions;
using Microsoft.Extensions.Logging.Abstractions;
using Rougamo;
using Rougamo.Context;
using Rougamo.Metadatas;
using System.Reflection;

namespace BotSharp.Core.Infrastructures.Log;

/// <summary>
/// Shared Rougamo-based logging attribute for IFunctionCallback implementations that captures
/// method execution details, parameters, and BotSharp-specific context.
/// This attribute can be used across all BotSharp plugins for consistent function logging.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
[Advice(Feature.OnException)]
public class FnExceptionLogAttribute : AsyncMoAttribute
{
    private readonly bool _logArguments;

    public FnExceptionLogAttribute(
        bool logArguments = true)
    {
        _logArguments = logArguments;
    }

    public override async ValueTask OnExceptionAsync(MethodContext context)
    {
        var logger = GetLogger(context);
        var functionContext = GetFunctionContext(context);
        LogMethodError(logger, context, functionContext, context?.Exception);

        await ValueTask.CompletedTask;
    }

    private ILogger? GetLogger(MethodContext context)
    {
        try
        {
            var target = context.Target;
            var targetType = target?.GetType();

            if (targetType == null) return NullLogger.Instance;

            var loggerField = targetType?.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .FirstOrDefault(f => f.FieldType == typeof(ILogger) ||
                                    (f.FieldType.IsGenericType &&
                                     f.FieldType.GetGenericTypeDefinition() == typeof(ILogger<>)));

            if (loggerField != null)
            {
                return loggerField.GetValue(target) as ILogger;
            }

            var serviceProviderField = targetType?.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .FirstOrDefault(f => f.FieldType == typeof(IServiceProvider));

            if (serviceProviderField != null)
            {
                var serviceProvider = serviceProviderField.GetValue(target) as IServiceProvider;
                var loggerFactory = serviceProvider?.GetService<ILoggerFactory>();
                return loggerFactory?.CreateLogger(targetType) ??
                       NullLogger.Instance;
            }

            return NullLogger.Instance;
        }
        catch
        {
            return NullLogger.Instance;
        }
    }

    private FunctionContext GetFunctionContext(MethodContext context)
    {
        var functionContext = new FunctionContext();

        try
        {
            var target = context.Target;
            var targetType = target?.GetType();
            functionContext.MethodFullName = $"{targetType?.Name}.{context.Method.Name}";

            var serviceProviderField = targetType?.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(f => f.FieldType == typeof(IServiceProvider));

            if (serviceProviderField != null)
            {
                var serviceProvider = serviceProviderField.GetValue(target) as IServiceProvider;
                var stateService = serviceProvider?.GetService<IConversationStateService>();

                if (stateService != null)
                {
                    functionContext.ConversationId = stateService.GetConversationId() ?? "unknown";
                    functionContext.Channel = stateService.GetState("channel", "unknown");
                    functionContext.CurrentAgentId = stateService.GetState("current_agent_id", string.Empty);
                }
            }

            if (target is IFunctionCallback callback)
            {
                functionContext.FunctionName = callback.Name ?? "unknown";
            }

            // Enhanced message argument processing
            if (context.Arguments?.FirstOrDefault(arg => arg is RoleDialogModel) is RoleDialogModel messageArg)
            {
                functionContext.MessageId = messageArg.MessageId ?? "unknown";
                functionContext.CurrentAgentId = messageArg.CurrentAgentId ?? functionContext.CurrentAgentId;
                functionContext.FunctionName = messageArg.FunctionName ?? functionContext.FunctionName;
            }
        }
        catch (Exception ex)
        {
            functionContext.ContextError = $"Context extraction failed: {ex.Message}";
        }

        return functionContext;
    }

    private void LogMethodError(ILogger? logger, MethodContext context, FunctionContext functionContext, Exception? ex)
    {
        if (logger == null || !logger.IsEnabled(LogLevel.Error)) return;

        var argumentsSummary = string.Empty;
        if (_logArguments && context?.Arguments?.Length > 0)
        {
            argumentsSummary = string.Join(", ", context.Arguments.Select((arg, i) =>
                $"arg{i}: {GetArgumentSummary(arg)}"));
        }

        logger.LogError(ex,
                "[FUNCTION_ERROR] {MethodName} | ConvId: {ConversationId} | Channel: {Channel} | AgentId: {AgentId} | Function: {FunctionName} | MsgId: {MessageId} | Exception: {ExceptionType} | Message: {ExceptionMessage}{ArgumentsInfo}",
                functionContext.MethodFullName,
                functionContext.ConversationId,
                functionContext.Channel,
                functionContext.CurrentAgentId,
                functionContext.FunctionName,
                functionContext.MessageId,
                ex?.GetType().Name ?? "Unknown",
                ex?.Message ?? "No message",
                !string.IsNullOrEmpty(argumentsSummary) ? $" | Args: [{argumentsSummary}]" : "");
    }

    private string GetArgumentSummary(object arg)
    {
        if (arg == null) return "null";

        try
        {
            if (arg is string str)
            {
                return $"\"{str}\"";
            }

            if (arg is RoleDialogModel message)
            {
                return $"RoleDialogModel(Role: {message.Role}, Content: {message.Content})";
            }

            if (arg.GetType().IsPrimitive || arg is decimal || arg is DateTime)
            {
                return arg.ToString();
            }

            var json = JsonSerializer.Serialize(arg);
            return json;
        }
        catch
        {
            return $"{arg.GetType().Name}(...)";
        }
    }

    /// <summary>
    /// Function context with additional metadata
    /// </summary>
    private class FunctionContext
    {
        public string ConversationId { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public string CurrentAgentId { get; set; } = string.Empty;
        public string FunctionName { get; set; } = string.Empty;
        public string MessageId { get; set; } = string.Empty;
        public string ContextError { get; set; } = string.Empty;
        public string MethodFullName { get; set; } = string.Empty;
    }
}
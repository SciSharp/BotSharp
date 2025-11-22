// Copyright (c) Microsoft. All rights reserved.

using BotSharp.Abstraction.Diagnostics.Telemetry;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

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
[ExcludeFromCodeCoverage]
public static class ActivityExtensions
{
    private const string EnableDiagnosticsSwitch = "BotSharp.Experimental.GenAI.EnableOTelDiagnostics";
    private const string EnableSensitiveEventsSwitch = "BotSharp.Experimental.GenAI.EnableOTelDiagnosticsSensitive";
    private const string EnableDiagnosticsEnvVar = "BOTSHARP_EXPERIMENTAL_GENAI_ENABLE_OTEL_DIAGNOSTICS";
    private const string EnableSensitiveEventsEnvVar = "BOTSHARP_EXPERIMENTAL_GENAI_ENABLE_OTEL_DIAGNOSTICS_SENSITIVE";

    public static readonly bool s_enableDiagnostics = AppContextSwitchHelper.GetConfigValue(EnableDiagnosticsSwitch, EnableDiagnosticsEnvVar);
    public static readonly bool s_enableSensitiveEvents = AppContextSwitchHelper.GetConfigValue(EnableSensitiveEventsSwitch, EnableSensitiveEventsEnvVar);


    /// <summary>
    /// Starts an activity with the appropriate tags for a kernel function execution.
    /// </summary>
    public static Activity? StartFunctionActivity(this ActivitySource source, string functionName, string functionDescription)
    {
        const string OperationName = "execute_tool";

        return source.StartActivityWithTags($"{OperationName} {functionName}", [
            new KeyValuePair<string, object?>(TelemetryConstants.ModelDiagnosticsTags.Operation, OperationName),
            new KeyValuePair<string, object?>(TelemetryConstants.ModelDiagnosticsTags.ToolName, functionName),
            new KeyValuePair<string, object?>(TelemetryConstants.ModelDiagnosticsTags.ToolDescription, functionDescription)
        ], ActivityKind.Internal);
    }

    /// <summary>
    /// Starts an activity with the specified name and tags.
    /// </summary>
    public static Activity? StartActivityWithTags(this ActivitySource source, string name, IEnumerable<KeyValuePair<string, object?>> tags, ActivityKind kind = ActivityKind.Internal)
        => source.StartActivity(name, kind, default(ActivityContext), tags);

    /// <summary>
    /// Adds tags to the activity.
    /// </summary>
    public static Activity SetTags(this Activity activity, ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        foreach (var tag in tags)
        {
            activity.SetTag(tag.Key, tag.Value);
        }
        return activity;
    }

    /// <summary>
    /// Adds an event to the activity. Should only be used for events that contain sensitive data.
    /// </summary>
    public static Activity AttachSensitiveDataAsEvent(this Activity activity, string name, IEnumerable<KeyValuePair<string, object?>> tags)
    {
        activity.AddEvent(new ActivityEvent(
            name,
            tags: [.. tags]
        ));

        return activity;
    }

    /// <summary>
    /// Sets the error status and type on the activity.
    /// </summary>
    public static Activity SetError(this Activity activity, Exception exception)
    {
        activity.SetTag("error.type", exception.GetType().FullName);
        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
        return activity;
    }
}

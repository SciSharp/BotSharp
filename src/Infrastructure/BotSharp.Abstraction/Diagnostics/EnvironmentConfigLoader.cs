using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Abstraction.Diagnostics;

internal static class EnvironmentConfigLoader
{
    private const string DefaultBaseUrl = "https://cloud.langfuse.com";

    private const string EnvTelemetry = "BOTSHARP_COLLECT_TELEMETRY";
 

    /// <summary>
    /// Loads configuration from environment variables and applies defaults.
    /// </summary>
    public static BotSharpOTelOptions LoadFromEnvironment(IConfiguration? configuration = null)
    {
        var options = new BotSharpOTelOptions();

        // Try configuration first (appsettings.json, etc.)
        if (configuration != null)
        {
            if (bool.TryParse(configuration["Otel:IsTelemetryEnabled"], out bool istelemetryEnabled))
            {
                options.IsTelemetryEnabled = istelemetryEnabled;
            }
        }

        var collectTelemetry = Environment.GetEnvironmentVariable(EnvTelemetry);
        if (!string.IsNullOrWhiteSpace(collectTelemetry))
        {
            options.IsTelemetryEnabled = bool.TryParse(collectTelemetry, out var shouldCollect) && shouldCollect;
        }


        return options;
    }

    /// <summary>
    /// Validates that required options are set.
    /// </summary>
    public static void Validate(BotSharpOTelOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Name))
        {
            throw new InvalidOperationException(
                $"Otel name is required. Set it via code or configuration.");
        }
         
    }

}
using BotSharp.Abstraction.Diagnostics.Telemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace BotSharp.Abstraction.Diagnostics;

public static class OpenTelemetryExtensions
{
    public static void AddOpenTelemetry(this IServiceCollection services,
            IConfiguration configure)
    {
        // Load from environment first
        var options = EnvironmentConfigLoader.LoadFromEnvironment(configure); 

        // Validate configuration
        EnvironmentConfigLoader.Validate(options);

        services.Configure<BotSharpOTelOptions>(cfg =>
           {
               cfg.Name = options.Name;
               cfg.Version = _assemblyVersion.Value;
               cfg.IsTelemetryEnabled = options.IsTelemetryEnabled;
           });

        services.AddSingleton<IMachineInformationProvider, MachineInformationProvider>();
        services.AddSingleton<ITelemetryService, TelemetryService>();

    }

    /// <summary>
    /// Align with --version command.
    /// https://github.com/dotnet/command-line-api/blob/bcdd4b9b424f0ff6ec855d08665569061a5d741f/src/System.CommandLine/Builder/CommandLineBuilderExtensions.cs#L23-L39
    /// </summary>
    private static readonly Lazy<string> _assemblyVersion = new(() =>
    {
        var assembly = Assembly.GetEntryAssembly();

        if (assembly == null)
        {
            throw new InvalidOperationException("Should be able to get entry assembly.");
        }

        var assemblyVersionAttribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();

        if (assemblyVersionAttribute is null)
        {
            return assembly.GetName().Version?.ToString() ?? "";
        }
        else
        {
            return assemblyVersionAttribute.InformationalVersion;
        }
    });
}
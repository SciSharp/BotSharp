using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System;
using System.Threading.Tasks;

namespace BotSharp.Langfuse;

public static class LangfuseSeriveExtention
{
    public static async Task<IServiceCollection> AddLangfuseOpenTelemetry(this IServiceCollection services)
    {
        
        // 从配置文件中获取 Langfuse 设置
        var configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
        var langfuseSection = configuration.GetSection("Langfuse");
        var publicKey = langfuseSection.GetValue<string>(nameof(LangfuseSettings.PublicKey)) ?? string.Empty;
        var secretKey = langfuseSection.GetValue<string>(nameof(LangfuseSettings.SecretKey)) ?? string.Empty;
        var host = langfuseSection.GetValue<string>(nameof(LangfuseSettings.Host)) ?? string.Empty;
        var plainTextBytes = System.Text.Encoding.UTF8.GetBytes($"{publicKey}:{secretKey}");
        string base64EncodedAuth = Convert.ToBase64String(plainTextBytes);

        // Endpoint to the Aspire Dashboard / Grafana Tempo 
        var endpoint = host;

        var resourceBuilder = ResourceBuilder
            .CreateDefault()
            .AddService("TelemetryAspireDashboardQuickstart");

        // Enable model diagnostics with sensitive data.
        AppContext.SetSwitch("Microsoft.SemanticKernel.Experimental.GenAI.EnableOTelDiagnostics", true);
        AppContext.SetSwitch("Microsoft.SemanticKernel.Experimental.GenAI.EnableOTelDiagnosticsSensitive", true);

        var traceProvider = Sdk.CreateTracerProviderBuilder()
            .SetSampler(new AlwaysOnSampler())
            .SetResourceBuilder(resourceBuilder)
            .AddSource("BotSharp*")
            .AddConsoleExporter(options => { options.Targets = ConsoleExporterOutputTargets.Console; })
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(endpoint);
                options.Protocol = OtlpExportProtocol.HttpProtobuf;
                options.Headers = $"Authorization=Basic {base64EncodedAuth}";  
            })
            .Build();

        // 在应用程序退出前明确刷新遥测数据，
        // 对于控制台应用程式非常重要是必須的。
        traceProvider.ForceFlush();
        await Task.Delay(3000);
        
        var meterProvider = Sdk.CreateMeterProviderBuilder()
            .SetResourceBuilder(resourceBuilder)
            .AddMeter("Microsoft.SemanticKernel*")
            .AddOtlpExporter(options => options.Endpoint = new Uri(endpoint))
            .Build();

        services.AddSingleton(traceProvider);
        services.AddSingleton(meterProvider);

        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.AddOpenTelemetry(options =>
            {
                options.SetResourceBuilder(resourceBuilder);
                options.AddConsoleExporter();
                options.AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(endpoint);
                    options.Protocol = OtlpExportProtocol.HttpProtobuf;
                    options.Headers = $"Authorization=Basic {base64EncodedAuth}";
                });
                options.IncludeFormattedMessage = true;
                options.IncludeScopes = true;
            });
            loggingBuilder.SetMinimumLevel(LogLevel.Information);
        });

        return services;
    }
}

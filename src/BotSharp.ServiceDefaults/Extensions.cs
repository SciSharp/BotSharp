using BotSharp.Langfuse;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ServiceDiscovery;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

namespace Microsoft.Extensions.Hosting
{
    // Adds common .NET Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
    // This project should be referenced by each service project in your solution.
    // To learn more about using this project, see https://aka.ms/dotnet/aspire/service-defaults
    public static class Extensions
    {
        public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
        {
            builder.ConfigureOpenTelemetry();

            builder.AddDefaultHealthChecks();

            builder.Services.AddServiceDiscovery();

            builder.Services.ConfigureHttpClientDefaults(http =>
            {
                // Turn on resilience by default
                http.AddStandardResilienceHandler();

                // Turn on service discovery by default
                http.AddServiceDiscovery();
            });

            // Uncomment the following to restrict the allowed schemes for service discovery.
            // builder.Services.Configure<ServiceDiscoveryOptions>(options =>
            // {
            //     options.AllowedSchemes = ["https"];
            // });

            return builder;
        }

        public static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
        {
            // Enable model diagnostics with sensitive data.
            AppContext.SetSwitch("BotSharp.Experimental.GenAI.EnableOTelDiagnostics", true);
            AppContext.SetSwitch("BotSharp.Experimental.GenAI.EnableOTelDiagnosticsSensitive", true);

            builder.Logging.AddOpenTelemetry(logging =>
            { // Use Serilog
                Log.Logger = new LoggerConfiguration()
                // 对请求路径为 / heathz和 / metrics不进行日志记录
                .Filter.ByExcluding(
                    e => e.Properties.TryGetValue("RequestPath", out var value) && (value.ToString().StartsWith("\"/metrics\"") || value.ToString().StartsWith("\"/healthz\""))
                    )
#if DEBUG
                    .MinimumLevel.Information()
#else
    .MinimumLevel.Warning()
#endif
                    .WriteTo.Console()
                    .WriteTo.File("logs/log-.txt",
                        shared: true,
                        rollingInterval: RollingInterval.Day)
                    //.WriteTo.OpenTelemetry(options =>
                    //{
                    //    options.Endpoint = builder.Configuration["OpenTelemetry:Endpoint"];
                    //    options.ResourceAttributes = new Dictionary<string, object>
                    //    {
                    //        ["service.name"] = builder.Configuration["OpenTelemetry:ServiceName"],
                    //        ["index"] = 10,
                    //        ["flag"] = true,
                    //        ["value"] = 3.14
                    //    };
                    //})
                    .CreateLogger();

                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
            });

            builder.Services.AddOpenTelemetry()
                .WithMetrics(metrics =>
                {
                    metrics.AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation();
                })
                .WithTracing(tracing =>
                {
                    tracing.SetResourceBuilder(
                        ResourceBuilder.CreateDefault()
                        .AddService("apiservice", serviceVersion: "1.0.0")
                        )
                    .AddSource("BotSharp")
                    .AddSource("BotSharp.Abstraction.Diagnostics")
                    .AddSource("BotSharp.Core.Routing.Executor");

                    tracing.AddAspNetCoreInstrumentation()
                        // Uncomment the following line to enable gRPC instrumentation (requires the OpenTelemetry.Instrumentation.GrpcNetClient package)
                        //.AddGrpcClientInstrumentation()
                        .AddHttpClientInstrumentation()
                        //.AddOtlpExporter(options =>
                        //{
                        //    //options.Endpoint = new Uri(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://localhost:4317");
                        //    options.Endpoint = new Uri(host);
                        //    options.Protocol = OtlpExportProtocol.HttpProtobuf;
                        //    options.Headers = $"Authorization=Basic {base64EncodedAuth}";
                        //})
                    ;
                       

                });

            builder.AddOpenTelemetryExporters();

            return builder;
        }

        private static IHostApplicationBuilder AddOpenTelemetryExporters(this IHostApplicationBuilder builder)
        {
            var langfuseSection = builder.Configuration.GetSection("Langfuse");
            var useLangfuse = langfuseSection != null;
            var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

            if (useOtlpExporter)
            {
                builder.Services.Configure<OpenTelemetryLoggerOptions>(logging => logging.AddOtlpExporter());
                builder.Services.ConfigureOpenTelemetryMeterProvider(metrics => metrics.AddOtlpExporter());
                if (useLangfuse)
                {                    
                    var publicKey = langfuseSection.GetValue<string>(nameof(LangfuseSettings.PublicKey)) ?? string.Empty;
                    var secretKey = langfuseSection.GetValue<string>(nameof(LangfuseSettings.SecretKey)) ?? string.Empty;
                    var host = langfuseSection.GetValue<string>(nameof(LangfuseSettings.Host)) ?? string.Empty;
                    var plainTextBytes = System.Text.Encoding.UTF8.GetBytes($"{publicKey}:{secretKey}");
                    string base64EncodedAuth = Convert.ToBase64String(plainTextBytes);

                    builder.Services.ConfigureOpenTelemetryTracerProvider(tracing => tracing.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(host);
                        options.Protocol = OtlpExportProtocol.HttpProtobuf;
                        options.Headers = $"Authorization=Basic {base64EncodedAuth}";
                    })
                    );
                }
                else
                {
                    builder.Services.ConfigureOpenTelemetryTracerProvider(tracing => tracing.AddOtlpExporter());
                }
            }

            // Uncomment the following lines to enable the Azure Monitor exporter (requires the Azure.Monitor.OpenTelemetry.AspNetCore package)
            //if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
            //{
            //    builder.Services.AddOpenTelemetry()
            //       .UseAzureMonitor();
            //}

            return builder;
        }

        public static IHostApplicationBuilder AddDefaultHealthChecks(this IHostApplicationBuilder builder)
        {
            builder.Services.AddHealthChecks()
                // Add a default liveness check to ensure app is responsive
                .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

            return builder;
        }

        public static WebApplication MapDefaultEndpoints(this WebApplication app)
        {
            // Adding health checks endpoints to applications in non-development environments has security implications.
            // See https://aka.ms/dotnet/aspire/healthchecks for details before enabling these endpoints in non-development environments.
            if (app.Environment.IsDevelopment())
            {
                // All health checks must pass for app to be considered ready to accept traffic after starting
                app.MapHealthChecks("/health");

                // Only health checks tagged with the "live" tag must pass for app to be considered alive
                app.MapHealthChecks("/alive", new HealthCheckOptions
                {
                    Predicate = r => r.Tags.Contains("live")
                });
            }

            return app;
        }
    }
}

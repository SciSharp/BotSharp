namespace BotSharp.Plugin.CodeAct;

public class CodeActPlugin : IBotSharpPlugin
{
    public string Id => "2a3a9b74-963d-4a71-9c6a-f7503a1dc9d8";
    public string Name => "CodeAct";
    public string Description => "Runtime-neutral CodeAct execution with default-deny bridge policy.";

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        var settings = new CodeActSettings();
        config.Bind("CodeAct", settings);
        services.AddSingleton(settings);

        services.AddScoped<FakeCodeActRuntime>();
        services.AddScoped<OpenSandboxCodeActRuntime>();
        services.AddScoped<ICodeActRuntime>(provider =>
        {
            var codeActSettings = provider.GetRequiredService<CodeActSettings>();
            return codeActSettings.Runtime.ToLowerInvariant() switch
            {
                "fake" => provider.GetRequiredService<FakeCodeActRuntime>(),
                "opensandbox" => provider.GetRequiredService<OpenSandboxCodeActRuntime>(),
                _ => throw new InvalidOperationException($"Unsupported CodeAct runtime '{codeActSettings.Runtime}'.")
            };
        });
        services.AddScoped<IOpenSandboxCodeClient, OpenSandboxHttpCodeClient>();
        services.AddScoped<IFunctionCallback, ExecuteCodeFn>();
        services.AddScoped<IAgentHook, CodeActAgentHook>();
        services.AddSingleton<ICodeActSecurityPolicy, DefaultCodeActSecurityPolicy>();
        services.AddSingleton<ICodeActTokenService, InMemoryCodeActTokenService>();
        services.AddScoped<ICodeActBridge, BotSharpRoutingCodeActBridge>();
    }
}

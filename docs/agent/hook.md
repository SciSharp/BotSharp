# Agent Hook
Agent Hook allows you to dynamically modify the Agent in your business code, such as adding callback functions and modifying system prompt words.
Agent Hook is defined through `IAgentHook`:
```csharp
bool OnAgentLoading(ref string id);
bool OnInstructionLoaded(string template, Dictionary<string, object> dict);
bool OnFunctionsLoaded(List<FunctionDef> functions);
bool OnSamplesLoaded(ref string samples);
void OnAgentLoaded(Agent agent);
```

## Register custom hook in plugin
```csharp
public class MyPlugin : IBotSharpPlugin
{
    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        // Register Hooks
        services.AddScoped<IAgentHook, MyAgentHook>();
    }
}
```

Add a new class inherts from `AgentHookBase` abstract class which has interface of `IAgentHook`.
```csharp
public class MyAgentHook : AgentHookBase
{
    public MyAgentHook(IServiceProvider services, AgentSettings settings) 
        : base(services, settings)
    {
    }
}
```

## Inject function
You can dynamically inject the LLM Callback function into the currently loaded Agent through Agent Hook.
```csharp
public class MyAgentHook : AgentHookBase
{
    public MyAgentHook(IServiceProvider services, AgentSettings settings) 
        : base(services, settings)
    {
    }

    public override bool OnFunctionsLoaded(List<FunctionDef> functions)
    {
        // Inject LLM callback function
        functions.Add(new FunctionDef
        {
            Name = "function_name",
            Description = "description of how LLM will utilize this function."
        });
        return base.OnFunctionsLoaded(functions);
    }
}
```

Implement the concrete function of `IFunctionCallback`.
```csharp
public class MyFunctionFn : IFunctionCallback
{
    public string Name => "function_name";

    public async Task<bool> Execute(RoleDialogModel message)
    {
        // Access external API
        message.ExecutionResult = new object();
        return true;
    }
}
```
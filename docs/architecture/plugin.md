# Plug-in

**BotSharp** adopts a `plug-in` architecture design, and the modules are independent of each other. With `Conversation` as the core, each plug-in completes various functionalities (such as adding LLM provider and Text Embedding provider) through the Hook mechanism. Regarding the Hooks provided by the system, you can refer to the special [Hook section](hooks.md).

Here we will introduce how to add a new plug-in to extend your LLM application. We still use the example of PizzaBot to illustrate how to complete this task.

1. Add Class Library
   Added a new class library called BotSharp.Plugin.PizzaBot and add referece to the web start application

2. Implement Interface
   Add a new class named `PizzaBotPlugin` and implement interface `IBotSharpPlugin`.
    ```csharp
    namespace BotSharp.Plugin.PizzaBot;

    public class PizzaBotPlugin : IBotSharpPlugin
    {
        public void RegisterDI(IServiceCollection services, IConfiguration config)
        {
            // Register callback function
            services.AddScoped<IFunctionCallback, MakePaymentFn>();

            // Register hooks
            services.AddScoped<IAgentHook, PizzaBotAgentHook>();
        }
    }    
    ```

3. Add to Settings
   Add plugin to appsettings.json.
    ```json
    "PluginLoader": {
        "Assemblies": [
            "BotSharp.Plugin.PizzaBot"
        ]
    }
    ```

After starting the web project, you should see a successful loading message printed in the Console.
```shell
Loaded plugin PizzaBotPlugin from BotSharp.Plugin.PizzaBot.
```
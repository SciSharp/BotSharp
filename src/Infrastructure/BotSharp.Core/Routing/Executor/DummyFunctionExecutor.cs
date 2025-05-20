using BotSharp.Abstraction.Routing.Executor;
using BotSharp.Abstraction.Templating;

namespace BotSharp.Core.Routing.Executor;

public class DummyFunctionExecutor: IFunctionExecutor
{
    private readonly IServiceProvider _services;
    private readonly FunctionDef _functionDef;

    public DummyFunctionExecutor(IServiceProvider services, FunctionDef functionDef)
    {
        _services = services;
        _functionDef = functionDef;
    }

    public async Task<bool> ExecuteAsync(RoleDialogModel message)
    {           
        var render = _services.GetRequiredService<ITemplateRender>();
        var state = _services.GetRequiredService<IConversationStateService>();

        var dict = new Dictionary<string, object>();
        foreach (var item in state.GetStates())
        {
            dict[item.Key] = item.Value;
        }

        var text = render.Render(_functionDef.Output!, dict);
        message.Content = text;
        return true;
    }

    public async Task<string> GetIndicatorAsync(RoleDialogModel message)
    {
        return "Running";
    }
}

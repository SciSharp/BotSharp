using BotSharp.Abstraction.Functions;
using BotSharp.Core.MessageHub;

namespace BotSharp.Core.Demo.Functions;

public class GetFunEventsFn : IFunctionCallback
{
    private readonly IServiceProvider _services;

    public GetFunEventsFn(IServiceProvider services)
    {
        _services = services;
    }

    public string Name => "get_fun_events";
    public string Indication => "Searching fun events";

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<WeatherLocation>(message.FunctionArgs);

        await Task.Delay(1000);

        _services.GetHub().PushIndication(message, $"Start querying event data in {args?.City}");

        await Task.Delay(1500);

        _services.GetHub().PushIndication(message, $"Still searching events in {args?.City}");

        await Task.Delay(1500);

        message.Content = $"Here in {args?.City}, there are a lot of fun events in summer.";
        message.StopCompletion = true;
        return true;
    }
}
using BotSharp.Abstraction.Functions;
using BotSharp.Core.MessageHub;

namespace BotSharp.Core.Demo.Functions;

public class GetWeatherFn : IFunctionCallback
{
    private readonly IServiceProvider _services;

    public GetWeatherFn(IServiceProvider services)
    {
        _services = services;
    }

    public string Name => "get_weather";
    public string Indication => "Querying weather";

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        var messageHub = _services.GetRequiredService<MessageHub<HubObserveData>>();

        await Task.Delay(1000);

        message.Indication = "Start querying weather data";
        messageHub.Push(new()
        {
            EventName = ChatEvent.OnIndicationReceived,
            Data = message,
            ServiceProvider = _services
        });

        await Task.Delay(1500);

        message.Indication = "Still working on it";
        messageHub.Push(new()
        {
            EventName = ChatEvent.OnIndicationReceived,
            Data = message,
            ServiceProvider = _services
        });

        await Task.Delay(1500);

        message.Content = $"It is a sunny day!";
        message.StopCompletion = false;
        return true;
    }
}
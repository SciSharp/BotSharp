using BotSharp.Abstraction.Functions;
using BotSharp.Core.MessageHub;
using System.Text.Json.Serialization;

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
        var args = JsonSerializer.Deserialize<WeatherLocation>(message.FunctionArgs);
        var conv = _services.GetRequiredService<IConversationService>();
        var messageHub = _services.GetRequiredService<MessageHub<HubObserveData<RoleDialogModel>>>();

        await Task.Delay(1000);

        message.Indication = $"Start querying weather data in {args?.City}";
        messageHub.Push(new()
        {
            EventName = ChatEvent.OnIndicationReceived,
            Data = message,
            RefId = conv.ConversationId
        });

        await Task.Delay(1500);

        message.Indication = $"Still working on it... Hold on, {args?.City}";
        messageHub.Push(new()
        {
            EventName = ChatEvent.OnIndicationReceived,
            Data = message,
            RefId = conv.ConversationId
        });

        await Task.Delay(1500);

        message.Content = $"It is a sunny day!";
        message.StopCompletion = false;
        return true;
    }
}

class WeatherLocation
{
    [JsonPropertyName("city")]
    public string City { get; set; }
}
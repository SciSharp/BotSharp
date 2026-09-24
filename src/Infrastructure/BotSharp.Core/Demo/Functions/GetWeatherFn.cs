using BotSharp.Abstraction.Conversations.Enums;
using BotSharp.Abstraction.Infrastructures.Enums;
using BotSharp.Abstraction.Functions;
using BotSharp.Abstraction.Models;
using BotSharp.Abstraction.SideCar;
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

        _services.GetHub().PushIndication(message, $"Start querying weather data in {args?.City}");

        await Task.Delay(1500);

#if DEBUG
        var intermediateMsg = RoleDialogModel.From(message, AgentRole.Assistant, $"Here is your weather in {args?.City}");
        messageHub.Push(new()
        {
            EventName = ChatEvent.OnIntermediateMessageReceivedFromAssistant,
            Data = intermediateMsg,
            RefId = conv.ConversationId,
            SaveDataToDb = true
        });
#endif

        _services.GetHub().PushIndication(message, $"Still working on it... Hold on, {args?.City}");

        await Task.Delay(1500);

        message.Content = $"It is a sunny day!";
        message.StopCompletion = false;

#if DEBUG
        var sidecar = _services.GetService<IConversationSideCar>();
        if (sidecar != null)
        {
            var text = $"I want to know fun events in {args?.City}";
            var states = new List<MessageState>
            {
                new() { Key = StateConst.CHANNEL, Value = ConversationChannel.Email }
            };

            var msg = await sidecar.SendMessage(message.CurrentAgentId, text, states: states);
            message.Content = $"{message.Content} {msg.Content}";
        }
#endif

        return true;
    }
}

class WeatherLocation
{
    [JsonPropertyName("city")]
    public string City { get; set; }
}
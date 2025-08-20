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
        var conv = _services.GetRequiredService<IConversationService>();
        var messageHub = _services.GetRequiredService<MessageHub<HubObserveData<RoleDialogModel>>>();

        await Task.Delay(1000);

        message.Indication = $"Start querying event data in {args?.City}";
        messageHub.Push(new()
        {
            EventName = ChatEvent.OnIndicationReceived,
            Data = message,
            RefId = conv.ConversationId
        });

        await Task.Delay(1500);

        message.Indication = $"Still searching events in {args?.City}";
        messageHub.Push(new()
        {
            EventName = ChatEvent.OnIndicationReceived,
            Data = message,
            RefId = conv.ConversationId
        });

        await Task.Delay(1500);

        message.Content = $"Here in {args?.City}, there are a lot of fun events in summer.";
        message.StopCompletion = true;
        return true;
    }
}
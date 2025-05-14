using BotSharp.Abstraction.Functions;
using BotSharp.Abstraction.Models;
using BotSharp.Abstraction.Options;
using BotSharp.Abstraction.SideCar;
using System.Text.Json.Serialization;

namespace BotSharp.Core.Functions;

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
        //var args = JsonSerializer.Deserialize<Location>(message.FunctionArgs, BotSharpOptions.defaultJsonOptions);

        //var sidecar = _services.GetService<IConversationSideCar>();
        //var states = GetSideCarStates();

        //var userMessage = $"Please find the information at location {args.City}, {args.State}";
        //var response = await sidecar.SendMessage(BuiltInAgentId.Chatbot, userMessage, states: states);
        message.Content = $"It is a sunny day.";
        return true;
    }

    private List<MessageState> GetSideCarStates()
    {
        var sideCarStates = new List<MessageState>()
        {
            new("channel", "email")
        };
        return sideCarStates;
    }
}

class Location
{
    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("state")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? State { get; set; }

    [JsonPropertyName("county")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? County { get; set; }
}
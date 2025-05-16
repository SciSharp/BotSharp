using BotSharp.Abstraction.Functions;

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
        message.Content = $"It is a sunny day!";
        //message.StopCompletion = true;
        return true;
    }
}
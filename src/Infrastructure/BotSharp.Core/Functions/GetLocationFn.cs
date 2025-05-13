using BotSharp.Abstraction.Functions;
using BotSharp.Abstraction.Options;

namespace BotSharp.Core.Functions;

public class GetLocationFn : IFunctionCallback
{
    private readonly IServiceProvider _services;

    public GetLocationFn(IServiceProvider services)
    {
        _services = services;
    }

    public string Name => "get_location";
    public string Indication => "Finding location";

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<Location>(message.FunctionArgs, BotSharpOptions.defaultJsonOptions);

        message.Content = $"There are a lot of fun events here in {args.City}";
        return true;
    }
}

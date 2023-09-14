using BotSharp.Abstraction.Repositories;

namespace BotSharp.Core.Routing.Services;

public partial class RoutingService
{
    public async Task DeleteRoutingItems()
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        db.DeleteRoutingItems();
        await Task.CompletedTask;
    }

    public async Task DeleteRoutingProfiles()
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        db.DeleteRoutingProfiles();
        await Task.CompletedTask;
    }
}

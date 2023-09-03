using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Repositories.Records;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Routing.Models;

namespace BotSharp.Core.Routing.Services;

public class RoutingService : IRoutingService
{
    private readonly IServiceProvider _services;

    public RoutingService(IServiceProvider service)
    {
        _services = service;
    }
    public async Task<List<RoutingItem>> CreateRoutingItems(List<RoutingItem> routingItems)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var items = routingItems?.Select(x => RoutingItemRecord.FromRoutingItem(x))?.ToList() ?? new List<RoutingItemRecord>();
        var savedItems = db.CreateRoutingItems(items);
        return await Task.FromResult(savedItems.Select(x => x.ToRoutingItem()).ToList());
    }

    public async Task<List<RoutingProfile>> CreateRoutingProfiles(List<RoutingProfile> routingProfiles)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var profiles = routingProfiles?.Select(x => RoutingProfileRecord.FromRoutingProfile(x))?.ToList() ?? new List<RoutingProfileRecord>();
        var savedProfiles = db.CreateRoutingProfiles(profiles);
        return await Task.FromResult(savedProfiles.Select(x => x.ToRoutingProfile()).ToList());
    }

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

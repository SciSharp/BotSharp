using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Routing.Models;
using System.IO;

namespace BotSharp.Core.Routing.Services;

public partial class RoutingService
{
    public async Task<List<RoutingItem>> CreateRoutingItems(List<RoutingItem> routingItems)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var items = FetchRoutingItemsFromFile();

        if (items.IsNullOrEmpty())
        {
            items = routingItems?.ToList() ?? new List<RoutingItem>();
        }

        var savedItems = db.CreateRoutingItems(items);
        return await Task.FromResult(savedItems.ToList());
    }

    public async Task<List<RoutingProfile>> CreateRoutingProfiles(List<RoutingProfile> routingProfiles)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var profiles = FetchRoutingProfilesFromFile();

        if (profiles.IsNullOrEmpty())
        {
            profiles = routingProfiles?.ToList() ?? new List<RoutingProfile>();
        }

        var savedProfiles = db.CreateRoutingProfiles(profiles);
        return await Task.FromResult(savedProfiles.ToList());
    }

    private List<RoutingItem> FetchRoutingItemsFromFile()
    {
        var routingItems = new List<RoutingItem>();
        var dbSettings = _services.GetRequiredService<BotSharpDatabaseSettings>();
        var agentSettings = _services.GetRequiredService<AgentSettings>();
        var filePath = Path.Combine(dbSettings.FileRepository, agentSettings.DataDir, "route.json");

        if (File.Exists(filePath))
        {
            routingItems = JsonSerializer.Deserialize<List<RoutingItem>>(File.ReadAllText(filePath), _options);
        }

        return routingItems ?? new List<RoutingItem>();
    }

    private List<RoutingProfile> FetchRoutingProfilesFromFile()
    {
        var routingProfiles = new List<RoutingProfile>();
        var dbSettings = _services.GetRequiredService<BotSharpDatabaseSettings>();
        var agentSettings = _services.GetRequiredService<AgentSettings>();
        var filePath = Path.Combine(dbSettings.FileRepository, agentSettings.DataDir, "routing-profile.json");

        if (File.Exists(filePath))
        {
            routingProfiles = JsonSerializer.Deserialize<List<RoutingProfile>>(File.ReadAllText(filePath), _options);
        }

        return routingProfiles ?? new List<RoutingProfile>();
    }
}

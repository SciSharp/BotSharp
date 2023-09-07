using BotSharp.Abstraction.ApiAdapters;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Routing.Models;
using BotSharp.OpenAPI.ViewModels.Routing;

namespace BotSharp.OpenAPI.Controllers;

[Authorize]
[ApiController]
public class RoutingController : ControllerBase, IApiAdapter
{
    private readonly IRoutingService _routingService;
    public RoutingController(IRoutingService routingService)
    {
        _routingService = routingService;
    }

    [HttpPost("/routing/items")]
    public async Task<List<RoutingItemViewModel>> CreateRoutingItems(List<RoutingItemCreationModel> routingItems)
    {
        var items = routingItems?.Select(x => x.ToRoutingItem())?.ToList() ?? new List<RoutingItem>();
        var savedItems = await _routingService.CreateRoutingItems(items);
        return savedItems.Select(x => RoutingItemViewModel.FromRoutingItem(x)).ToList();
    }

    [HttpPost("/routing/profiles")]
    public async Task<List<RoutingProfileViewModel>> CreateRoutingProfiles(List<RoutingProfileCreationModel> routingProfiles)
    {
        var profiles = routingProfiles?.Select(x => x.ToRoutingProfile())?.ToList() ?? new List<RoutingProfile>();
        var savedProfiles = await _routingService.CreateRoutingProfiles(profiles);
        return savedProfiles.Select(x => RoutingProfileViewModel.FromRoutingProfile(x)).ToList();
    }

    [HttpDelete("/routing/items")]
    public async Task RemoveRoutingItems()
    {
        await _routingService.DeleteRoutingItems();
    }

    [HttpDelete("/routing/profiles")]
    public async Task RemoveRoutingProfiles()
    {
        await _routingService.DeleteRoutingProfiles();
    }
}

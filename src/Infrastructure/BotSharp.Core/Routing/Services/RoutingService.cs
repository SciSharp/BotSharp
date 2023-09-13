using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Routing.Models;
using System.IO;

namespace BotSharp.Core.Routing.Services;

public partial class RoutingService : IRoutingService
{
    private readonly IServiceProvider _services;
    private readonly JsonSerializerOptions _options;

    public RoutingService(IServiceProvider service)
    {
        _services = service;
        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }
}

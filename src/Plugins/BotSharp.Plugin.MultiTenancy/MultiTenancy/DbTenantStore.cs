using BotSharp.Abstraction.MultiTenancy;
using BotSharp.Abstraction.MultiTenancy.Options;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace BotSharp.Plugin.MultiTenancy.MultiTenancy;

public class DbTenantStore : ITenantStore
{
    private readonly ITenantRepository _repo;
    private readonly ILogger<DbTenantStore> _logger;

    public DbTenantStore(ITenantRepository repo, ILogger<DbTenantStore> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public List<TenantConfiguration> GetTenants()
    {
        try
        {
            var tenants = _repo.GetTenants();
            return tenants;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DbTenantStore: failed to load tenant configurations from database.");
            return new List<TenantConfiguration>();
        }
    }
}
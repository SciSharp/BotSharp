using BotSharp.Abstraction.MultiTenancy;
using BotSharp.Abstraction.MultiTenancy.Options;
using BotSharp.Plugin.MultiTenancy.Models;
using BotSharp.Plugin.MultiTenancy.MultiTenancy;
using BotSharp.Plugin.MultiTenancy.MultiTenancy.Resolvers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System.Collections.Generic;

namespace BotSharp.Plugin.MultiTenancy.Extensions;

public static class MultiTenancyServiceCollectionExtensions
{
    public static IServiceCollection AddMultiTenancy(this IServiceCollection services, IConfiguration configuration, string sectionName = "TenantStore")
    {
        var section = configuration.GetSection(sectionName);
        if (section.Exists())
        {
            services.Configure<TenantStoreOptions>(section);
        }
        else
        {
            services.Configure<TenantStoreOptions>(_ => { });
        }

        services.Configure<TenantResolveOptions>(options =>
        {
            options.TenantResolvers.Add(new ClaimsTenantResolveContributor());
            options.TenantResolvers.Add(new HeaderTenantResolveContributor());
            options.TenantResolvers.Add(new QueryStringTenantResolveContributor());
        });

        services.AddScoped<ITenantResolver, TenantResolver>();
        services.AddScoped<MultiTenancyMiddleware>();
        services.AddScoped<ICurrentTenant, CurrentTenant>();
        services.AddSingleton<ICurrentTenantAccessor>(AsyncLocalCurrentTenantAccessor.Instance);

        // tenant store infrastructure
        services.AddMemoryCache();
        services.TryAddScoped<ITenantRepository, NullTenantRepository>();
        services.TryAddScoped<ConfigTenantStore>();
        services.TryAddScoped<DbTenantStore>();
        services.TryAddScoped<ITenantStore>(sp => new CompositeTenantStore(
            sp.GetRequiredService<IOptionsMonitor<TenantStoreOptions>>(),
            new List<ITenantStore>
            {
                sp.GetRequiredService<ConfigTenantStore>(),
                sp.GetRequiredService<DbTenantStore>()
            }));

        services.TryAddScoped<IConnectionStringResolver, DefaultConnectionStringResolver>();
        services.TryAddScoped<ITenantFeature, TenantFeature>();
        services.TryAddScoped<ITenantConnectionProvider, TenantConnectionProvider>();

        return services;
    }
}
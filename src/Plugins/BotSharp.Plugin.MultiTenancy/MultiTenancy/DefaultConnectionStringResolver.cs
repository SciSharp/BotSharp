using BotSharp.Abstraction.MultiTenancy;
using BotSharp.Abstraction.MultiTenancy.Options;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;

namespace BotSharp.Plugin.MultiTenancy.MultiTenancy
{
    public class DefaultConnectionStringResolver : IConnectionStringResolver
    {
        private readonly TenantStoreOptions _tenantStoreOptions;
        private readonly ICurrentTenant _currentTenant;

        public DefaultConnectionStringResolver(IOptionsMonitor<TenantStoreOptions> tenantStoreOptions, ICurrentTenant currentTenant)
        {
            _tenantStoreOptions = tenantStoreOptions.CurrentValue;
            _currentTenant = currentTenant;
        }

        public string? GetConnectionString(string connectionStringName)
        {
            if (!_tenantStoreOptions.Enabled || !_tenantStoreOptions.Tenants.Any()) return null;
            if (_currentTenant.Id.HasValue)
            {
                var tenant = _tenantStoreOptions.Tenants.FirstOrDefault(t => t.Id == _currentTenant.Id.Value);
                return tenant?.ConnectionStrings?.GetValueOrDefault(connectionStringName);
            }

            return null;
        }

        public string? GetConnectionString<TContext>()
        {
            var contextType = typeof(TContext);
            var connStringName = ConnectionStringNameAttribute.GetConnStringName(contextType);
            return GetConnectionString(connStringName);
        }
    }
}
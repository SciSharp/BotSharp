namespace BotSharp.Abstraction.MultiTenancy.Options;

public class TenantStoreOptions
{
    public bool Enabled { get; set; } = false;

    public TenantConfiguration[] Tenants { get; set; } = [];
}
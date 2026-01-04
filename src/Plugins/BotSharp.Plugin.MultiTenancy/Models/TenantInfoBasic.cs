using System;

namespace BotSharp.Plugin.MultiTenancy.Models;

public class TenantInfoBasic
{
    public TenantInfoBasic(Guid? tenantId, string? name)
    {
        TenantId = tenantId;
        Name = name;
    }

    public Guid? TenantId { get; set; }

    public string? Name { get; set; }
}
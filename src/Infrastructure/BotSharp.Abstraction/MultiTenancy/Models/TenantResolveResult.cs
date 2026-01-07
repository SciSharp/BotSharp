namespace BotSharp.Abstraction.MultiTenancy.Models;

public class TenantResolveResult
{
    public Guid? TenantId { get; set; }
    public string? Name { get; set; }
    public bool Succeeded => TenantId.HasValue;
}
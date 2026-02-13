namespace BotSharp.Abstraction.MultiTenancy;
public interface IMultiTenant
{
    /// <summary>
    /// Id of the related tenant.
    /// </summary>
    string? TenantId { get; }
}
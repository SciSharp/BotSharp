namespace BotSharp.Abstraction.MultiTenancy;

public interface ITenantFeature
{
    bool Enabled { get; }
}
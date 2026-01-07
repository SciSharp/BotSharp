namespace BotSharp.Abstraction.MultiTenancy;

public interface ITenantConnectionProvider
{
    string GetConnectionString(string name);
    string GetDefaultConnectionString();
}
namespace BotSharp.Plugin.MultiTenancy.MultiTenancy;

public interface IConnectionStringResolver
{
    string? GetConnectionString(string connectionStringName);

    string? GetConnectionString<TContext>();
}
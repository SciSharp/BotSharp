namespace BotSharp.Abstraction.MultiTenancy;

public interface IConnectionStringResolver
{
    string? GetConnectionString(string connectionStringName);

    string? GetConnectionString<TContext>();
}
namespace BotSharp.Core.Repository;

public class BotSharpDbContext : Database, IBotSharpRepository
{
    public IServiceProvider ServiceProvider => throw new NotImplementedException();
}

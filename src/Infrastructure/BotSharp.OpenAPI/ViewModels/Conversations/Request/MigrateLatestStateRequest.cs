namespace BotSharp.OpenAPI.ViewModels.Conversations;

public class MigrateLatestStateRequest
{
    public int BatchSize { get; set; } = 1000;
    public int ErrorLimit { get; set; } = 10;
}

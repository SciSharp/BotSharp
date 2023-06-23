namespace BotSharp.Core.Agents.Services;

public partial class AgentService : IChatServiceZone
{
    public int Priority => 10;

    /// <summary>
    /// Prepare agent profile and configurations
    /// </summary>
    /// <param name="content"></param>
    /// <returns></returns>
    public async Task Serving(ContentContainer content)
    {
        
    }
}

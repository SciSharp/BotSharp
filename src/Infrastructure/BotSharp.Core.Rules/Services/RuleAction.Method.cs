namespace BotSharp.Core.Rules.Services;

public partial class RuleAction
{
    public async Task<bool> ExecuteMethodAsync(Agent agent, Func<Agent, Task> func)
    {
        try
        {
            await func(agent);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when executing custom method.");
            return false;
        }
    }
}

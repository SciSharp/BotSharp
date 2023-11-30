namespace BotSharp.Logger.Hooks;

public class TokenStatsConversationHook : IContentGeneratingHook
{
    private readonly ITokenStatistics _tokenStatistics;

    public TokenStatsConversationHook(ITokenStatistics tokenStatistics)
    {
        _tokenStatistics = tokenStatistics;
    }

    public async Task BeforeGenerating(Agent agent, List<RoleDialogModel> conversations)
    {
        _tokenStatistics.StartTimer();
        await Task.CompletedTask;
    }

    public async Task AfterGenerated(RoleDialogModel message, TokenStatsModel tokenStats)
    {
        _tokenStatistics.StopTimer();
        tokenStats.PromptCost = 0.0015f;
        tokenStats.CompletionCost = 0.002f;
        _tokenStatistics.AddToken(tokenStats);
        await Task.CompletedTask;
    }
}

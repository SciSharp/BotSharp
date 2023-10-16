using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.MLTasks;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BotSharp.Plugin.AzureOpenAI.Hooks;

/// <summary>
/// Token statistics for Azure OpenAI
/// </summary>
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
    }

    public async Task AfterGenerated(RoleDialogModel message, TokenStatsModel tokenStats)
    {
        _tokenStatistics.StopTimer();

        tokenStats.PromptCost = 0.0015f;
        tokenStats.CompletionCost = 0.002f;
        _tokenStatistics.AddToken(tokenStats);
    }
}

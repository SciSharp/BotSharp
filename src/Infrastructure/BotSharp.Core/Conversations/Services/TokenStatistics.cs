using System.Drawing;

namespace BotSharp.Core.Conversations.Services;

public class TokenStatistics : ITokenStatistics
{
    private int _promptTokenCount = 0;
    private float _promptCost = 0f;
    private int _completionTokenCount = 0;
    private float _completionCost = 0f;
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    public int Total => _promptTokenCount + _completionTokenCount;
    public string _model;

    public float Cost => _promptCost + _completionCost;
    public float AccumulatedCost
    {
        get 
        {
            var stat = _services.GetRequiredService<IConversationStateService>();
            return float.Parse(stat.GetState("llm_total_cost", "0"));
        }
    }

    public TokenStatistics(IServiceProvider services, ILogger<TokenStatistics> logger) 
    { 
        _services = services;
        _logger = logger;
    }

    public void AddToken(TokenStatsModel stats)
    {
        _model = stats.Model;
        _promptTokenCount += stats.PromptCount;
        _completionTokenCount += stats.CompletionCount;
        _promptCost += stats.PromptCount / 1000f * stats.PromptCost;
        _completionCost += stats.CompletionCount / 1000f * stats.CompletionCost;

        // Accumulated Token
        var stat = _services.GetRequiredService<IConversationStateService>();
        var count1 = int.Parse(stat.GetState("prompt_total", "0"));
        stat.SetState("prompt_total", stats.PromptCount + count1);
        var count2 = int.Parse(stat.GetState("completion_total", "0"));
        stat.SetState("completion_total", stats.CompletionCount + count2);

        // Total cost
        var count3 = float.Parse(stat.GetState("llm_total_cost", "0"));
        stat.SetState("llm_total_cost", stats.PromptCount / 1000f * stats.PromptCost + stats.CompletionCount / 1000f * stats.CompletionCost + count3);
    }

    public void PrintStatistics()
    {
        var stats = $"Token Usage: {_promptTokenCount} prompt + {_completionTokenCount} completion = {Total} total tokens. One-Way cost: ${Cost:C4}, accumulated cost: ${AccumulatedCost:C4}. Model: {_model}";
#if DEBUG
        Console.WriteLine(stats, Color.DarkGray);
#else
        _logger.LogInformation(stats);
#endif
    }
}

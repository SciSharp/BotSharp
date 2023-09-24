using System.Drawing;

namespace BotSharp.Core.Conversations.Services;

public class TokenStatistics : ITokenStatistics
{
    private int _promptTokenCount = 0;
    private int _completionTokenCount = 0;
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    public int Total => _promptTokenCount + _completionTokenCount;

    public float Cost => _promptTokenCount / 1000f * 0.0015f + _completionTokenCount / 1000f * 0.002f;

    public float AccumulatedCost
    {
        get 
        {
            var stat = _services.GetRequiredService<IConversationStateService>();
            var promptTokenCount = int.Parse(stat.GetState("prompt_total", "0"));
            var completionTokenCount = int.Parse(stat.GetState("completion_total", "0"));
            return promptTokenCount / 1000f * 0.0015f + completionTokenCount / 1000f * 0.002f;
        }
    }

    public TokenStatistics(IServiceProvider services, ILogger<TokenStatistics> logger) 
    { 
        _services = services;
        _logger = logger;
    }

    public void AddToken(int promptCount, int completionCount)
    {
        _promptTokenCount += promptCount;
        _completionTokenCount += completionCount;

        // Accumulated Token
        var stat = _services.GetRequiredService<IConversationStateService>();
        var count1 = int.Parse(stat.GetState("prompt_total", "0"));
        stat.SetState("prompt_total", promptCount + count1);
        var count2 = int.Parse(stat.GetState("completion_total", "0"));
        stat.SetState("completion_total", completionCount + count2);
    }

    public void PrintStatistics()
    {
#if DEBUG
        Console.WriteLine($"Token Usage: {_promptTokenCount} prompt + {_completionTokenCount} completion = {Total} total (${Cost}), accumulated cost: ${AccumulatedCost}", Color.DarkGray);
#else
        _logger.LogInformation($"Token Usage: {_promptTokenCount} prompt + {_completionTokenCount} completion = {Total} total (${Cost}), accumulated cost: ${AccumulatedCost}");
#endif
    }
}

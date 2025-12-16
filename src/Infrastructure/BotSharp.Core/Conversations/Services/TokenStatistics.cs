using BotSharp.Abstraction.MLTasks;
using System.Diagnostics;

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
    private Stopwatch _timer;

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

    public void AddToken(TokenStatsModel stats, RoleDialogModel message)
    {
        _model = stats.Model;
        _promptTokenCount += stats.TotalInputTokens;
        _completionTokenCount += stats.TotalOutputTokens;

        var settingsService = _services.GetRequiredService<ILlmProviderService>();
        var settings = settingsService.GetSetting(stats.Provider, _model);

        #region Text tokens
        var deltaTextInputCost = GetDeltaCost(stats.TextInputTokens, settings?.Cost?.TextInputCost);
        var deltaCachedTextInputCost = GetDeltaCost(stats.CachedTextInputTokens, settings?.Cost?.CachedTextInputCost);
        var deltaTextOutputCost = GetDeltaCost(stats.TextOutputTokens, settings?.Cost?.TextOutputCost);
        #endregion

        #region Audio tokens
        var deltaAudioInputCost = GetDeltaCost(stats.AudioInputTokens, settings?.Cost?.AudioInputCost);
        var deltaCachedAudioInputCost = GetDeltaCost(stats.CachedAudioInputTokens, settings?.Cost?.CachedAudioInputCost);
        var deltaAudioOutputCost = GetDeltaCost(stats.AudioOutputTokens, settings?.Cost?.AudioOutputCost);
        #endregion

        #region Image tokens
        var deltaImageInputCost = GetDeltaCost(stats.ImageInputTokens, settings?.Cost?.ImageInputCost);
        var deltaCachedImageInputCost = GetDeltaCost(stats.CachedImageInputTokens, settings?.Cost?.CachedImageInputCost);
        var deltaImageOutputCost = GetDeltaCost(stats.ImageOutputTokens, settings?.Cost?.ImageOutputCost);
        #endregion

        #region Image generation
        var deltaImageGenerationCost = stats.ImageGenerationCount * stats.ImageGenerationUnitCost;
        #endregion


        var deltaPromptCost = deltaTextInputCost + deltaCachedTextInputCost 
                            + deltaAudioInputCost + deltaCachedAudioInputCost
                            + deltaImageInputCost + deltaCachedImageInputCost;
        var deltaCompletionCost = deltaTextOutputCost + deltaAudioOutputCost + deltaImageOutputCost;

        var deltaTotal = deltaPromptCost + deltaCompletionCost + deltaImageGenerationCost;
        _promptCost += deltaPromptCost;
        _completionCost += deltaCompletionCost;

        // Accumulated Token
        var state = _services.GetRequiredService<IConversationStateService>();
        var inputCount = int.Parse(state.GetState("prompt_total", "0"));
        state.SetState("prompt_total", stats.TotalInputTokens + inputCount, isNeedVersion: false, source: StateSource.Application);
        var outputCount = int.Parse(state.GetState("completion_total", "0"));
        state.SetState("completion_total", stats.TotalOutputTokens + outputCount, isNeedVersion: false, source: StateSource.Application);

        // Total cost
        var total_cost = float.Parse(state.GetState("llm_total_cost", "0"));
        total_cost += deltaTotal;
        state.SetState("llm_total_cost", total_cost, isNeedVersion: false, source: StateSource.Application);

        // Save stats
        var metric = StatsMetric.AgentLlmCost;
        var dim = "agent";
        var agentId = message.CurrentAgentId ?? string.Empty;
        var globalStats = _services.GetRequiredService<IBotSharpStatsService>();
        var delta = new BotSharpStatsDelta
        {
            AgentId = agentId,
            RecordTime = DateTime.UtcNow,
            IntervalType = StatsInterval.Day,
            CountDelta = new()
            {
                AgentCallCountDelta = 1,
                ImageGenerationTotalCountDelta = stats.ImageGenerationCount
            },
            LlmCostDelta = new()
            {
                PromptTokensDelta = stats.TotalInputTokens,
                CompletionTokensDelta = stats.TotalOutputTokens,
                PromptTotalCostDelta = deltaPromptCost,
                CompletionTotalCostDelta = deltaCompletionCost,
                ImageGenerationTotalCostDelta = deltaImageGenerationCost
            }
        };
        globalStats.UpdateStats($"global-{metric}-{dim}-{agentId}", delta);
    }

    public void PrintStatistics()
    {
        if (_timer == null)
        {
            _timer = Stopwatch.StartNew();
        }
        else
        {
            _timer.Start();
        }
        var stats = $"Token Usage: {_promptTokenCount} prompt + {_completionTokenCount} completion = {Total} total tokens ({_timer.ElapsedMilliseconds / 1000f:f2}s). One-Way cost: {Cost:C4}, accumulated cost: {AccumulatedCost:C4}. [{_model}]";
#if DEBUG
        Console.WriteLine(stats);
#else
        _logger.LogInformation(stats);
#endif
    }

    public void StartTimer()
    {
        if (_timer == null)
        {
            _timer = Stopwatch.StartNew();
        }
        else
        {
            _timer.Start();
        }
    }

    public void StopTimer()
    {
        if (_timer == null)
        {
            return;
        }
        _timer.Stop();
    }

    private float GetDeltaCost(int tokens, float? unitCost)
    {
        return tokens / 1000f * (unitCost ?? 0f);
    }
}

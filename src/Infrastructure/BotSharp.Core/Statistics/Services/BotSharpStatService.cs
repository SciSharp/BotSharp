using BotSharp.Abstraction.Infrastructures;
using BotSharp.Abstraction.Statistics.Settings;

namespace BotSharp.Core.Statistics.Services;

public class BotSharpStatService : IBotSharpStatService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<BotSharpStatService> _logger;
    private readonly StatisticsSettings _settings;

    private const string GLOBAL_LLM_COST = "global-llm-cost";
    private const string GLOBAL_AGENT_CALL = "global-agent-call";
    private const int TIMEOUT_SECONDS = 5;

    public BotSharpStatService(
        IServiceProvider services,
        ILogger<BotSharpStatService> logger,
        StatisticsSettings settings)
    {
        _services = services;
        _logger = logger;
        _settings = settings;
    }

    public bool UpdateLlmCost(BotSharpStats stats)
    {
        try
        {
            if (!_settings.Enabled) return false;

            var db = _services.GetRequiredService<IBotSharpRepository>();
            var locker = _services.GetRequiredService<IDistributedLocker>();

            var res = locker.Lock(GLOBAL_LLM_COST, () =>
            {
                var body = db.GetGlobalStats(stats.Category, stats.Group, stats.RecordTime);
                if (body == null)
                {
                    db.SaveGlobalStats(stats);
                    return;
                }

                foreach (var item in stats.Data)
                {
                    var curValue = item.Value;
                    if (body.Data.TryGetValue(item.Key, out var preValue))
                    {
                        var preValStr = preValue?.ToString();
                        var curValStr = curValue?.ToString();
                        try
                        {
                            if (int.TryParse(preValStr, out var count))
                            {
                                curValue = int.Parse(curValStr ?? "0") + count;
                            }
                            else if (double.TryParse(preValStr, out var num))
                            {
                                curValue = double.Parse(curValStr ?? "0") + num;
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }

                    body.Data[item.Key] = curValue;
                }

                db.SaveGlobalStats(body);
            }, TIMEOUT_SECONDS);
            return res;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error when updating global llm cost stats {stats}. {ex.Message}\r\n{ex.InnerException}");
            return false;
        }
    }

    public bool UpdateAgentCall(BotSharpStats stats)
    {
        try
        {
            if (!_settings.Enabled) return false;

            var db = _services.GetRequiredService<IBotSharpRepository>();
            var locker = _services.GetRequiredService<IDistributedLocker>();

            var res = locker.Lock(GLOBAL_AGENT_CALL, () =>
            {
                var body = db.GetGlobalStats(stats.Category, stats.Group, stats.RecordTime);
                if (body == null)
                {
                    db.SaveGlobalStats(stats);
                    return;
                }

                foreach (var item in stats.Data)
                {
                    var curValue = item.Value;
                    if (body.Data.TryGetValue(item.Key, out var preValue))
                    {
                        var preValStr = preValue?.ToString();
                        var curValStr = curValue?.ToString();
                        try
                        {
                            if (int.TryParse(preValStr, out var count))
                            {
                                curValue = int.Parse(curValStr ?? "0") + count;
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }
                    body.Data[item.Key] = curValue;
                }

                db.SaveGlobalStats(body);
            }, TIMEOUT_SECONDS);
            return res;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error when updating global agent call stats {stats}. {ex.Message}\r\n{ex.InnerException}");
            return false;
        }
    }
}

using System.IO;

namespace BotSharp.Core.Repository;

public partial class FileRepository
{
    public BotSharpStats? GetGlobalStats(string agentId, DateTime recordTime, StatsInterval interval)
    {
        if (string.IsNullOrWhiteSpace(agentId))
        {
            return null;
        }

        var baseDir = Path.Combine(_dbSettings.FileRepository, STATS_FOLDER);
        var (startTime, endTime) = BotSharpStats.BuildTimeInterval(recordTime, interval);
        var dir = Path.Combine(baseDir, agentId, startTime.Year.ToString(), startTime.Month.ToString("D2"));
        if (!Directory.Exists(dir))
        {
            return null;
        }

        var file = Directory.GetFiles(dir).FirstOrDefault(x => Path.GetFileName(x) == STATS_FILE);
        if (file == null)
        {
            return null;
        }

        var text = File.ReadAllText(file);
        var list = JsonSerializer.Deserialize<List<BotSharpStats>>(text, _options);
        var found = list?.FirstOrDefault(x => x.AgentId.IsEqualTo(agentId)
                                            && x.StartTime == startTime
                                            && x.EndTime == endTime);

        return found;
    }

    public bool SaveGlobalStats(BotSharpStatsDelta delta)
    {
        if (delta == null || string.IsNullOrWhiteSpace(delta.AgentId))
        {
            return false;
        }

        var baseDir = Path.Combine(_dbSettings.FileRepository, STATS_FOLDER);
        var (startTime, endTime) = BotSharpStats.BuildTimeInterval(delta.RecordTime, delta.IntervalType);

        var dir = Path.Combine(baseDir, delta.AgentId, startTime.Year.ToString(), startTime.Month.ToString("D2"));
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var newItem = new BotSharpStats
        {
            AgentId = delta.AgentId,
            AgentCallCount = delta.AgentCallCountDelta,
            PromptTokens = delta.PromptTokensDelta,
            CompletionTokens = delta.CompletionTokensDelta,
            PromptTotalCost = delta.PromptTotalCostDelta,
            CompletionTotalCost = delta.CompletionTotalCostDelta,
            RecordTime = delta.RecordTime,
            StartTime = startTime,
            EndTime = endTime,
            Interval = delta.Interval
        };

        var file = Path.Combine(dir, STATS_FILE);
        if (!File.Exists(file))
        {
            var list = new List<BotSharpStats> { newItem };
            File.WriteAllText(file, JsonSerializer.Serialize(list, _options));
        }
        else
        {
            var text = File.ReadAllText(file);
            var list = JsonSerializer.Deserialize<List<BotSharpStats>>(text, _options);
            var found = list?.FirstOrDefault(x => x.AgentId.IsEqualTo(delta.AgentId)
                                                && x.StartTime == startTime
                                                && x.EndTime == endTime);

            if (found != null)
            {
                found.AgentId = delta.AgentId;
                found.RecordTime = delta.RecordTime;
                found.AgentCallCount += delta.AgentCallCountDelta;
                found.PromptTokens += delta.PromptTokensDelta;
                found.CompletionTokens += delta.CompletionTokensDelta;
                found.PromptTotalCost += delta.PromptTotalCostDelta;
                found.CompletionTotalCost += delta.CompletionTotalCostDelta;
                found.StartTime = startTime;
                found.EndTime = endTime;
                found.Interval = delta.Interval;
            }
            else if (list != null)
            {
                list.Add(newItem);
            }
            else if (list == null)
            {
                list = [newItem];
            }

            File.WriteAllText(file, JsonSerializer.Serialize(list, _options));
        }

        return true;
    }
}

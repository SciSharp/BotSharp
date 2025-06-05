using BotSharp.Abstraction.Repositories.Settings;
using System.IO;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService
{
    public async Task<IEnumerable<AgentUtility>> GetAgentUtilityOptions()
    {
        var utilities = new List<AgentUtility>();
        var hooks = _services.GetServices<IAgentUtilityHook>();
        foreach (var hook in hooks)
        {
            hook.AddUtilities(utilities);
        }

        utilities = utilities.Where(x => !string.IsNullOrWhiteSpace(x.Category)
                                 && !string.IsNullOrWhiteSpace(x.Name)
                                 && !x.Items.IsNullOrEmpty()).ToList();

        var allItems = utilities.SelectMany(x => x.Items).ToList();
        var functionNames = allItems.Select(x => x.FunctionName).Distinct().ToList();
        var mapper = await GetAgentDocs(functionNames);

        allItems.ForEach(x =>
        {
            if (mapper.ContainsKey(x.FunctionName))
            {
                x.Description = mapper[x.FunctionName];
            }
        });

        return utilities;
    }

    #region Private methods
    private async ValueTask<IDictionary<string, string>> GetAgentDocs(IEnumerable<string> names)
    {
        var mapper = new Dictionary<string, string>();
        if (names.IsNullOrEmpty())
        {
            return mapper;
        }

        var dir = GetAgentDocDir(BuiltInAgentId.UtilityAssistant);
        if (string.IsNullOrEmpty(dir))
        {
            return mapper;
        }

        var matchDocs = Directory.GetFiles(dir, "*.md")
                                .Where(x => names.Contains(Path.GetFileNameWithoutExtension(x)))
                                .ToList();

        if (matchDocs.IsNullOrEmpty())
        {
            return mapper;
        }

        await foreach (var item in GetUtilityDescriptions(matchDocs))
        {
            mapper[item.Key] = item.Value;
        }

        return mapper;
    }

    private string GetAgentDocDir(string agentId)
    {
        var dbSettings = _services.GetRequiredService<BotSharpDatabaseSettings>();
        var agentSettings = _services.GetRequiredService<AgentSettings>();
        var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dbSettings.FileRepository, agentSettings.DataDir, agentId, "docs");
        if (!Directory.Exists(dir))
        {
            dir = string.Empty;
        }
        return dir;
    }

    private async IAsyncEnumerable<KeyValuePair<string, string>> GetUtilityDescriptions(IEnumerable<string> docs)
    {
        foreach (var doc in docs)
        {
            var content = string.Empty;
            try
            {
                content = await File.ReadAllTextAsync(doc);
            }
            catch { }

            if (string.IsNullOrWhiteSpace(content))
            {
                continue;
            }

            var fileName = Path.GetFileNameWithoutExtension(doc);
            yield return new KeyValuePair<string, string>(fileName, content);
        }
    }
    #endregion
}

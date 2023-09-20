using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Routing.Settings;
using System.IO;

namespace BotSharp.Core.Conversations.Services;

public class ConversationStorage : IConversationStorage
{
    private readonly BotSharpDatabaseSettings _dbSettings;
    private readonly AgentSettings _agentSettings;
    private readonly IServiceProvider _services;
    private readonly IUserIdentity _user;
    public ConversationStorage(
        BotSharpDatabaseSettings dbSettings,
        AgentSettings agentSettings,
        IServiceProvider services,
        IUserIdentity user)
    {
        _dbSettings = dbSettings;
        _agentSettings = agentSettings;
        _services = services;
        _user = user;
    }

    public void Append(string conversationId, RoleDialogModel dialog)
    {
        var agentId = dialog.CurrentAgentId;
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var dialogText = db.GetConversationDialog(conversationId);
        var sb = new StringBuilder(dialogText);

        if (dialog.Role == AgentRole.Function)
        {
            var args = dialog.FunctionArgs.Replace("\r", " ").Replace("\n", " ").Trim();

            sb.AppendLine($"{dialog.CreatedAt}|{dialog.Role}|{agentId}|{dialog.FunctionName}|{args}");

            var content = dialog.ExecutionResult.Replace("\r", " ").Replace("\n", " ").Trim();
            if (string.IsNullOrEmpty(content))
            {
                return;
            }
            sb.AppendLine($"  - {content}");
        }
        else
        {
            var routingSetting = _services.GetRequiredService<RoutingSettings>();
            var agentName = routingSetting.RouterId == agentId ? "Router" : db.Agents.First(x => x.Id == agentId).Name;

            sb.AppendLine($"{dialog.CreatedAt}|{dialog.Role}|{agentId}|{agentName}|");
            var content = dialog.Content.Replace("\r", " ").Replace("\n", " ").Trim();
            if (string.IsNullOrEmpty(content))
            {
                return;
            }
            sb.AppendLine($"  - {content}");
        }

        var updatedDialogs = sb.ToString();
        db.UpdateConversationDialog(conversationId, updatedDialogs);
    }

    public List<RoleDialogModel> GetDialogs(string conversationId)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var dialogText = db.GetConversationDialog(conversationId);
        var dialogs = dialogText.SplitByNewLine();

        var results = new List<RoleDialogModel>();
        for (int i = 0; i < dialogs.Length; i += 2)
        {
            var meta = dialogs[i];
            var dialog = dialogs[i + 1];
            var createdAt = DateTime.Parse(meta.Split('|')[0]);
            var role = meta.Split('|')[1];
            var currentAgentId = meta.Split('|')[2];
            var funcName = meta.Split('|')[3];
            var funcArgs= meta.Split('|')[4];
            var text = dialog.Substring(4);

            results.Add(new RoleDialogModel(role, text)
            {
                CurrentAgentId = currentAgentId,
                FunctionName = funcName,
                FunctionArgs = funcArgs,
                ExecutionResult = text,
                CreatedAt = createdAt
            });
        }
        return results;
    }

    public void InitStorage(string conversationId)
    {
        var file = GetStorageFile(conversationId);
        if (!File.Exists(file))
        {
            File.WriteAllLines(file, new string[0]);
        }
    }

    private string GetStorageFile(string conversationId)
    {
        var dir = Path.Combine(_dbSettings.FileRepository, "conversations", conversationId);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        return Path.Combine(dir, "dialogs.txt");
    }
}

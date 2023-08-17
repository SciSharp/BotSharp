using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Conversations.Models;
using System.IO;

namespace BotSharp.Core.Conversations.Services;

public class ConversationStorage : IConversationStorage
{
    private readonly MyDatabaseSettings _dbSettings;
    public ConversationStorage(MyDatabaseSettings dbSettings)
    {
        _dbSettings = dbSettings;
    }

    public void Append(string conversationId, RoleDialogModel dialog)
    {
        var conversationFile = GetStorageFile(conversationId);
        var sb = new StringBuilder();

        if (dialog.Role == AgentRole.Function)
        {
            var args = dialog.FunctionArgs.Replace("\r", " ").Replace("\n", " ").Trim();

            sb.AppendLine($"{dialog.CreatedAt}|{dialog.Role}|{dialog.CurrentAgentId}|{dialog.FunctionName}|{args}");

            var content = dialog.ExecutionResult.Replace("\r", " ").Replace("\n", " ").Trim();
            if (string.IsNullOrEmpty(content))
            {
                return;
            }
            sb.AppendLine($"  - {content}");
        }
        else if (dialog.Role == AgentRole.Assistant)
        {
            sb.AppendLine($"{dialog.CreatedAt}|{dialog.Role}|||");
            var content = dialog.Content.Replace("\r", " ").Replace("\n", " ").Trim();
            if (string.IsNullOrEmpty(content))
            {
                return;
            }
            sb.AppendLine($"  - {content}");
        }
        else
        {
            sb.AppendLine($"{dialog.CreatedAt}|{dialog.Role}|{dialog.CurrentAgentId}||");
            var content = dialog.Content.Replace("\r", " ").Replace("\n", " ").Trim();
            if (string.IsNullOrEmpty(content))
            {
                return;
            }
            sb.AppendLine($"  - {content}");
        }

        var conversation = sb.ToString();
        File.AppendAllText(conversationFile, conversation);
    }

    public List<RoleDialogModel> GetDialogs(string conversationId)
    {
        var conversationFile = GetStorageFile(conversationId);
        var dialogs = File.ReadAllLines(conversationFile);

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

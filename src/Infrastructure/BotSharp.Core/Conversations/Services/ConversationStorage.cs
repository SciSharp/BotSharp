using BotSharp.Abstraction.Repositories;
using System.IO;

namespace BotSharp.Core.Conversations.Services;

public class ConversationStorage : IConversationStorage
{
    private readonly BotSharpDatabaseSettings _dbSettings;
    private readonly IServiceProvider _services;
    public ConversationStorage(
        BotSharpDatabaseSettings dbSettings,
        IServiceProvider services)
    {
        _dbSettings = dbSettings;
        _services = services;
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

            sb.AppendLine($"{dialog.CreatedAt}|{dialog.Role}|{agentId}|{dialog.MessageId}");

            var content = dialog.Content;
            content = content.Replace("\r", " ").Replace("\n", " ").Trim();
            if (string.IsNullOrEmpty(content))
            {
                return;
            }
            sb.AppendLine($"  - {content}");
        }
        else
        {
            sb.AppendLine($"{dialog.CreatedAt}|{dialog.Role}|{agentId}|{dialog.MessageId}");
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
            var messageId = meta.Split('|')[3];
            var text = dialog.Substring(4);

            results.Add(new RoleDialogModel(role, text)
            {
                CurrentAgentId = currentAgentId,
                MessageId = messageId,
                Content = text,
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

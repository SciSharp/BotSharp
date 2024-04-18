using BotSharp.Abstraction.Messaging;
using BotSharp.Abstraction.Messaging.Models.RichContent;
using BotSharp.Abstraction.Options;
using System.IO;

namespace BotSharp.Core.Conversations.Services;

public class ConversationStorage : IConversationStorage
{
    private readonly BotSharpDatabaseSettings _dbSettings;
    private readonly BotSharpOptions _options;
    private readonly IServiceProvider _services;

    public ConversationStorage(
        BotSharpDatabaseSettings dbSettings,
        BotSharpOptions options,
        IServiceProvider services)
    {
        _dbSettings = dbSettings;
        _services = services;
        _options = options;
    }

    public void Append(string conversationId, RoleDialogModel dialog)
    {
        var agentId = dialog.CurrentAgentId;
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var dialogElements = new List<DialogElement>();

        // Prevent duplicate record to be inserted
        /*var dialogs = db.GetConversationDialogs(conversationId);
        if (dialogs.Any(x => x.MetaData.MessageId == dialog.MessageId && x.Content == dialog.Content))
        {
            return;
        }*/

        if (dialog.Role == AgentRole.Function)
        {
            var meta = new DialogMetaData
            {
                Role = dialog.Role,
                AgentId = agentId,
                MessageId = dialog.MessageId,
                FunctionName = dialog.FunctionName,
                CreateTime = dialog.CreatedAt
            }; 
            
            var content = dialog.Content.RemoveNewLine();
            if (string.IsNullOrEmpty(content))
            {
                return;
            }
            dialogElements.Add(new DialogElement(meta, content, dialog.SecondaryContent));
        }
        else
        {
            var meta = new DialogMetaData
            {
                Role = dialog.Role,
                AgentId = agentId,
                MessageId = dialog.MessageId,
                SenderId = dialog.SenderId,
                FunctionName = dialog.FunctionName,
                CreateTime = dialog.CreatedAt
            };
            
            var content = dialog.Content.RemoveNewLine();
            if (string.IsNullOrEmpty(content))
            {
                return;
            }

            var richContent = dialog.RichContent != null ? JsonSerializer.Serialize(dialog.RichContent, _options.JsonSerializerOptions) : null;
            var secondaryRichContent = dialog.SecondaryRichContent != null ? JsonSerializer.Serialize(dialog.SecondaryRichContent, _options.JsonSerializerOptions) : null;
            dialogElements.Add(new DialogElement(meta, content, richContent, dialog.SecondaryContent, secondaryRichContent));
        }

        db.AppendConversationDialogs(conversationId, dialogElements);
    }

    public List<RoleDialogModel> GetDialogs(string conversationId)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var dialogs = db.GetConversationDialogs(conversationId);
        var hooks = _services.GetServices<IConversationHook>();

        var results = new List<RoleDialogModel>();
        foreach (var dialog in dialogs)
        {
            var meta = dialog.MetaData;
            var content = dialog.Content;
            var secondaryContent = dialog.SecondaryContent;
            var role = meta.Role;
            var currentAgentId = meta.AgentId;
            var messageId = meta.MessageId;
            var function = meta.FunctionName;
            var senderId = role == AgentRole.Function ? currentAgentId : meta.SenderId;
            var createdAt = meta.CreateTime;
            var richContent = !string.IsNullOrEmpty(dialog.RichContent) ? 
                                JsonSerializer.Deserialize<RichContent<IRichMessage>>(dialog.RichContent, _options.JsonSerializerOptions) : null;
            var secondaryRichContent = !string.IsNullOrEmpty(dialog.SecondaryRichContent) ?
                                JsonSerializer.Deserialize<RichContent<IRichMessage>>(dialog.SecondaryRichContent, _options.JsonSerializerOptions) : null;

            var record = new RoleDialogModel(role, content)
            {
                CurrentAgentId = currentAgentId,
                MessageId = messageId,
                CreatedAt = createdAt,
                SenderId = senderId,
                FunctionName = function,
                RichContent = richContent,
                SecondaryContent = secondaryContent,
                SecondaryRichContent = secondaryRichContent,
            };
            results.Add(record);

            foreach(var hook in hooks)
            {
                hook.OnDialogRecordLoaded(record).Wait();
            }
        }

        foreach (var hook in hooks)
        {
            hook.OnDialogsLoaded(results).Wait();
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

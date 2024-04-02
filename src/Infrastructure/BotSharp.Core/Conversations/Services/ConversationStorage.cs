using BotSharp.Abstraction.Messaging;
using BotSharp.Abstraction.Messaging.Models.RichContent;
using BotSharp.Abstraction.Options;
using System.IO;

namespace BotSharp.Core.Conversations.Services;

public class ConversationStorage : IConversationStorage
{
    private readonly BotSharpDatabaseSettings _dbSettings;
    private readonly IServiceProvider _services;
    private readonly JsonSerializerOptions _jsonOptions;

    public ConversationStorage(
        BotSharpDatabaseSettings dbSettings,
        BotSharpOptions options,
        IServiceProvider services)
    {
        _dbSettings = dbSettings;
        _services = services;
        _jsonOptions = InitJsonSerilizerOptions(options);
    }

    public void Append(string conversationId, RoleDialogModel dialog)
    {
        var agentId = dialog.CurrentAgentId;
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var dialogElements = new List<DialogElement>();

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
            dialogElements.Add(new DialogElement(meta, content));
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

            var richContent = dialog.RichContent != null ? JsonSerializer.Serialize(dialog.RichContent, _jsonOptions) : null;
            dialogElements.Add(new DialogElement(meta, content, richContent));
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
            var role = meta.Role;
            var currentAgentId = meta.AgentId;
            var messageId = meta.MessageId;
            var function = meta.FunctionName;
            var senderId = role == AgentRole.Function ? currentAgentId : meta.SenderId;
            var createdAt = meta.CreateTime;
            var richContent = !string.IsNullOrEmpty(dialog.RichContent) ? 
                                JsonSerializer.Deserialize<RichContent<IRichMessage>>(dialog.RichContent, _jsonOptions) : null;

            var record = new RoleDialogModel(role, content)
            {
                CurrentAgentId = currentAgentId,
                MessageId = messageId,
                CreatedAt = createdAt,
                SenderId = senderId,
                FunctionName = function,
                RichContent = richContent
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

    private JsonSerializerOptions InitJsonSerilizerOptions(BotSharpOptions botSharOptions)
    {
        var options = botSharOptions.JsonSerializerOptions;
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = options.PropertyNameCaseInsensitive,
            PropertyNamingPolicy = options.PropertyNamingPolicy ?? JsonNamingPolicy.CamelCase,
            AllowTrailingCommas = options.AllowTrailingCommas,
        };

        foreach (var converter in options.Converters)
        {
            jsonOptions.Converters.Add(converter);
        }
        return jsonOptions;
    }
}

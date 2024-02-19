using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Repositories.Filters;
using BotSharp.Abstraction.Repositories.Models;
using BotSharp.Plugin.MongoStorage.Collections;
using BotSharp.Plugin.MongoStorage.Models;

namespace BotSharp.Plugin.MongoStorage.Repository;

public partial class MongoRepository
{
    public void CreateNewConversation(Conversation conversation)
    {
        if (conversation == null) return;

        var convDoc = new ConversationDocument
        {
            Id = !string.IsNullOrEmpty(conversation.Id) ? conversation.Id : Guid.NewGuid().ToString(),
            AgentId = conversation.AgentId,
            UserId = !string.IsNullOrEmpty(conversation.UserId) ? conversation.UserId : string.Empty,
            Title = conversation.Title,
            Channel = conversation.Channel,
            TaskId = conversation.TaskId,
            Status = conversation.Status,
            CreatedTime = DateTime.UtcNow,
            UpdatedTime = DateTime.UtcNow,
        };

        var dialogDoc = new ConversationDialogDocument
        {
            Id = Guid.NewGuid().ToString(),
            ConversationId = convDoc.Id,
            Dialogs = new List<DialogMongoElement>()
        };

        var states = conversation.States ?? new Dictionary<string, string>();
        var initialStates = states.Select(x => new StateMongoElement
        {
            Key = x.Key,
            Values = new List<StateValueMongoElement>
        {
            new StateValueMongoElement { Data = x.Value, UpdateTime = DateTime.UtcNow }
        }
        }).ToList();

        var stateDoc = new ConversationStateDocument
        {
            Id = Guid.NewGuid().ToString(),
            ConversationId = convDoc.Id,
            States = initialStates
        };

        _dc.Conversations.InsertOne(convDoc);
        _dc.ConversationDialogs.InsertOne(dialogDoc);
        _dc.ConversationStates.InsertOne(stateDoc);
    }

    public bool DeleteConversation(string conversationId)
    {
        if (string.IsNullOrEmpty(conversationId)) return false;

        var filterConv = Builders<ConversationDocument>.Filter.Eq(x => x.Id, conversationId);
        var filterDialog = Builders<ConversationDialogDocument>.Filter.Eq(x => x.ConversationId, conversationId);
        var filterSates = Builders<ConversationStateDocument>.Filter.Eq(x => x.ConversationId, conversationId);
        var filterExeLog = Builders<ExecutionLogDocument>.Filter.Eq(x => x.ConversationId, conversationId);
        var filterPromptLog = Builders<LlmCompletionLogDocument>.Filter.Eq(x => x.ConversationId, conversationId);
        var filterContentLog = Builders<ConversationContentLogDocument>.Filter.Eq(x => x.ConversationId, conversationId);
        var filterStateLog = Builders<ConversationStateLogDocument>.Filter.Eq(x => x.ConversationId, conversationId);

        var exeLogDeleted = _dc.ExectionLogs.DeleteMany(filterExeLog);
        var promptLogDeleted = _dc.LlmCompletionLogs.DeleteMany(filterPromptLog);
        var contentLogDeleted = _dc.ContentLogs.DeleteMany(filterContentLog);
        var stateLogDeleted = _dc.StateLogs.DeleteMany(filterStateLog);
        var statesDeleted = _dc.ConversationStates.DeleteMany(filterSates);
        var dialogDeleted = _dc.ConversationDialogs.DeleteMany(filterDialog);
        var convDeleted = _dc.Conversations.DeleteMany(filterConv);
        
        return convDeleted.DeletedCount > 0 || dialogDeleted.DeletedCount > 0 || statesDeleted.DeletedCount > 0
            || exeLogDeleted.DeletedCount > 0 || promptLogDeleted.DeletedCount > 0
            || contentLogDeleted.DeletedCount > 0 || stateLogDeleted.DeletedCount > 0;
    }

    public List<DialogElement> GetConversationDialogs(string conversationId)
    {
        var dialogs = new List<DialogElement>();
        if (string.IsNullOrEmpty(conversationId)) return dialogs;

        var filter = Builders<ConversationDialogDocument>.Filter.Eq(x => x.ConversationId, conversationId);
        var foundDialog = _dc.ConversationDialogs.Find(filter).FirstOrDefault();
        if (foundDialog == null) return dialogs;

        var formattedDialog = foundDialog.Dialogs?.Select(x => DialogMongoElement.ToDomainElement(x))?.ToList();
        return formattedDialog ?? new List<DialogElement>();
    }

    public void UpdateConversationDialogElements(string conversationId, List<DialogContentUpdateModel> updateElements)
    {
        if (string.IsNullOrEmpty(conversationId) || updateElements.IsNullOrEmpty()) return;

        var filterDialog = Builders<ConversationDialogDocument>.Filter.Eq(x => x.ConversationId, conversationId);
        var foundDialog = _dc.ConversationDialogs.Find(filterDialog).FirstOrDefault();
        if (foundDialog == null || foundDialog.Dialogs.IsNullOrEmpty()) return;

        foundDialog.Dialogs = foundDialog.Dialogs.Select((x, idx) =>
        {
            var found = updateElements.FirstOrDefault(e => e.Index == idx);
            if (found != null)
            {
                x.Content = found.UpdateContent;
            }
            return x;
        }).ToList();

        _dc.ConversationDialogs.ReplaceOne(filterDialog, foundDialog);
    }

    public void AppendConversationDialogs(string conversationId, List<DialogElement> dialogs)
    {
        if (string.IsNullOrEmpty(conversationId)) return;

        var filterConv = Builders<ConversationDocument>.Filter.Eq(x => x.Id, conversationId);
        var filterDialog = Builders<ConversationDialogDocument>.Filter.Eq(x => x.ConversationId, conversationId);
        var dialogElements = dialogs.Select(x => DialogMongoElement.ToMongoElement(x)).ToList();
        var updateDialog = Builders<ConversationDialogDocument>.Update.PushEach(x => x.Dialogs, dialogElements);
        var updateConv = Builders<ConversationDocument>.Update.Set(x => x.UpdatedTime, DateTime.UtcNow);

        _dc.ConversationDialogs.UpdateOne(filterDialog, updateDialog);
        _dc.Conversations.UpdateOne(filterConv, updateConv);
    }

    public void UpdateConversationTitle(string conversationId, string title)
    {
        if (string.IsNullOrEmpty(conversationId)) return;

        var filterConv = Builders<ConversationDocument>.Filter.Eq(x => x.Id, conversationId);
        var updateConv = Builders<ConversationDocument>.Update
            .Set(x => x.UpdatedTime, DateTime.UtcNow)
            .Set(x => x.Title, title);

        _dc.Conversations.UpdateOne(filterConv, updateConv);
    }

    public ConversationState GetConversationStates(string conversationId)
    {
        var states = new ConversationState();
        if (string.IsNullOrEmpty(conversationId)) return states;

        var filter = Builders<ConversationStateDocument>.Filter.Eq(x => x.ConversationId, conversationId);
        var foundStates = _dc.ConversationStates.Find(filter).FirstOrDefault();
        if (foundStates == null || foundStates.States.IsNullOrEmpty()) return states;

        var savedStates = foundStates.States.Select(x => StateMongoElement.ToDomainElement(x)).ToList();
        return new ConversationState(savedStates);
    }

    public void UpdateConversationStates(string conversationId, List<StateKeyValue> states)
    {
        if (string.IsNullOrEmpty(conversationId) || states.IsNullOrEmpty()) return;

        var filterStates = Builders<ConversationStateDocument>.Filter.Eq(x => x.ConversationId, conversationId);
        var saveStates = states.Select(x => StateMongoElement.ToMongoElement(x)).ToList();
        var updateStates = Builders<ConversationStateDocument>.Update.Set(x => x.States, saveStates);

        _dc.ConversationStates.UpdateOne(filterStates, updateStates);
    }

    public void UpdateConversationStatus(string conversationId, string status)
    {
        if (string.IsNullOrEmpty(conversationId) || string.IsNullOrEmpty(status)) return;

        var filter = Builders<ConversationDocument>.Filter.Eq(x => x.Id, conversationId);
        var update = Builders<ConversationDocument>.Update
            .Set(x => x.Status, status)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);

        _dc.Conversations.UpdateOne(filter, update);
    }

    public Conversation GetConversation(string conversationId)
    {
        if (string.IsNullOrEmpty(conversationId)) return null;

        var filterConv = Builders<ConversationDocument>.Filter.Eq(x => x.Id, conversationId);
        var filterDialog = Builders<ConversationDialogDocument>.Filter.Eq(x => x.ConversationId, conversationId);
        var filterState = Builders<ConversationStateDocument>.Filter.Eq(x => x.ConversationId, conversationId);

        var conv = _dc.Conversations.Find(filterConv).FirstOrDefault();
        var dialog = _dc.ConversationDialogs.Find(filterDialog).FirstOrDefault();
        var states = _dc.ConversationStates.Find(filterState).FirstOrDefault();

        if (conv == null) return null;

        var dialogElements = dialog?.Dialogs?.Select(x => DialogMongoElement.ToDomainElement(x))?.ToList() ?? new List<DialogElement>();
        var curStates = new Dictionary<string, string>();
        states.States.ForEach(x =>
        {
            curStates[x.Key] = x.Values?.LastOrDefault()?.Data ?? string.Empty;
        });

        return new Conversation
        {
            Id = conv.Id.ToString(),
            AgentId = conv.AgentId.ToString(),
            UserId = conv.UserId.ToString(),
            Title = conv.Title,
            Channel = conv.Channel,
            Status = conv.Status,
            Dialogs = dialogElements,
            States = curStates,
            CreatedTime = conv.CreatedTime,
            UpdatedTime = conv.UpdatedTime
        };
    }

    public PagedItems<Conversation> GetConversations(ConversationFilter filter)
    {
        var conversations = new List<Conversation>();
        var builder = Builders<ConversationDocument>.Filter;
        var filters = new List<FilterDefinition<ConversationDocument>>() { builder.Empty };

        if (!string.IsNullOrEmpty(filter.Id)) filters.Add(builder.Eq(x => x.Id, filter.Id));
        if (!string.IsNullOrEmpty(filter.AgentId)) filters.Add(builder.Eq(x => x.AgentId, filter.AgentId));
        if (!string.IsNullOrEmpty(filter.Status)) filters.Add(builder.Eq(x => x.Status, filter.Status));
        if (!string.IsNullOrEmpty(filter.Channel)) filters.Add(builder.Eq(x => x.Channel, filter.Channel));
        if (!string.IsNullOrEmpty(filter.UserId)) filters.Add(builder.Eq(x => x.UserId, filter.UserId));
        if (!string.IsNullOrEmpty(filter.TaskId)) filters.Add(builder.Eq(x => x.TaskId, filter.TaskId));

        var filterDef = builder.And(filters);
        var sortDef = Builders<ConversationDocument>.Sort.Descending(x => x.CreatedTime);
        var pager = filter?.Pager ?? new Pagination();
        var conversationDocs = _dc.Conversations.Find(filterDef).Sort(sortDef).Skip(pager.Offset).Limit(pager.Size).ToList();
        var count = _dc.Conversations.CountDocuments(filterDef);

        foreach (var conv in conversationDocs)
        {
            var convId = conv.Id.ToString();
            conversations.Add(new Conversation
            {
                Id = convId,
                AgentId = conv.AgentId.ToString(),
                UserId = conv.UserId.ToString(),
                TaskId = conv.TaskId,
                Title = conv.Title,
                Channel = conv.Channel,
                Status = conv.Status,
                CreatedTime = conv.CreatedTime,
                UpdatedTime = conv.UpdatedTime
            });
        }

        return new PagedItems<Conversation>
        {
            Items = conversations,
            Count = (int)count
        };
    }

    public List<Conversation> GetLastConversations()
    {
        var records = new List<Conversation>();
        var conversations = _dc.Conversations.Aggregate()
                                             .Group(c => c.UserId, g => g.OrderByDescending(x => x.CreatedTime).First())
                                             .ToList();
        return conversations.Select(c => new Conversation()
        {
            Id = c.Id.ToString(),
            AgentId = c.AgentId.ToString(),
            UserId = c.UserId.ToString(),
            Title = c.Title,
            Channel = c.Channel,
            Status = c.Status,
            CreatedTime = c.CreatedTime,
            UpdatedTime = c.UpdatedTime
        }).ToList();
    }

    public bool TruncateConversation(string conversationId, string messageId)
    {
        if (string.IsNullOrEmpty(conversationId) || string.IsNullOrEmpty(messageId)) return false;

        var dialogFilter = Builders<ConversationDialogDocument>.Filter.Eq(x => x.ConversationId, conversationId);
        var foundDialog = _dc.ConversationDialogs.Find(dialogFilter).FirstOrDefault();
        if (foundDialog == null || foundDialog.Dialogs.IsNullOrEmpty()) return false;

        var foundIdx = foundDialog.Dialogs.FindIndex(x => x.MetaData?.MessageId == messageId);
        if (foundIdx < 0) return false;

        // Handle truncated dialogs
        var truncatedDialogs = foundDialog.Dialogs.Where((x, idx) => idx < foundIdx).ToList();
        
        // Handle truncated states
        var refTime = foundDialog.Dialogs.ElementAt(foundIdx).MetaData.CreateTime;
        var stateFilter = Builders<ConversationStateDocument>.Filter.Eq(x => x.ConversationId, conversationId);
        var foundStates = _dc.ConversationStates.Find(stateFilter).FirstOrDefault();
        if (foundStates == null || foundStates.States.IsNullOrEmpty()) return false;

        var truncatedStates = new List<StateMongoElement>();
        foreach (var state in foundStates.States)
        {
            var values = state.Values.Where(x => x.UpdateTime < refTime).ToList();
            if (values.Count == 0) continue;

            state.Values = values;
            truncatedStates.Add(state);
        }

        // Save
        foundDialog.Dialogs = truncatedDialogs;
        foundStates.States = truncatedStates;
        _dc.ConversationDialogs.ReplaceOne(dialogFilter, foundDialog);
        _dc.ConversationStates.ReplaceOne(stateFilter, foundStates);
        return true;
    }
}

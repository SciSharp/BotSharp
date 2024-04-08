using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Repositories.Filters;
using BotSharp.Abstraction.Repositories.Models;
using BotSharp.Plugin.MongoStorage.Collections;
using BotSharp.Plugin.MongoStorage.Models;
using System.Text.RegularExpressions;

namespace BotSharp.Plugin.MongoStorage.Repository;

public partial class MongoRepository
{
    public void CreateNewConversation(Conversation conversation)
    {
        if (conversation == null) return;

        var utcNow = DateTime.UtcNow;
        var convDoc = new ConversationDocument
        {
            Id = !string.IsNullOrEmpty(conversation.Id) ? conversation.Id : Guid.NewGuid().ToString(),
            AgentId = conversation.AgentId,
            UserId = !string.IsNullOrEmpty(conversation.UserId) ? conversation.UserId : string.Empty,
            Title = conversation.Title,
            Channel = conversation.Channel,
            TaskId = conversation.TaskId,
            Status = conversation.Status,
            CreatedTime = utcNow,
            UpdatedTime = utcNow
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
            States = initialStates,
            Breakpoints = new List<BreakpointMongoElement>()
        };

        _dc.Conversations.InsertOne(convDoc);
        _dc.ConversationDialogs.InsertOne(dialogDoc);
        _dc.ConversationStates.InsertOne(stateDoc);
    }

    public bool DeleteConversations(IEnumerable<string> conversationIds)
    {
        if (conversationIds.IsNullOrEmpty()) return false;

        var filterConv = Builders<ConversationDocument>.Filter.In(x => x.Id, conversationIds);
        var filterDialog = Builders<ConversationDialogDocument>.Filter.In(x => x.ConversationId, conversationIds);
        var filterSates = Builders<ConversationStateDocument>.Filter.In(x => x.ConversationId, conversationIds);
        var filterExeLog = Builders<ExecutionLogDocument>.Filter.In(x => x.ConversationId, conversationIds);
        var filterPromptLog = Builders<LlmCompletionLogDocument>.Filter.In(x => x.ConversationId, conversationIds);
        var filterContentLog = Builders<ConversationContentLogDocument>.Filter.In(x => x.ConversationId, conversationIds);
        var filterStateLog = Builders<ConversationStateLogDocument>.Filter.In(x => x.ConversationId, conversationIds);

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
        var updateConv = Builders<ConversationDocument>.Update.Set(x => x.UpdatedTime, DateTime.UtcNow)
                                                              .Inc(x => x.DialogCount, dialogs.Count);

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

    public void UpdateConversationBreakpoint(string conversationId, ConversationBreakpoint breakpoint)
    {
        if (string.IsNullOrEmpty(conversationId)) return;

        var newBreakpoint = new BreakpointMongoElement()
        {
            MessageId = breakpoint.MessageId,
            Breakpoint = breakpoint.Breakpoint,
            CreatedTime = DateTime.UtcNow,
            Reason = breakpoint.Reason
        };
        var filterState = Builders<ConversationStateDocument>.Filter.Eq(x => x.ConversationId, conversationId);
        var updateState = Builders<ConversationStateDocument>.Update.Push(x => x.Breakpoints, newBreakpoint);

        _dc.ConversationStates.UpdateOne(filterState, updateState);
    }

    public ConversationBreakpoint? GetConversationBreakpoint(string conversationId)
    {
        if (string.IsNullOrEmpty(conversationId))
        {
            return null;
        }

        var filter = Builders<ConversationStateDocument>.Filter.Eq(x => x.ConversationId, conversationId);
        var state = _dc.ConversationStates.Find(filter).FirstOrDefault();

        if (state == null || state.Breakpoints.IsNullOrEmpty())
        {
            return null;
        }

        return state.Breakpoints.Select(x => new ConversationBreakpoint
        {
            Breakpoint = x.Breakpoint,
            CreatedTime = x.CreatedTime,
            MessageId = x.MessageId,
            Reason = x.Reason,
        }).LastOrDefault();
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
        if (string.IsNullOrEmpty(conversationId) || states == null) return;

        var filterConv = Builders<ConversationDocument>.Filter.Eq(x => x.Id, conversationId);
        var filterStates = Builders<ConversationStateDocument>.Filter.Eq(x => x.ConversationId, conversationId);
        var saveStates = states.Select(x => StateMongoElement.ToMongoElement(x)).ToList();
        var updateStates = Builders<ConversationStateDocument>.Update.Set(x => x.States, saveStates);
        var updateConv = Builders<ConversationDocument>.Update.Set(x => x.UpdatedTime, DateTime.UtcNow);

        _dc.ConversationStates.UpdateOne(filterStates, updateStates);
        _dc.Conversations.UpdateOne(filterConv, updateConv);
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
            DialogCount = conv.DialogCount,
            CreatedTime = conv.CreatedTime,
            UpdatedTime = conv.UpdatedTime
        };
    }

    public PagedItems<Conversation> GetConversations(ConversationFilter filter)
    {
        var conversations = new List<Conversation>();
        var builder = Builders<ConversationDocument>.Filter;
        var filters = new List<FilterDefinition<ConversationDocument>>() { builder.Empty };

        if (!string.IsNullOrEmpty(filter?.Id))
        {
            filters.Add(builder.Eq(x => x.Id, filter.Id));
        }
        if (!string.IsNullOrEmpty(filter?.AgentId))
        {
            filters.Add(builder.Eq(x => x.AgentId, filter.AgentId));
        }
        if (!string.IsNullOrEmpty(filter?.Status))
        {
            filters.Add(builder.Eq(x => x.Status, filter.Status));
        }
        if (!string.IsNullOrEmpty(filter?.Channel))
        {
            filters.Add(builder.Eq(x => x.Channel, filter.Channel));
        }
        if (!string.IsNullOrEmpty(filter?.UserId))
        {
            filters.Add(builder.Eq(x => x.UserId, filter.UserId));
        }
        if (!string.IsNullOrEmpty(filter?.TaskId))
        {
            filters.Add(builder.Eq(x => x.TaskId, filter.TaskId));
        }
        if (filter?.StartTime != null)
        {
            filters.Add(builder.Gte(x => x.CreatedTime, filter.StartTime.Value));
        }

        // Check states
        if (filter != null && !filter.States.IsNullOrEmpty())
        {
            var targetConvIds = new List<string>();

            foreach (var pair in filter.States)
            {
                if (pair == null || string.IsNullOrWhiteSpace(pair.Key)) continue;

                var query = _dc.ConversationStates.AsQueryable();
                var convIds = query.AsEnumerable().Where(x => 
                {
                    var foundState = x.States.FirstOrDefault(s => s.Key.IsEqualTo(pair.Key));
                    if (foundState == null) return false;

                    if (!string.IsNullOrWhiteSpace(pair.Value))
                    {
                        return pair.Value.IsEqualTo(foundState.Values.LastOrDefault()?.Data);
                    }

                    return true;
                }).Select(x => x.ConversationId).ToList();

                targetConvIds = targetConvIds.Concat(convIds).Distinct().ToList();
            }

            filters.Add(builder.In(x => x.Id, targetConvIds));
        }

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
                DialogCount = conv.DialogCount,
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
                                             .Group(c => c.UserId, g => g.First(x => x.CreatedTime == g.Select(y => y.CreatedTime).Max()))
                                             .ToList();
        return conversations.Select(c => new Conversation()
        {
            Id = c.Id.ToString(),
            AgentId = c.AgentId.ToString(),
            UserId = c.UserId.ToString(),
            Title = c.Title,
            Channel = c.Channel,
            Status = c.Status,
            DialogCount = c.DialogCount,
            CreatedTime = c.CreatedTime,
            UpdatedTime = c.UpdatedTime
        }).ToList();
    }

    public List<string> GetIdleConversations(int batchSize, int messageLimit, int bufferHours)
    {
        var page = 1;
        var batchLimit = 100;
        var utcNow = DateTime.UtcNow;
        var conversationIds = new List<string>();

        if (batchSize <= 0 || batchSize > batchLimit)
        {
            batchSize = batchLimit;
        }

        if (bufferHours <= 0)
        {
            bufferHours = 12;
        }

        if (messageLimit <= 0)
        {
            messageLimit = 2;
        }

        while (true)
        {
            var skip = (page - 1) * batchSize;
            var candidates = _dc.Conversations.AsQueryable()
                                              .Where(x => (x.DialogCount <= messageLimit) && x.UpdatedTime <= utcNow.AddHours(-bufferHours))
                                              .Skip(skip)
                                              .Take(batchSize)
                                              .Select(x => x.Id)
                                              .ToList();

            if (candidates.IsNullOrEmpty())
            {
                break;
            }
            
            conversationIds = conversationIds.Concat(candidates).Distinct().ToList();
            if (conversationIds.Count >= batchSize)
            {
                break;
            }

            page++;
        }

        return conversationIds.Take(batchSize).ToList();
    }

    public bool TruncateConversation(string conversationId, string messageId, bool cleanLog = false)
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

        if (foundStates != null)
        {
            // Truncate states
            if (!foundStates.States.IsNullOrEmpty())
            {
                var truncatedStates = new List<StateMongoElement>();
                foreach (var state in foundStates.States)
                {
                    if (!state.Versioning)
                    {
                        truncatedStates.Add(state);
                        continue;
                    }

                    var values = state.Values.Where(x => x.MessageId != messageId)
                                             .Where(x => x.UpdateTime < refTime)
                                             .ToList();
                    if (values.Count == 0) continue;

                    state.Values = values;
                    truncatedStates.Add(state);
                }
                foundStates.States = truncatedStates;
            }

            // Truncate breakpoints
            if (!foundStates.Breakpoints.IsNullOrEmpty())
            {
                var breakpoints = foundStates.Breakpoints ?? new List<BreakpointMongoElement>();
                var targetIdx = breakpoints.FindIndex(x => x.MessageId == messageId);
                var truncatedBreakpoints = breakpoints.Where((x, idx) => idx < targetIdx).ToList();
                foundStates.Breakpoints = truncatedBreakpoints;
            }
            
            // Update
            _dc.ConversationStates.ReplaceOne(stateFilter, foundStates);
        }

        // Save dialogs
        foundDialog.Dialogs = truncatedDialogs;
        _dc.ConversationDialogs.ReplaceOne(dialogFilter, foundDialog);

        // Update conversation
        var convFilter = Builders<ConversationDocument>.Filter.Eq(x => x.Id, conversationId);
        var updateConv = Builders<ConversationDocument>.Update.Set(x => x.UpdatedTime, DateTime.UtcNow)
                                                              .Set(x => x.DialogCount, truncatedDialogs.Count);
        _dc.Conversations.UpdateOne(convFilter, updateConv);

        // Remove logs
        if (cleanLog)
        {
            var contentLogBuilder = Builders<ConversationContentLogDocument>.Filter;
            var stateLogBuilder = Builders<ConversationStateLogDocument>.Filter;

            var contentLogFilters = new List<FilterDefinition<ConversationContentLogDocument>>()
            {
                contentLogBuilder.Eq(x => x.ConversationId, conversationId),
                contentLogBuilder.Gte(x => x.CreateTime, refTime)
            };
            var stateLogFilters = new List<FilterDefinition<ConversationStateLogDocument>>()
            {
                stateLogBuilder.Eq(x => x.ConversationId, conversationId),
                stateLogBuilder.Gte(x => x.CreateTime, refTime)
            };

            _dc.ContentLogs.DeleteMany(contentLogBuilder.And(contentLogFilters));
            _dc.StateLogs.DeleteMany(stateLogBuilder.And(stateLogFilters));
        }
        
        return true;
    }
}

using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Repositories.Filters;
using Microsoft.Extensions.Logging;
using MongoDB.Driver.Linq;
using System.Text.Json;

namespace BotSharp.Plugin.MongoStorage.Repository;

public partial class MongoRepository
{
    public async Task CreateNewConversation(Conversation conversation)
    {
        if (conversation == null) return;

        var utcNow = DateTime.UtcNow;
        var userId = !string.IsNullOrEmpty(conversation.UserId) ? conversation.UserId : string.Empty;
        var convDoc = new ConversationDocument
        {
            Id = !string.IsNullOrEmpty(conversation.Id) ? conversation.Id : Guid.NewGuid().ToString(),
            AgentId = conversation.AgentId,
            UserId = userId,
            Title = conversation.Title,
            Channel = conversation.Channel,
            ChannelId = conversation.ChannelId,
            TaskId = conversation.TaskId,
            Status = conversation.Status,
            Tags = conversation.Tags ?? new(),
            CreatedTime = utcNow,
            UpdatedTime = utcNow,
            LatestStates = []
        };

        var dialogDoc = new ConversationDialogDocument
        {
            Id = Guid.NewGuid().ToString(),
            ConversationId = convDoc.Id,
            AgentId = conversation.AgentId,
            UserId = userId,
            Dialogs = [],
            UpdatedTime = utcNow
        };

        var stateDoc = new ConversationStateDocument
        {
            Id = Guid.NewGuid().ToString(),
            ConversationId = convDoc.Id,
            AgentId = conversation.AgentId,
            UserId = userId,
            States = [],
            Breakpoints = [],
            UpdatedTime = utcNow
        };

        try
        {
            await _dc.Conversations.InsertOneAsync(convDoc);
            await _dc.ConversationDialogs.InsertOneAsync(dialogDoc);
            await _dc.ConversationStates.InsertOneAsync(stateDoc);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Code == 11000)
        {
            //11000  duplicate key error
            return;
        }
    }

    public async Task<bool> DeleteConversations(IEnumerable<string> conversationIds)
    {
        if (conversationIds.IsNullOrEmpty()) return false;

        var filterConv = Builders<ConversationDocument>.Filter.In(x => x.Id, conversationIds);
        var filterDialog = Builders<ConversationDialogDocument>.Filter.In(x => x.ConversationId, conversationIds);
        var filterSates = Builders<ConversationStateDocument>.Filter.In(x => x.ConversationId, conversationIds);
        var filterPromptLog = Builders<LlmCompletionLogDocument>.Filter.In(x => x.ConversationId, conversationIds);
        var filterContentLog = Builders<ConversationContentLogDocument>.Filter.In(x => x.ConversationId, conversationIds);
        var filterStateLog = Builders<ConversationStateLogDocument>.Filter.In(x => x.ConversationId, conversationIds);
        var conbTabItems = Builders<CrontabItemDocument>.Filter.In(x => x.ConversationId, conversationIds);
        //var filterConvFile = Builders<ConversationFileDocument>.Filter.In(x => x.ConversationId, conversationIds);

        var promptLogDeleted = await _dc.LlmCompletionLogs.DeleteManyAsync(filterPromptLog);
        var contentLogDeleted = await _dc.ContentLogs.DeleteManyAsync(filterContentLog);
        var stateLogDeleted = await _dc.StateLogs.DeleteManyAsync(filterStateLog);
        var statesDeleted = await _dc.ConversationStates.DeleteManyAsync(filterSates);
        var dialogDeleted = await _dc.ConversationDialogs.DeleteManyAsync(filterDialog);
        var cronDeleted = await _dc.CrontabItems.DeleteManyAsync(conbTabItems);
        //var fileDeleted = await _dc.ConversationFiles.DeleteManyAsync(filterConvFile);
        var convDeleted = await _dc.Conversations.DeleteManyAsync(filterConv);

        return convDeleted.DeletedCount > 0 || dialogDeleted.DeletedCount > 0 || statesDeleted.DeletedCount > 0
            || promptLogDeleted.DeletedCount > 0 || contentLogDeleted.DeletedCount > 0
            || stateLogDeleted.DeletedCount > 0
            || convDeleted.DeletedCount > 0;
    }

    [SideCar]
    public async Task<List<DialogElement>> GetConversationDialogs(string conversationId, ConversationDialogFilter? filter = null)
    {
        var dialogs = new List<DialogElement>();
        if (string.IsNullOrEmpty(conversationId))
        {
            return dialogs;
        }

        var dialogFilter = Builders<ConversationDialogDocument>.Filter.Eq(x => x.ConversationId, conversationId);
        var foundDialog = await _dc.ConversationDialogs.Find(dialogFilter).FirstOrDefaultAsync();
        if (foundDialog == null)
        {
            return dialogs;
        }

        var formattedDialog = foundDialog.Dialogs?.Select(x => DialogMongoElement.ToDomainElement(x))?.ToList() ?? [];
        if (filter?.Order == "desc")
        {
            formattedDialog = formattedDialog.OrderByDescending(x => x.MetaData?.CreatedTime).ToList();
        }
        return formattedDialog ?? [];
    }

    [SideCar]
    public async Task AppendConversationDialogs(string conversationId, List<DialogElement> dialogs)
    {
        if (string.IsNullOrEmpty(conversationId)) return;

        var filterConv = Builders<ConversationDocument>.Filter.Eq(x => x.Id, conversationId);
        var filterDialog = Builders<ConversationDialogDocument>.Filter.Eq(x => x.ConversationId, conversationId);
        var dialogElements = dialogs.Select(x => DialogMongoElement.ToMongoElement(x)).ToList();
        var updateDialog = Builders<ConversationDialogDocument>.Update.PushEach(x => x.Dialogs, dialogElements)
                                                                      .Set(x => x.UpdatedTime, DateTime.UtcNow);
        var updateConv = Builders<ConversationDocument>.Update.Set(x => x.UpdatedTime, DateTime.UtcNow)
                                                              .Inc(x => x.DialogCount, dialogs.Count);

        await _dc.ConversationDialogs.UpdateOneAsync(filterDialog, updateDialog);
        await _dc.Conversations.UpdateOneAsync(filterConv, updateConv);
    }

    public async Task UpdateConversationTitle(string conversationId, string title)
    {
        if (string.IsNullOrEmpty(conversationId)) return;

        var filterConv = Builders<ConversationDocument>.Filter.Eq(x => x.Id, conversationId);
        var updateConv = Builders<ConversationDocument>.Update
            .Set(x => x.UpdatedTime, DateTime.UtcNow)
            .Set(x => x.Title, title);

        await _dc.Conversations.UpdateOneAsync(filterConv, updateConv);
    }
    public async Task UpdateConversationTitleAlias(string conversationId, string titleAlias)
    {
        if (string.IsNullOrEmpty(conversationId)) return;

        var filterConv = Builders<ConversationDocument>.Filter.Eq(x => x.Id, conversationId);
        var updateConv = Builders<ConversationDocument>.Update
            .Set(x => x.UpdatedTime, DateTime.UtcNow)
            .Set(x => x.TitleAlias, titleAlias);

        await _dc.Conversations.UpdateOneAsync(filterConv, updateConv);
    }

    public async Task<bool> UpdateConversationTags(string conversationId, List<string> toAddTags, List<string> toDeleteTags)
    {
        if (string.IsNullOrEmpty(conversationId)) return false;

        var filter = Builders<ConversationDocument>.Filter.Eq(x => x.Id, conversationId);
        var conv = await _dc.Conversations.Find(filter).FirstOrDefaultAsync();
        if (conv == null) return false;

        var tags = conv.Tags ?? [];
        tags = tags.Concat(toAddTags).Distinct().ToList();
        tags = tags.Where(x => !toDeleteTags.Contains(x, StringComparer.OrdinalIgnoreCase)).ToList();

        var update = Builders<ConversationDocument>.Update
                                                   .Set(x => x.Tags, tags)
                                                   .Set(x => x.UpdatedTime, DateTime.UtcNow);

        var res = await _dc.Conversations.UpdateOneAsync(filter, update);
        return res.ModifiedCount > 0;
    }

    public async Task<bool> AppendConversationTags(string conversationId, List<string> tags)
    {
        if (string.IsNullOrEmpty(conversationId) || tags.IsNullOrEmpty()) return false;

        var filter = Builders<ConversationDocument>.Filter.Eq(x => x.Id, conversationId);
        var conv = await _dc.Conversations.Find(filter).FirstOrDefaultAsync();
        if (conv == null) return false;

        var curTags = conv.Tags ?? new();
        var newTags = curTags.Concat(tags).Distinct(StringComparer.InvariantCultureIgnoreCase).ToList();
        var update = Builders<ConversationDocument>.Update
                                                   .Set(x => x.Tags, newTags)
                                                   .Set(x => x.UpdatedTime, DateTime.UtcNow);

        var res = await _dc.Conversations.UpdateOneAsync(filter, update);
        return res.ModifiedCount > 0;
    }

    public async Task<bool> UpdateConversationMessage(string conversationId, UpdateMessageRequest request)
    {
        if (string.IsNullOrEmpty(conversationId)) return false;

        var filter = Builders<ConversationDialogDocument>.Filter.Eq(x => x.ConversationId, conversationId);
        var foundDialog = await _dc.ConversationDialogs.Find(filter).FirstOrDefaultAsync();
        if (foundDialog == null || foundDialog.Dialogs.IsNullOrEmpty())
        {
            return false;
        }

        var dialogs = foundDialog.Dialogs;
        var candidates = dialogs.Where(x => x.MetaData.MessageId == request.Message.MetaData.MessageId
                                            && x.MetaData.Role == request.Message.MetaData.Role).ToList();

        var found = candidates.Where((_, idx) => idx == request.InnerIndex).FirstOrDefault();
        if (found == null) return false;

        found.Content = request.Message.Content;
        found.RichContent = request.Message.RichContent;

        if (!string.IsNullOrEmpty(found.SecondaryContent))
        {
            found.SecondaryContent = request.Message.Content;
        }

        if (!string.IsNullOrEmpty(found.SecondaryRichContent))
        {
            found.SecondaryRichContent = request.Message.RichContent;
        }

        var update = Builders<ConversationDialogDocument>.Update.Set(x => x.Dialogs, dialogs)
                                                                .Set(x => x.UpdatedTime, DateTime.UtcNow);
        await _dc.ConversationDialogs.UpdateOneAsync(filter, update);
        return true;
    }

    [SideCar]
    public async Task UpdateConversationBreakpoint(string conversationId, ConversationBreakpoint breakpoint)
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
        var updateState = Builders<ConversationStateDocument>.Update.Push(x => x.Breakpoints, newBreakpoint)
                                                                    .Set(x => x.UpdatedTime, DateTime.UtcNow);

        await _dc.ConversationStates.UpdateOneAsync(filterState, updateState);
    }

    [SideCar]
    public async Task<ConversationBreakpoint?> GetConversationBreakpoint(string conversationId)
    {
        if (string.IsNullOrEmpty(conversationId))
        {
            return null;
        }

        var filter = Builders<ConversationStateDocument>.Filter.Eq(x => x.ConversationId, conversationId);
        var state = await _dc.ConversationStates.Find(filter).FirstOrDefaultAsync();
        var leafNode = state?.Breakpoints?.LastOrDefault();

        if (leafNode == null)
        {
            return null;
        }

        return new ConversationBreakpoint
        {
            Breakpoint = leafNode.Breakpoint,
            MessageId = leafNode.MessageId,
            Reason = leafNode.Reason,
            CreatedTime = leafNode.CreatedTime,
        };
    }

    public async Task<ConversationState> GetConversationStates(string conversationId)
    {
        var states = new ConversationState();
        if (string.IsNullOrEmpty(conversationId)) return states;

        var filter = Builders<ConversationStateDocument>.Filter.Eq(x => x.ConversationId, conversationId);
        var foundStates = await _dc.ConversationStates.Find(filter).FirstOrDefaultAsync();
        if (foundStates == null || foundStates.States.IsNullOrEmpty()) return states;

        var savedStates = foundStates.States.Select(x => StateMongoElement.ToDomainElement(x)).ToList();
        return new ConversationState(savedStates);
    }

    [SideCar]
    public async Task UpdateConversationStates(string conversationId, List<StateKeyValue> states)
    {
        if (string.IsNullOrEmpty(conversationId) || states == null) return;

        var filterStates = Builders<ConversationStateDocument>.Filter.Eq(x => x.ConversationId, conversationId);
        var saveStates = states.Select(x => StateMongoElement.ToMongoElement(x)).ToList();
        var updateStates = Builders<ConversationStateDocument>.Update.Set(x => x.States, saveStates)
                                                                     .Set(x => x.UpdatedTime, DateTime.UtcNow);

        await _dc.ConversationStates.UpdateOneAsync(filterStates, updateStates);

        // Update latest states
        var endNodes = BuildLatestStates(saveStates);
        var filter = Builders<ConversationDocument>.Filter.Eq(x => x.Id, conversationId);
        var update = Builders<ConversationDocument>.Update.Set(x => x.LatestStates, endNodes)
                                                          .Set(x => x.UpdatedTime, DateTime.UtcNow);

        await _dc.Conversations.UpdateOneAsync(filter, update);
    }

    public async Task UpdateConversationStatus(string conversationId, string status)
    {
        if (string.IsNullOrEmpty(conversationId) || string.IsNullOrEmpty(status)) return;

        var filter = Builders<ConversationDocument>.Filter.Eq(x => x.Id, conversationId);
        var update = Builders<ConversationDocument>.Update
            .Set(x => x.Status, status)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);

        await _dc.Conversations.UpdateOneAsync(filter, update);
    }

    public async Task<Conversation> GetConversation(string conversationId, bool isLoadStates = false)
    {
        if (string.IsNullOrEmpty(conversationId)) return null;

        var filterConv = Builders<ConversationDocument>.Filter.Eq(x => x.Id, conversationId);
        var filterDialog = Builders<ConversationDialogDocument>.Filter.Eq(x => x.ConversationId, conversationId);

        var conv = await _dc.Conversations.Find(filterConv).FirstOrDefaultAsync();
        var dialog = await _dc.ConversationDialogs.Find(filterDialog).FirstOrDefaultAsync();

        if (conv == null) return null;

        var dialogElements = dialog?.Dialogs?.Select(x => DialogMongoElement.ToDomainElement(x))?.ToList() ?? new List<DialogElement>();
        var curStates = conv.LatestStates?.ToDictionary(x => x.Key, x =>
        {
            var jsonDoc = JsonDocument.Parse(x.Value.ToJson());
            var data = jsonDoc.RootElement.GetProperty("data");
            return data.ValueKind != JsonValueKind.Null ? data.ToString() : null;
        }) ?? [];

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
            Tags = conv.Tags,
            CreatedTime = conv.CreatedTime,
            UpdatedTime = conv.UpdatedTime
        };
    }

    public async Task<PagedItems<Conversation>> GetConversations(ConversationFilter filter)
    {
        if (filter == null)
        {
            filter = ConversationFilter.Empty();
        }

        var convBuilder = Builders<ConversationDocument>.Filter;
        var convFilters = new List<FilterDefinition<ConversationDocument>>() { convBuilder.Empty };

        if (filter?.AgentId != null)
        {
            filter.AgentIds ??= [];
            filter.AgentIds.Add(filter.AgentId);
        }

        // Filter conversations
        if (!string.IsNullOrEmpty(filter?.Id))
        {
            convFilters.Add(convBuilder.Eq(x => x.Id, filter.Id));
        }
        if (!string.IsNullOrEmpty(filter?.Title))
        {
            convFilters.Add(convBuilder.Regex(x => x.Title, new BsonRegularExpression(filter.Title, "i")));
        }
        if (!string.IsNullOrEmpty(filter?.TitleAlias))
        {
            convFilters.Add(convBuilder.Regex(x => x.Title, new BsonRegularExpression(filter.TitleAlias, "i")));
        }
        if (filter?.AgentIds != null && filter.AgentIds.Any())
        {
            convFilters.Add(convBuilder.In(x => x.AgentId, filter.AgentIds));
        }
        if (!string.IsNullOrEmpty(filter?.Status))
        {
            convFilters.Add(convBuilder.Eq(x => x.Status, filter.Status));
        }
        if (!string.IsNullOrEmpty(filter?.Channel))
        {
            convFilters.Add(convBuilder.Eq(x => x.Channel, filter.Channel));
        }
        if (!string.IsNullOrEmpty(filter?.ChannelId))
        {
            convFilters.Add(convBuilder.Eq(x => x.ChannelId, filter.ChannelId));
        }
        if (!string.IsNullOrEmpty(filter?.UserId))
        {
            convFilters.Add(convBuilder.Eq(x => x.UserId, filter.UserId));
        }
        if (!string.IsNullOrEmpty(filter?.TaskId))
        {
            convFilters.Add(convBuilder.Eq(x => x.TaskId, filter.TaskId));
        }
        if (filter?.StartTime != null)
        {
            convFilters.Add(convBuilder.Gte(x => x.CreatedTime, filter.StartTime.Value));
        }
        if (filter?.EndTime != null)
        {
            convFilters.Add(convBuilder.Lte(x => x.CreatedTime, filter.EndTime.Value));
        }
        if (filter?.Tags != null && filter.Tags.Any())
        {
            convFilters.Add(convBuilder.AnyIn(x => x.Tags, filter.Tags));
        }

        // Filter states
        if (filter != null && !filter.States.IsNullOrEmpty())
        {
            foreach (var pair in filter.States)
            {
                if (string.IsNullOrWhiteSpace(pair.Key)) continue;

                // Format key
                var keys = pair.Key.Split(".").ToList();
                keys.Insert(1, "data");
                keys.Insert(0, "LatestStates");
                var formattedKey = string.Join(".", keys);

                if (string.IsNullOrWhiteSpace(pair.Value))
                {
                    convFilters.Add(convBuilder.Exists(formattedKey));
                }
                else if (bool.TryParse(pair.Value, out var boolValue))
                {
                    convFilters.Add(convBuilder.Eq(formattedKey, boolValue));
                }
                else if (int.TryParse(pair.Value, out var intValue))
                {
                    convFilters.Add(convBuilder.Eq(formattedKey, intValue));
                }
                else if (decimal.TryParse(pair.Value, out var decimalValue))
                {
                    convFilters.Add(convBuilder.Eq(formattedKey, decimalValue));
                }
                else if (float.TryParse(pair.Value, out var floatValue))
                {
                    convFilters.Add(convBuilder.Eq(formattedKey, floatValue));
                }
                else if (double.TryParse(pair.Value, out var doubleValue))
                {
                    convFilters.Add(convBuilder.Eq(formattedKey, doubleValue));
                }
                else
                {
                    convFilters.Add(convBuilder.Eq(formattedKey, pair.Value));
                }
            }
        }

        // Sort and paginate
        var filterDef = convBuilder.And(convFilters);
        var sortDef = Builders<ConversationDocument>.Sort.Descending(x => x.CreatedTime);
        var pager = filter?.Pager ?? new Pagination();

        // Apply sorting based on sort and order fields
        if (!string.IsNullOrEmpty(pager?.Sort))
        {
            var sortField = ConvertSnakeCaseToPascalCase(pager.Sort);

            if (pager.Order == "asc")
            {
                sortDef = Builders<ConversationDocument>.Sort.Ascending(sortField);
            }
            else if (pager.Order == "desc")
            {
                sortDef = Builders<ConversationDocument>.Sort.Descending(sortField);
            }
        }

        var docsTask = _dc.Conversations.FindAsync(filterDef, options: new()
        {
            Sort = sortDef,
            Skip = pager.Offset,
            Limit = pager.Size
        });
        var countTask = _dc.Conversations.CountDocumentsAsync(filterDef);
        await Task.WhenAll([docsTask, countTask]);

        var docs = docsTask.Result.ToList();
        var count = countTask.Result;

        var conversations = docs.Select(x =>
        {
            var states = new Dictionary<string, string>();
            if (filter.IsLoadLatestStates)
            {
                states = x.LatestStates.ToDictionary(p => p.Key, p =>
                {
                    var jsonDoc = JsonDocument.Parse(p.Value.ToJson());
                    var data = jsonDoc.RootElement.GetProperty("data");
                    return data.ValueKind != JsonValueKind.Null ? data.ToString() : null;
                });
            }

            return new Conversation
            {
                Id = x.Id.ToString(),
                AgentId = x.AgentId.ToString(),
                UserId = x.UserId.ToString(),
                TaskId = x.TaskId,
                Title = x.Title,
                Channel = x.Channel,
                Status = x.Status,
                DialogCount = x.DialogCount,
                Tags = x.Tags ?? [],
                States = states,
                CreatedTime = x.CreatedTime,
                UpdatedTime = x.UpdatedTime
            };
        }).ToList();

        return new PagedItems<Conversation>
        {
            Items = conversations,
            Count = count
        };
    }

    public async Task<List<Conversation>> GetLastConversations()
    {
        var records = new List<Conversation>();
        var conversations = await _dc.Conversations.Aggregate()
                                                  .Group(c => c.UserId, g => g.First(x => x.CreatedTime == g.Select(y => y.CreatedTime).Max()))
                                                  .ToListAsync();
        return conversations.Select(c => new Conversation()
        {
            Id = c.Id.ToString(),
            AgentId = c.AgentId.ToString(),
            UserId = c.UserId.ToString(),
            Title = c.Title,
            Channel = c.Channel,
            Status = c.Status,
            DialogCount = c.DialogCount,
            Tags = c.Tags ?? new(),
            CreatedTime = c.CreatedTime,
            UpdatedTime = c.UpdatedTime
        }).ToList();
    }

    public async Task<List<string>> GetIdleConversations(int batchSize, int messageLimit, int bufferHours, IEnumerable<string> excludeAgentIds)
    {
        var page = 1;
        var batchLimit = 100;
        var utcNow = DateTime.UtcNow;
        var conversationIds = new List<string>();
        var excludeAgentIdsList = excludeAgentIds?.ToList() ?? new List<string>();

        if (batchSize <= 0 || batchSize > batchLimit)
        {
            batchSize = batchLimit;
        }

        while (true)
        {
            var skip = (page - 1) * batchSize;
            var candidates = await _dc.Conversations.AsQueryable()
                                              .Where(x => ((!excludeAgentIds.Contains(x.AgentId) && x.DialogCount <= messageLimit)
                                                       || (excludeAgentIds.Contains(x.AgentId) && x.DialogCount == 0))
                                                        && x.UpdatedTime <= utcNow.AddHours(-bufferHours))
                                              .Skip(skip)
                                              .Take(batchSize)
                                              .Select(x => x.Id)
                                              .ToListAsync();

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

    public async Task<List<string>> TruncateConversation(string conversationId, string messageId, bool cleanLog = false)
    {
        var deletedMessageIds = new List<string>();
        if (string.IsNullOrEmpty(conversationId) || string.IsNullOrEmpty(messageId))
        {
            return deletedMessageIds;
        }

        var dialogFilter = Builders<ConversationDialogDocument>.Filter.Eq(x => x.ConversationId, conversationId);
        var foundDialog = await _dc.ConversationDialogs.Find(dialogFilter).FirstOrDefaultAsync();
        if (foundDialog == null || foundDialog.Dialogs.IsNullOrEmpty())
        {
            return deletedMessageIds;
        }

        var foundIdx = foundDialog.Dialogs.FindIndex(x => x.MetaData?.MessageId == messageId);
        if (foundIdx < 0)
        {
            return deletedMessageIds;
        }

        deletedMessageIds = foundDialog.Dialogs.Where((x, idx) => idx >= foundIdx && !string.IsNullOrEmpty(x.MetaData?.MessageId))
                                               .Select(x => x.MetaData.MessageId).Distinct().ToList();

        // Handle truncated dialogs
        var truncatedDialogs = foundDialog.Dialogs.Where((x, idx) => idx < foundIdx).ToList();

        // Handle truncated states
        var refTime = foundDialog.Dialogs.ElementAt(foundIdx).MetaData.CreateTime;
        var stateFilter = Builders<ConversationStateDocument>.Filter.Eq(x => x.ConversationId, conversationId);
        var foundStates = await _dc.ConversationStates.Find(stateFilter).FirstOrDefaultAsync();

        var endNodes = new Dictionary<string, BsonDocument>();
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
                endNodes = BuildLatestStates(truncatedStates);
            }

            // Truncate breakpoints
            if (!foundStates.Breakpoints.IsNullOrEmpty())
            {
                var breakpoints = foundStates.Breakpoints ?? new List<BreakpointMongoElement>();
                var truncatedBreakpoints = breakpoints.Where(x => x.CreatedTime < refTime).ToList();
                foundStates.Breakpoints = truncatedBreakpoints;
            }

            // Update
            foundStates.UpdatedTime = DateTime.UtcNow;
            await _dc.ConversationStates.ReplaceOneAsync(stateFilter, foundStates);
        }

        // Save dialogs
        foundDialog.Dialogs = truncatedDialogs;
        foundDialog.UpdatedTime = DateTime.UtcNow;
        await _dc.ConversationDialogs.ReplaceOneAsync(dialogFilter, foundDialog);

        // Update conversation
        var convFilter = Builders<ConversationDocument>.Filter.Eq(x => x.Id, conversationId);
        var updateConv = Builders<ConversationDocument>.Update.Set(x => x.UpdatedTime, DateTime.UtcNow)
                                                              .Set(x => x.LatestStates, endNodes)
                                                              .Set(x => x.DialogCount, truncatedDialogs.Count);
        await _dc.Conversations.UpdateOneAsync(convFilter, updateConv);

        // Remove logs
        if (cleanLog)
        {
            var contentLogBuilder = Builders<ConversationContentLogDocument>.Filter;
            var stateLogBuilder = Builders<ConversationStateLogDocument>.Filter;

            var contentLogFilters = new List<FilterDefinition<ConversationContentLogDocument>>()
            {
                contentLogBuilder.Eq(x => x.ConversationId, conversationId),
                contentLogBuilder.Gte(x => x.CreatedTime, refTime)
            };
            var stateLogFilters = new List<FilterDefinition<ConversationStateLogDocument>>()
            {
                stateLogBuilder.Eq(x => x.ConversationId, conversationId),
                stateLogBuilder.Gte(x => x.CreatedTime, refTime)
            };

            await _dc.ContentLogs.DeleteManyAsync(contentLogBuilder.And(contentLogFilters));
            await _dc.StateLogs.DeleteManyAsync(stateLogBuilder.And(stateLogFilters));
        }

        return deletedMessageIds;
    }

#if !DEBUG
    [SharpCache(10)]
#endif
    public async Task<List<string>> GetConversationStateSearchKeys(ConversationStateKeysFilter filter)
    {
        var builder = Builders<ConversationDocument>.Filter;
        var sortDef = Builders<ConversationDocument>.Sort.Descending(x => x.UpdatedTime);
        var filters = new List<FilterDefinition<ConversationDocument>>()
        {
            builder.Exists(x => x.LatestStates),
            builder.Ne(x => x.LatestStates, [])
        };

        if (!filter.AgentIds.IsNullOrEmpty())
        {
            filters.Add(builder.In(x => x.AgentId, filter.AgentIds));
        }
        if (!filter.UserIds.IsNullOrEmpty())
        {
            filters.Add(builder.In(x => x.UserId, filter.UserIds));
        }
        if (filter.StartTime.HasValue)
        {
            filters.Add(builder.Gte(x => x.CreatedTime, filter.StartTime.Value));
        }
        if (filter.EndTime.HasValue)
        {
            filters.Add(builder.Lte(x => x.CreatedTime, filter.EndTime.Value));
        }

        var convDocs = await _dc.Conversations.Find(builder.And(filters))
                                               .Sort(sortDef)
                                               .Limit(filter.ConvLimit)
                                               .ToListAsync();
        var keys = convDocs.SelectMany(x => x.LatestStates.Select(x => x.Key)).Distinct().ToList();
        return keys;
    }



    public async Task<List<string>> GetConversationsToMigrate(int batchSize = 100)
    {
        var convFilter = Builders<ConversationDocument>.Filter.Exists(x => x.LatestStates, false);
        var sortDef = Builders<ConversationDocument>.Sort.Ascending(x => x.CreatedTime);
        var convIds = await _dc.Conversations.Find(convFilter)
                                             .Sort(sortDef)
                                             .Limit(batchSize)
                                             .Project(x => x.Id)
                                             .ToListAsync();
        return convIds ?? [];
    }

    public async Task<bool> MigrateConvsersationLatestStates(string conversationId)
    {
        if (string.IsNullOrEmpty(conversationId)) return false;

        var stateFilter = Builders<ConversationStateDocument>.Filter.Eq(x => x.ConversationId, conversationId);
        var foundStates = await _dc.ConversationStates.Find(stateFilter).FirstOrDefaultAsync();
        if (foundStates?.States == null) return false;

        var states = foundStates.States.ToList();
        var latestStates = BuildLatestStates(states);

        var convFilter = Builders<ConversationDocument>.Filter.Eq(x => x.Id, conversationId);
        var convUpdate = Builders<ConversationDocument>.Update.Set(x => x.LatestStates, latestStates);
        await _dc.Conversations.UpdateOneAsync(convFilter, convUpdate);

        return true;
    }

    #region Files
    public async Task<List<ConversationFile>> GetConversationFiles(ConversationFileFilter filter)
    {
        if (filter == null || filter.ConversationIds.IsNullOrEmpty())
        {
            return [];
        }

        var builder = Builders<ConversationFileDocument>.Filter;
        var fileFilter = builder.In(x => x.ConversationId, filter.ConversationIds);

        var fileDocs = await _dc.ConversationFiles.Find(fileFilter).ToListAsync();
        var files = fileDocs.Select(x => ConversationFileDocument.ToDomainModel(x)).ToList();
        return files;
    }

    public async Task<bool> SaveConversationFiles(List<ConversationFile> files)
    {
        if (files.IsNullOrEmpty())
        {
            return false;
        }

        try
        {
            var builder = Builders<ConversationFileDocument>.Filter;
            var ops = files.Where(x => !string.IsNullOrEmpty(x.ConversationId))
                           .Select(file =>
                           {
                               var updateBuilder = Builders<ConversationFileDocument>.Update
                                   .Set(y => y.Thumbnail, file.Thumbnail)
                                   .Set(y => y.UpdatedTime, DateTime.UtcNow)
                                   .SetOnInsert(y => y.Id, Guid.NewGuid().ToString())
                                   .SetOnInsert(y => y.ConversationId, file.ConversationId)
                                   .SetOnInsert(y => y.CreatedTime, DateTime.UtcNow);

                               var filter = builder.Eq(y => y.ConversationId, file.ConversationId);

                               return new UpdateOneModel<ConversationFileDocument>(
                                   filter,
                                   updateBuilder
                               ) { IsUpsert = true };
                           })
                           .ToList();

            if (!ops.IsNullOrEmpty())
            {
                await _dc.ConversationFiles.BulkWriteAsync(ops, new BulkWriteOptions { IsOrdered = false });
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when saving conversation files.");
            return false;
        }
    }

    public async Task<bool> DeleteConversationFiles(List<string> conversationIds)
    {
        if (conversationIds.IsNullOrEmpty())
        {
            return false;
        }

        try
        {
            var builder = Builders<ConversationFileDocument>.Filter;
            var filter = builder.In(x => x.ConversationId, conversationIds);
            var result = await _dc.ConversationFiles.DeleteManyAsync(filter);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when deleting conversation files.");
            return false;
        }
    }
    #endregion


    #region Private methods
    private string ConvertSnakeCaseToPascalCase(string snakeCase)
    {
        string[] words = snakeCase.Split('_');
        StringBuilder pascalCase = new();

        foreach (string word in words)
        {
            if (!string.IsNullOrEmpty(word))
            {
                string firstLetter = word[..1].ToUpper();
                string restOfWord = word[1..].ToLower();
                pascalCase.Append(firstLetter + restOfWord);
            }
        }

        return pascalCase.ToString();
    }

    private Dictionary<string, BsonDocument> BuildLatestStates(List<StateMongoElement> states)
    {
        var endNodes = new Dictionary<string, BsonDocument>();
        if (states.IsNullOrEmpty())
        {
            return endNodes;
        }

        foreach (var pair in states)
        {
            var value = pair.Values?.LastOrDefault();
            if (value == null || !value.Active) continue;

            try
            {
                var jsonStr = JsonSerializer.Serialize(new { Data = JsonDocument.Parse(value.Data) }, _botSharpOptions.JsonSerializerOptions);
                var json = BsonDocument.Parse(jsonStr);
                endNodes[pair.Key] = json;
            }
            catch
            {
                var str = JsonSerializer.Serialize(new { Data = value.Data }, _botSharpOptions.JsonSerializerOptions);
                var json = BsonDocument.Parse(str);
                endNodes[pair.Key] = json;
            }
        }

        return endNodes;
    }
    #endregion
}

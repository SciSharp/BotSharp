using BotSharp.Abstraction.Loggers.Models;
using BotSharp.Abstraction.Repositories.Filters;
using MongoDB.Driver;
using System.Text.Json;

namespace BotSharp.Plugin.MongoStorage.Repository;

public partial class MongoRepository
{
    #region LLM Completion Log
    public void SaveLlmCompletionLog(LlmCompletionLog log)
    {
        if (log == null) return;

        var conversationId = log.ConversationId.IfNullOrEmptyAs(Guid.NewGuid().ToString());
        var messageId = log.MessageId.IfNullOrEmptyAs(Guid.NewGuid().ToString());

        var data = new LlmCompletionLogDocument
        {
            Id = Guid.NewGuid().ToString(),
            ConversationId = conversationId,
            MessageId = messageId,
            AgentId = log.AgentId,
            Prompt = log.Prompt,
            Response = log.Response,
            CreatedTime = log.CreatedTime
        };
        _dc.LlmCompletionLogs.InsertOne(data);
    }

    #endregion

    #region Conversation Content Log
    public void SaveConversationContentLog(ContentLogOutputModel log)
    {
        if (log == null) return;

        var filter = Builders<ConversationDocument>.Filter.Eq(x => x.Id, log.ConversationId);
        var found = _dc.Conversations.Find(filter).FirstOrDefault();
        if (found == null) return;

        var logDoc = new ConversationContentLogDocument
        {
            ConversationId = log.ConversationId,
            MessageId = log.MessageId,
            Name = log.Name,
            AgentId = log.AgentId,
            Role = log.Role,
            Source = log.Source,
            Content = log.Content,
            CreatedTime = log.CreatedTime
        };

        _dc.ContentLogs.InsertOne(logDoc);
    }

    public DateTimePagination<ContentLogOutputModel> GetConversationContentLogs(string conversationId, ConversationLogFilter filter)
    {
        var builder = Builders<ConversationContentLogDocument>.Filter;
        var logFilters = new List<FilterDefinition<ConversationContentLogDocument>>
        {
            builder.Eq(x => x.ConversationId, conversationId),
            builder.Lt(x => x.CreatedTime, filter.StartTime)
        };
        var logSortDef = Builders<ConversationContentLogDocument>.Sort.Descending(x => x.CreatedTime);

        var docs = _dc.ContentLogs.Find(builder.And(logFilters)).Sort(logSortDef).Limit(filter.Size).ToList();
        var logs = docs.Select(x => new ContentLogOutputModel
        {
            ConversationId = x.ConversationId,
            MessageId = x.MessageId,
            Name = x.Name,
            AgentId = x.AgentId,
            Role = x.Role,
            Source = x.Source,
            Content = x.Content,
            CreatedTime = x.CreatedTime
        }).ToList();

        logs.Reverse();
        return new DateTimePagination<ContentLogOutputModel>
        {
            Items = logs,
            Count = logs.Count,
            NextTime = logs.FirstOrDefault()?.CreatedTime
        };
    }
    #endregion

    #region Conversation State Log
    public void SaveConversationStateLog(ConversationStateLogModel log)
    {
        if (log == null) return;

        var filter = Builders<ConversationDocument>.Filter.Eq(x => x.Id, log.ConversationId);
        var found = _dc.Conversations.Find(filter).FirstOrDefault();
        if (found == null) return;

        var logDoc = new ConversationStateLogDocument
        {
            ConversationId = log.ConversationId,
            AgentId= log.AgentId,
            MessageId = log.MessageId,
            States = log.States,
            CreatedTime = log.CreatedTime
        };

        _dc.StateLogs.InsertOne(logDoc);
    }

    public DateTimePagination<ConversationStateLogModel> GetConversationStateLogs(string conversationId, ConversationLogFilter filter)
    {
        var builder = Builders<ConversationStateLogDocument>.Filter;
        var logFilters = new List<FilterDefinition<ConversationStateLogDocument>>
        {
            builder.Eq(x => x.ConversationId, conversationId),
            builder.Lt(x => x.CreatedTime, filter.StartTime)
        };
        var logSortDef = Builders<ConversationStateLogDocument>.Sort.Descending(x => x.CreatedTime);

        var docs = _dc.StateLogs.Find(builder.And(logFilters)).Sort(logSortDef).Limit(filter.Size).ToList();
        var logs = docs.Select(x => new ConversationStateLogModel
        {
            ConversationId = x.ConversationId,
            AgentId = x.AgentId,
            MessageId = x.MessageId,
            States = x.States,
            CreatedTime = x.CreatedTime
        }).ToList();

        logs.Reverse();
        return new DateTimePagination<ConversationStateLogModel>
        {
            Items = logs,
            Count = logs.Count,
            NextTime = logs.FirstOrDefault()?.CreatedTime
        };
    }
    #endregion

    #region Instruction Log
    public bool SaveInstructionLogs(IEnumerable<InstructionLogModel> logs)
    {
        if (logs.IsNullOrEmpty()) return false;

        var docs = new List<InstructionLogDocument>();
        foreach (var log in logs)
        {
            var doc = InstructionLogDocument.ToMongoModel(log);
            foreach (var pair in log.States)
            {
                try
                {
                    var jsonStr = JsonSerializer.Serialize(new { Data = JsonDocument.Parse(pair.Value) }, _botSharpOptions.JsonSerializerOptions);
                    var json = BsonDocument.Parse(jsonStr);
                    doc.States[pair.Key] = json;
                }
                catch
                {
                    var jsonStr = JsonSerializer.Serialize(new { Data = pair.Value }, _botSharpOptions.JsonSerializerOptions);
                    var json = BsonDocument.Parse(jsonStr);
                    doc.States[pair.Key] = json;
                }
            }
            docs.Add(doc);
        }

        _dc.InstructionLogs.InsertMany(docs);
        return true;
    }

    public PagedItems<InstructionLogModel> GetInstructionLogs(InstructLogFilter filter)
    {
        if (filter == null)
        {
            filter = InstructLogFilter.Empty();
        }

        var logBuilder = Builders<InstructionLogDocument>.Filter;
        var logFilters = new List<FilterDefinition<InstructionLogDocument>>() { logBuilder.Empty };

        // Filter logs
        if (!filter.AgentIds.IsNullOrEmpty())
        {
            logFilters.Add(logBuilder.In(x => x.AgentId, filter.AgentIds));
        }
        if (!filter.Providers.IsNullOrEmpty())
        {
            logFilters.Add(logBuilder.In(x => x.Provider, filter.Providers));
        }
        if (!filter.Models.IsNullOrEmpty())
        {
            logFilters.Add(logBuilder.In(x => x.Model, filter.Models));
        }
        if (!filter.TemplateNames.IsNullOrEmpty())
        {
            logFilters.Add(logBuilder.In(x => x.TemplateName, filter.TemplateNames));
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
                keys.Insert(0, "States");
                var formattedKey = string.Join(".", keys);

                if (string.IsNullOrWhiteSpace(pair.Value))
                {
                    logFilters.Add(logBuilder.Exists(formattedKey));
                }
                else if (bool.TryParse(pair.Value, out var boolValue))
                {
                    logFilters.Add(logBuilder.Eq(formattedKey, boolValue));
                }
                else if (int.TryParse(pair.Value, out var intValue))
                {
                    logFilters.Add(logBuilder.Eq(formattedKey, intValue));
                }
                else if (decimal.TryParse(pair.Value, out var decimalValue))
                {
                    logFilters.Add(logBuilder.Eq(formattedKey, decimalValue));
                }
                else if (float.TryParse(pair.Value, out var floatValue))
                {
                    logFilters.Add(logBuilder.Eq(formattedKey, floatValue));
                }
                else if (double.TryParse(pair.Value, out var doubleValue))
                {
                    logFilters.Add(logBuilder.Eq(formattedKey, doubleValue));
                }
                else
                {
                    logFilters.Add(logBuilder.Eq(formattedKey, pair.Value));
                }
            }
        }

        var filterDef = logBuilder.And(logFilters);
        var sortDef = Builders<InstructionLogDocument>.Sort.Descending(x => x.CreatedTime);
        var docs = _dc.InstructionLogs.Find(filterDef).Sort(sortDef).Skip(filter.Offset).Limit(filter.Size).ToList();
        var count = _dc.InstructionLogs.CountDocuments(filterDef);

        var logs = docs.Select(x =>
        {
            var log = InstructionLogDocument.ToDomainModel(x);
            log.States = x.States.ToDictionary(p => p.Key, p =>
            {
                var jsonStr = p.Value.ToJson();
                var jsonDoc = JsonDocument.Parse(jsonStr);
                var data = jsonDoc.RootElement.GetProperty("data");
                return data.ValueKind != JsonValueKind.Null ? data.ToString() : null;
            });
            return log;
        }).ToList();

        return new PagedItems<InstructionLogModel>
        {
            Items = logs,
            Count = (int)count
        };
    }

    public List<string> GetInstructionLogSearchKeys(InstructLogKeysFilter filter)
    {
        var builder = Builders<InstructionLogDocument>.Filter;
        var sortDef = Builders<InstructionLogDocument>.Sort.Descending(x => x.CreatedTime);
        var filters = new List<FilterDefinition<InstructionLogDocument>>()
        {
            builder.Exists(x => x.States),
            builder.Ne(x => x.States, [])
        };

        if (!filter.AgentIds.IsNullOrEmpty())
        {
            filters.Add(builder.In(x => x.AgentId, filter.AgentIds));
        }

        if (!filter.UserIds.IsNullOrEmpty())
        {
            filters.Add(builder.In(x => x.UserId, filter.UserIds));
        }

        var convDocs = _dc.InstructionLogs.Find(builder.And(filters))
                                          .Sort(sortDef)
                                          .Limit(filter.LogLimit)
                                          .ToList();
        var keys = convDocs.SelectMany(x => x.States.Select(x => x.Key)).Distinct().ToList();
        return keys;
    }
    #endregion
}

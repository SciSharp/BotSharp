using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Repositories.Filters;
using BotSharp.Abstraction.Repositories.Options;

namespace BotSharp.Plugin.MongoStorage.Repository;

public partial class MongoRepository
{
    #region Code script
    public List<AgentCodeScript> GetAgentCodeScripts(string agentId, AgentCodeScriptFilter? filter = null)
    {
        if (string.IsNullOrWhiteSpace(agentId))
        {
            return [];
        }

        filter ??= AgentCodeScriptFilter.Empty();

        var builder = Builders<AgentCodeScriptDocument>.Filter;
        var filters = new List<FilterDefinition<AgentCodeScriptDocument>>()
        {
            builder.Eq(x => x.AgentId, agentId)
        };

        if (!filter.ScriptNames.IsNullOrEmpty())
        {
            filters.Add(builder.In(x => x.Name, filter.ScriptNames));
        }
        if (!filter.ScriptTypes.IsNullOrEmpty())
        {
            filters.Add(builder.In(x => x.ScriptType, filter.ScriptTypes));
        }

        var found = _dc.AgentCodeScripts.Find(builder.And(filters)).ToList();
        return found.Select(x => AgentCodeScriptDocument.ToDomainModel(x)).ToList();
    }

    public string? GetAgentCodeScript(string agentId, string scriptName, string scriptType = AgentCodeScriptType.Src)
    {
        if (string.IsNullOrWhiteSpace(agentId)
            || string.IsNullOrWhiteSpace(scriptName)
            || string.IsNullOrWhiteSpace(scriptType))
        {
            return null;
        }

        var builder = Builders<AgentCodeScriptDocument>.Filter;
        var filters = new List<FilterDefinition<AgentCodeScriptDocument>>()
        {
            builder.Eq(x => x.AgentId, agentId),
            builder.Eq(x => x.Name, scriptName),
            builder.Eq(x => x.ScriptType, scriptType)
        };

        var found = _dc.AgentCodeScripts.Find(builder.And(filters)).FirstOrDefault();
        return found?.Content;
    }

    public bool UpdateAgentCodeScripts(string agentId, List<AgentCodeScript> scripts, AgentCodeScriptDbUpdateOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(agentId) || scripts.IsNullOrEmpty())
        {
            return false;
        }

        var builder = Builders<AgentCodeScriptDocument>.Filter;
        var ops = scripts.Where(x => !string.IsNullOrWhiteSpace(x.Name))
                         .Select(x => new UpdateOneModel<AgentCodeScriptDocument>(
                                builder.And(new List<FilterDefinition<AgentCodeScriptDocument>>
                                {
                                    builder.Eq(y => y.AgentId, agentId),
                                    builder.Eq(y => y.Name, x.Name),
                                    builder.Eq(y => y.ScriptType, x.ScriptType)
                                }),
                                Builders<AgentCodeScriptDocument>.Update.Set(y => y.Content, x.Content)
                                                                        .Set(x => x.UpdatedTime, DateTime.UtcNow)
                         ) { IsUpsert = options?.IsUpsert ?? false })
                         .ToList();

        var result = _dc.AgentCodeScripts.BulkWrite(ops, new BulkWriteOptions { IsOrdered = false });
        return result.ModifiedCount > 0 || result.MatchedCount > 0;
    }

    public bool BulkInsertAgentCodeScripts(string agentId, List<AgentCodeScript> scripts)
    {
        if (string.IsNullOrWhiteSpace(agentId) || scripts.IsNullOrEmpty())
        {
            return false;
        }

        var docs = scripts.Select(x =>
        {
            var script = AgentCodeScriptDocument.ToMongoModel(x);
            script.AgentId = agentId;
            script.Id = x.Id.IfNullOrEmptyAs(Guid.NewGuid().ToString())!;
            script.CreatedTime = DateTime.UtcNow;
            script.UpdatedTime = DateTime.UtcNow;
            return script;
        }).ToList();

        _dc.AgentCodeScripts.InsertMany(docs);
        return true;
    }

    public bool DeleteAgentCodeScripts(string agentId, List<AgentCodeScript>? scripts = null)
    {
        if (string.IsNullOrWhiteSpace(agentId))
        {
            return false;
        }

        DeleteResult deleted;
        var builder = Builders<AgentCodeScriptDocument>.Filter;

        if (scripts != null)
        {
            var scriptPaths = scripts.Select(x => x.CodePath);
            var exprFilter = new BsonDocument("$expr", new BsonDocument("$in", new BsonArray
            {
                new BsonDocument("$concat", new BsonArray { "$ScriptType", "/", "$Name" }),
                new BsonArray(scriptPaths)
            }));
            
            var filterDef = builder.And(
                builder.Eq(x => x.AgentId, agentId),
                new BsonDocumentFilterDefinition<AgentCodeScriptDocument>(exprFilter)
            );
            deleted = _dc.AgentCodeScripts.DeleteMany(filterDef);
        }
        else
        {
            deleted = _dc.AgentCodeScripts.DeleteMany(builder.Eq(x => x.AgentId, agentId));
        }

        return deleted.DeletedCount > 0;
    }
    #endregion
}

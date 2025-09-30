using BotSharp.Abstraction.Agents.Models;

namespace BotSharp.Plugin.MongoStorage.Repository;

public partial class MongoRepository
{
    #region Code
    public List<AgentCodeScript> GetAgentCodeScripts(string agentId, List<string>? scriptNames = null)
    {
        if (string.IsNullOrWhiteSpace(agentId))
        {
            return [];
        }

        var builder = Builders<AgentCodeDocument>.Filter;
        var filters = new List<FilterDefinition<AgentCodeDocument>>()
        {
            builder.Eq(x => x.AgentId, agentId)
        };

        if (!scriptNames.IsNullOrEmpty())
        {
            filters.Add(builder.In(x => x.Name, scriptNames));
        }

        var found = _dc.AgentCodes.Find(builder.And(filters)).ToList();
        return found.Select(x => AgentCodeDocument.ToDomainModel(x)).ToList();
    }

    public string? GetAgentCodeScript(string agentId, string scriptName)
    {
        if (string.IsNullOrWhiteSpace(agentId)
            || string.IsNullOrWhiteSpace(scriptName))
        {
            return null;
        }

        var builder = Builders<AgentCodeDocument>.Filter;
        var filters = new List<FilterDefinition<AgentCodeDocument>>()
        {
            builder.Eq(x => x.AgentId, agentId),
            builder.Eq(x => x.Name, scriptName)
        };

        var found = _dc.AgentCodes.Find(builder.And(filters)).FirstOrDefault();
        return found?.Content;
    }

    public bool UpdateAgentCodeScript(string agentId, AgentCodeScript script)
    {
        if (string.IsNullOrWhiteSpace(agentId) || script == null)
        {
            return false;
        }

        var builder = Builders<AgentCodeDocument>.Filter;
        var filters = new List<FilterDefinition<AgentCodeDocument>>()
        {
            builder.Eq(x => x.AgentId, agentId),
            builder.Eq(x => x.Name, script.Name)
        };
        var filterDef = builder.And(filters);

        var found = _dc.AgentCodes.Find(filterDef).FirstOrDefault();
        if (found == null)
        {
            return false;
        }

        var update = Builders<AgentCodeDocument>.Update.Set(x => x.Content, script.Content);
        _dc.AgentCodes.UpdateOne(filterDef, update);
        return true;
    }

    public bool BulkInsertAgentCodeScripts(string agentId, List<AgentCodeScript> scripts)
    {
        if (string.IsNullOrWhiteSpace(agentId) || scripts.IsNullOrEmpty())
        {
            return false;
        }

        var docs = scripts.Select(x =>
        {
            var script = AgentCodeDocument.ToMongoModel(x);
            script.AgentId = agentId;
            script.Id = !string.IsNullOrEmpty(x.Id) ? x.Id : Guid.NewGuid().ToString();
            return script;
        }).ToList();

        _dc.AgentCodes.InsertMany(docs);
        return true;
    }

    public bool DeleteAgentCodeScripts(string agentId, List<string>? scriptNames)
    {
        if (string.IsNullOrWhiteSpace(agentId))
        {
            return false;
        }

        var filterDef = Builders<AgentCodeDocument>.Filter.Empty;
        if (scriptNames != null)
        {
            var builder = Builders<AgentCodeDocument>.Filter;
            var filters = new List<FilterDefinition<AgentCodeDocument>>
            {
                builder.In(x => x.Name, scriptNames)
            };
            filterDef = builder.And(filters);
        }

        var deleted = _dc.AgentCodes.DeleteMany(filterDef);
        return deleted.DeletedCount > 0;
    }
    #endregion
}

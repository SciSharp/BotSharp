using BotSharp.Abstraction.Crontab.Models;
using Microsoft.Extensions.Logging;

namespace BotSharp.Plugin.MongoStorage.Repository;

public partial class MongoRepository
{
    public bool InsertCrontabItem(CrontabItem item)
    {
        if (item == null)
        {
            return false;
        }

        try
        {
            var cronDoc = CrontabItemDocument.ToMongoModel(item);
            cronDoc.Id = Guid.NewGuid().ToString();
            _dc.CrontabItems.InsertOne(cronDoc);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error when saving crontab item: {ex.Message}\r\n{ex.InnerException}");
            return false;
        }
    }


    public PagedItems<CrontabItem> GetCrontabItems(CrontabItemFilter filter)
    {
        if (filter == null)
        {
            filter = CrontabItemFilter.Empty();
        }

        var cronBuilder = Builders<CrontabItemDocument>.Filter;
        var cronFilters = new List<FilterDefinition<CrontabItemDocument>>() { cronBuilder.Empty };

        // Filter conversations
        if (filter?.AgentIds != null)
        {
            cronFilters.Add(cronBuilder.In(x => x.AgentId, filter.AgentIds));
        }
        if (filter?.ConversationIds != null)
        {
            cronFilters.Add(cronBuilder.In(x => x.ConversationId, filter.ConversationIds));
        }
        if (filter?.UserIds != null)
        {
            cronFilters.Add(cronBuilder.In(x => x.UserId, filter.UserIds));
        }

        // Sort and paginate
        var filterDef = cronBuilder.And(cronFilters);
        var sortDef = Builders<CrontabItemDocument>.Sort.Descending(x => x.CreatedTime);

        var cronDocs = _dc.CrontabItems.Find(filterDef).Sort(sortDef).Skip(filter.Offset).Limit(filter.Size).ToList();
        var count = _dc.CrontabItems.CountDocuments(filterDef);

        var crontabItems = cronDocs.Select(x => CrontabItemDocument.ToDomainModel(x)).ToList();

        return new PagedItems<CrontabItem>
        {
            Items = crontabItems,
            Count = (int)count
        };
    }
}

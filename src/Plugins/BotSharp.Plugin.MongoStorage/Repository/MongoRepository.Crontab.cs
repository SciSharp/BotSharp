using BotSharp.Abstraction.Crontab.Models;
using Microsoft.Extensions.Logging;

namespace BotSharp.Plugin.MongoStorage.Repository;

public partial class MongoRepository
{
    public bool UpsertCrontabItem(CrontabItem item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.ConversationId))
        {
            return false;
        }

        try
        {
            var cronDoc = CrontabItemDocument.ToMongoModel(item);
            cronDoc.Id = Guid.NewGuid().ToString();

            var filter = Builders<CrontabItemDocument>.Filter.Eq(x => x.ConversationId, item.ConversationId);
            var result = _dc.CrontabItems.ReplaceOne(filter, cronDoc, new ReplaceOptions
            {
                IsUpsert = true
            });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error when saving crontab item: {ex.Message}\r\n{ex.InnerException}");
            return false;
        }
    }


    public bool DeleteCrontabItem(string conversationId)
    {
        if (string.IsNullOrWhiteSpace(conversationId))
        {
            return false;
        }

        var filter = Builders<CrontabItemDocument>.Filter.Eq(x => x.ConversationId, conversationId);
        var result = _dc.CrontabItems.DeleteMany(filter);
        return result.DeletedCount > 0;
    }


    public PagedItems<CrontabItem> GetCrontabItems(CrontabItemFilter filter)
    {
        if (filter == null)
        {
            filter = CrontabItemFilter.Empty();
        }

        var cronBuilder = Builders<CrontabItemDocument>.Filter;
        var cronFilters = new List<FilterDefinition<CrontabItemDocument>>() { cronBuilder.Empty };

        // Filter cron
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

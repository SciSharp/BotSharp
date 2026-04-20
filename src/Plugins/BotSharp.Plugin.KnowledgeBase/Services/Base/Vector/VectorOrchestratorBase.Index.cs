using BotSharp.Abstraction.Models;

namespace BotSharp.Plugin.KnowledgeBase.Services;

public abstract partial class VectorOrchestratorBase
{
    #region Index
    public async Task<SuccessFailResponse<string>> CreateIndexes(string collectionName, KnowledgeIndexOptions options)
    {
        if (string.IsNullOrWhiteSpace(collectionName))
        {
            return new();
        }

        var vectorDb = GetVectorDb(options?.DbProvider);
        if (vectorDb == null)
        {
            return new();
        }

        var response = new SuccessFailResponse<string>();
        var innerOptions = options?.Items?.DistinctBy(x => x.FieldName)?.ToList();
        if (innerOptions.IsNullOrEmpty())
        {
            return new();
        }

        foreach (var option in innerOptions!)
        {
            var created = await vectorDb.CreateCollectionPayloadIndex(collectionName, option);
            var field = $"{option.FieldName} ({option.FieldSchemaType})";
            if (created)
            {
                response.Success.Add(field);
            }
            else
            {
                response.Fail.Add(field);
            }
        }

        return response;
    }

    public async Task<SuccessFailResponse<string>> DeleteIndexes(string collectionName, KnowledgeIndexOptions options)
    {
        if (string.IsNullOrWhiteSpace(collectionName))
        {
            return new();
        }

        var vectorDb = GetVectorDb(options?.DbProvider);
        if (vectorDb == null)
        {
            return new();
        }

        var response = new SuccessFailResponse<string>();
        var innerOptions = options?.Items?.DistinctBy(x => x.FieldName)?.ToList();
        if (innerOptions.IsNullOrEmpty())
        {
            return new();
        }

        foreach (var option in innerOptions!)
        {
            var deleted = await vectorDb.DeleteCollectionPayloadIndex(collectionName, option);
            var field = $"{option.FieldName}";
            if (deleted)
            {
                response.Success.Add(field);
            }
            else
            {
                response.Fail.Add(field);
            }
        }

        return response;
    }
    #endregion
}

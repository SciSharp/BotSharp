using BotSharp.Abstraction.Models;

namespace BotSharp.Plugin.KnowledgeBase.Services;

public partial class KnowledgeService
{
    public async Task<SuccessFailResponse<string>> CreateVectorCollectionPayloadIndexes(string collectionName, IEnumerable<CreateVectorCollectionIndexOptions> options)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(collectionName) || options.IsNullOrEmpty())
            {
                return new();
            }

            var response = new SuccessFailResponse<string>();
            var vectorDb = GetVectorDb();
            foreach (var option in options)
            {
                var created = await vectorDb.CreateCollectionPayloadIndex(collectionName, option);
                var field = $"{option.FieldName}-{option.FieldSchemaType}";
                if (created)
                {
                    response.Success.Add(field);
                }
                else
                {
                    _logger.LogError($"Failed to create vector collection payload index ({collectionName}-{field}).");
                    response.Fail.Add(field);
                }
            }
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when creating vector collection payload index ({collectionName}).");
            return new();
        }
    }

    public async Task<SuccessFailResponse<string>> DeleteVectorCollectionPayloadIndexes(string collectionName, IEnumerable<DeleteVectorCollectionIndexOptions> options)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(collectionName) || options.IsNullOrEmpty())
            {
                return new();
            }

            var response = new SuccessFailResponse<string>();
            var vectorDb = GetVectorDb();
            foreach (var option in options)
            {
                var deleted = await vectorDb.DeleteCollectionPayloadIndex(collectionName, option);
                var field = $"{option.FieldName}";
                if (deleted)
                {
                    response.Success.Add(field);
                }
                else
                {
                    _logger.LogError($"Failed to deleting vector collection payload index ({collectionName}-{field}).");
                    response.Fail.Add(field);
                }
            }
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when deleting vector collection payload index ({collectionName}).");
            return new();
        }
    }
}

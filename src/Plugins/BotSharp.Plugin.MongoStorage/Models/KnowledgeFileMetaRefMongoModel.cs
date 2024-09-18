using BotSharp.Abstraction.Knowledges.Models;

namespace BotSharp.Plugin.MongoStorage.Models;

public class KnowledgeFileMetaRefMongoModel
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Url { get; set; }
    public string? JsonContent { get; set; }

    public static KnowledgeFileMetaRefMongoModel? ToMongoModel(DocMetaRefData? model)
    {
        if (model == null) return null;

        return new KnowledgeFileMetaRefMongoModel
        {
            Id = model.Id,
            Name = model.Name,
            Url = model.Url,
            JsonContent = model.JsonContent
        };
    }

    public static DocMetaRefData? ToDomainModel(KnowledgeFileMetaRefMongoModel? model)
    {
        if (model == null) return null;

        return new DocMetaRefData
        {
            Id = model.Id,
            Name = model.Name,
            Url = model.Url,
            JsonContent = model.JsonContent
        };
    }
}

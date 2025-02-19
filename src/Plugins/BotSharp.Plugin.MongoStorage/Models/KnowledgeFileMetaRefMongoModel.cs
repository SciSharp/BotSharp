using BotSharp.Abstraction.Knowledges.Models;

namespace BotSharp.Plugin.MongoStorage.Models;

[BsonIgnoreExtraElements(Inherited = true)]
public class KnowledgeFileMetaRefMongoModel
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Type { get; set; } = default!;
    public string Url { get; set; } = default!;
    public IDictionary<string, string>? Data { get; set; }

    public static KnowledgeFileMetaRefMongoModel? ToMongoModel(DocMetaRefData? model)
    {
        if (model == null) return null;

        return new KnowledgeFileMetaRefMongoModel
        {
            Id = model.Id,
            Name = model.Name,
            Type = model.Type,
            Url = model.Url,
            Data = model.Data
        };
    }

    public static DocMetaRefData? ToDomainModel(KnowledgeFileMetaRefMongoModel? model)
    {
        if (model == null) return null;

        return new DocMetaRefData
        {
            Id = model.Id,
            Name = model.Name,
            Type = model.Type,
            Url = model.Url,
            Data = model.Data
        };
    }
}

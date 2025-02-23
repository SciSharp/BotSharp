namespace BotSharp.Plugin.MongoStorage.Collections;

public class KnowledgeCollectionFileMetaDocument : MongoBase
{
    public string Collection { get; set; } = default!;
    public Guid FileId { get; set; }
    public string FileName { get; set; } = default!;
    public string FileSource { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public string VectorStoreProvider { get; set; } = default!;
    public IEnumerable<string> VectorDataIds { get; set; } = [];
    public KnowledgeFileMetaRefMongoModel? RefData { get; set; }
    public DateTime CreatedDate { get; set; }
    public string CreateUserId { get; set; } = default!;
}

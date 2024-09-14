namespace BotSharp.Plugin.MongoStorage.Collections;

public class KnowledgeCollectionFileDocument : MongoBase
{
    public string Collection { get; set; }
    public string FileId { get; set; }
    public string FileName { get; set; }
    public string FileSource { get; set; }
    public string ContentType { get; set; }
    public string VectorStoreProvider { get; set; }
    public IEnumerable<string> VectorDataIds { get; set; } = new List<string>();
    public DateTime CreateDate { get; set; }
    public string CreateUserId { get; set; }
}

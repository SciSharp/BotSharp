using BotSharp.Abstraction.VectorStorage.Models;

namespace BotSharp.Abstraction.Knowledges.Models;

public class KnowledgeCollectionSnapshot
{
    public string Name { get; set; } = default!;
    public long Size { get; set; }
    public DateTime CreatedTime { get; set; }
    public string? CheckSum { get; set; }

    public static KnowledgeCollectionSnapshot? CopyFrom(VectorCollectionSnapshot? model)
    {
        if (model == null)
        {
            return null;
        }

        return new KnowledgeCollectionSnapshot
        {
            Name = model.Name,
            Size = model.Size,
            CreatedTime = model.CreatedTime,
            CheckSum = model.CheckSum
        };
    }
}

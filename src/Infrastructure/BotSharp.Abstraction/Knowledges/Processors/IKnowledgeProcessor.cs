using BotSharp.Abstraction.Knowledges.Options;
using BotSharp.Abstraction.Knowledges.Responses;

namespace BotSharp.Abstraction.Knowledges.Processors;

public interface IKnowledgeProcessor
{
    public string Provider { get; }

    Task<FileKnowledgeResponse> GetFileKnowledgeAsync(FileBinaryDataModel file, FileKnowledgeHandleOptions? options = null)
        => throw new NotImplementedException();
}

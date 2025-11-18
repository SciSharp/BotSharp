using BotSharp.Abstraction.Files.Options;
using BotSharp.Abstraction.Files.Responses;
using BotSharp.Abstraction.Knowledges.Options;
using BotSharp.Abstraction.Knowledges.Responses;

namespace BotSharp.Abstraction.Files.Proccessors;

public interface IFileProcessor
{
    public string Provider { get; }

    Task<FileHandleResponse> HandleFilesAsync(Agent agent, string text, IEnumerable<InstructFileModel> files, FileHandleOptions? options = null)
        => throw new NotImplementedException();

    Task<FileKnowledgeResponse> GetFileKnowledgeAsync(FileBinaryDataModel file, FileKnowledgeHandleOptions? options = null)
        => throw new NotImplementedException();
}

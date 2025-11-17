using BotSharp.Abstraction.Files.Options;
using BotSharp.Abstraction.Files.Responses;

namespace BotSharp.Abstraction.Files.Proccessors;

public interface IFileProcessor
{
    public string Provider { get; }

    Task<FileHandleResponse> HandleFilesAsync(Agent agent, string text, IEnumerable<InstructFileModel> files, FileHandleOptions? options = null)
        => throw new NotImplementedException();
}

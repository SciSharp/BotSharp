using BotSharp.Abstraction.Files.Options;
using BotSharp.Abstraction.Files.Responses;

namespace BotSharp.Abstraction.Files.Proccessors;

public interface IFileLlmProcessor
{
    public string Provider { get; }

    Task<FileLlmInferenceResponse> GetFileLlmInferenceAsync(Agent agent, string text, IEnumerable<InstructFileModel> files, FileLlmProcessOptions? options = null)
        => throw new NotImplementedException();
}

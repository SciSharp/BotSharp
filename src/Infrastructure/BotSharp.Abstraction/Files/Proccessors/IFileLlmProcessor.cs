namespace BotSharp.Abstraction.Files.Proccessors;

public interface IFileLlmProcessor
{
    public string Provider { get; }

    Task<RoleDialogModel> GetFileLlmInferenceAsync(Agent agent, string text, IEnumerable<InstructFileModel> files, FileLlmProcessOptions? options = null)
        => throw new NotImplementedException();
}

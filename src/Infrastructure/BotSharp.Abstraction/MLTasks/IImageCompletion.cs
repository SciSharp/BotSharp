using System.IO;

namespace BotSharp.Abstraction.MLTasks;

public interface IImageCompletion
{
    /// <summary>
    /// The LLM provider like Microsoft Azure, OpenAI, ClaudAI
    /// </summary>
    string Provider { get; }

    /// <summary>
    /// Set model name, one provider can consume different model or version(s)
    /// </summary>
    /// <param name="model">deployment name</param>
    void SetModelName(string model);

    Task<RoleDialogModel> GetImageGeneration(Agent agent, RoleDialogModel message);

    Task<RoleDialogModel> GetImageVariation(Agent agent, RoleDialogModel message, Stream image, string imageFileName);
}

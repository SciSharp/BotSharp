using BotSharp.Abstraction.Instructs.Models;

namespace BotSharp.Abstraction.Instructs;

public interface IInstructService
{
    /// <summary>
    /// Execute completion by using specified instruction or template
    /// </summary>
    /// <param name="agentId"></param>
    /// <param name="message"></param>
    /// <param name="instruction"></param>
    /// <param name="templateName"></param>
    /// <param name="files"></param>
    /// <param name="codeOptions"></param>
    /// <param name="fileOptions"></param>
    /// <returns></returns>
    Task<InstructResult> Execute(string agentId, RoleDialogModel message,
        string? instruction = null, string? templateName = null,
        IEnumerable<InstructFileModel>? files = null,
        CodeInstructOptions? codeOptions = null,
        FileInstructOptions? fileOptions = null);

    /// <summary>
    /// A generic way to execute completion by using specified instruction or template
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="instruction">Prompt</param>
    /// <param name="agentId">Agent id</param>
    /// <param name="options">Llm Provider, model, message, prompt data</param>
    /// <returns></returns>
    Task<T?> Instruct<T>(string text, InstructOptions? options = null) where T : class;
}

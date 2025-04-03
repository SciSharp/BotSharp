using BotSharp.Abstraction.Instructs.Models;

namespace BotSharp.Abstraction.Instructs;

public interface IInstructService
{
    /// <summary>
    /// Execute completion by using specified instruction or template
    /// </summary>
    /// <param name="agentId">Agent (static agent)</param>
    /// <param name="message">Additional message provided by user</param>
    /// <param name="templateName">Template name</param>
    /// <param name="instruction">System prompt</param>
    /// <returns></returns>
    Task<InstructResult> Execute(string agentId, RoleDialogModel message,
        string? templateName = null, string? instruction = null, IEnumerable<InstructFileModel>? files = null);

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

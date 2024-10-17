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
    Task<InstructResult> Execute(string agentId, RoleDialogModel message, string? templateName = null, string? instruction = null);

    
    Task<T?> Instruct<T>(string instruction, string agentId, InstructOptions options) where T : class;
}

using BotSharp.Abstraction.Evaluations.Models;

namespace BotSharp.Abstraction.Evaluations;

public interface IEvaluatingService
{
    /// <summary>
    /// Execute task
    /// </summary>
    /// <param name="task">Task template name</param>
    /// <param name="request"></param>
    /// <returns>Conversation</returns>
    Task<Conversation> Execute(string task, EvaluationRequest request);

    /// <summary>
    /// Review result
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    Task<EvaluationResult> Review(string conversationId, EvaluationRequest request);

    /// <summary>
    /// Generate evaluation report
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    Task<EvaluationResult> Evaluate(string conversationId, EvaluationRequest request);
}

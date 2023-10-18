using BotSharp.Abstraction.Evaluations.Models;

namespace BotSharp.Abstraction.Evaluations;

public interface IEvaluatingService
{
    Task<EvaluationResult> Evaluate(EvaluationRequest request);
}

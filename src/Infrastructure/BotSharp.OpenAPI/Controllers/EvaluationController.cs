using BotSharp.Abstraction.Evaluations;
using BotSharp.Abstraction.Evaluations.Models;

namespace BotSharp.OpenAPI.Controllers;

[Authorize]
[ApiController]
public class EvaluationController : ControllerBase, IApiAdapter
{
    private readonly IServiceProvider _services;
    public EvaluationController(IServiceProvider services)
    {
        _services = services;
    }

    [HttpPost("/evaluation/execute/{task}")]
    public async Task<Conversation> Execute([FromRoute] string task, [FromBody] EvaluationRequest request)
    {
        var eval = _services.GetRequiredService<IEvaluatingService>();
        return await eval.Execute(task, request);
    }

    [HttpPost("/evaluation/review/{conversationId}")]
    public async Task<EvaluationResult> Review([FromRoute] string conversationId, [FromBody] EvaluationRequest request)
    {
        var eval = _services.GetRequiredService<IEvaluatingService>();
        return await eval.Review(conversationId, request);
    }

    [HttpPost("/evaluation/evaluate/{conversationId}")]
    public async Task<EvaluationResult> Evaluate([FromRoute] string conversationId, [FromBody] EvaluationRequest request)
    {
        var eval = _services.GetRequiredService<IEvaluatingService>();
        return await eval.Evaluate(conversationId, request);
    }
}

using BotSharp.Abstraction.ApiAdapters;
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

    [HttpPost("/evaluation")]
    public async Task<EvaluationResult> RunTask([FromBody] EvaluationRequest request)
    {
        var eval = _services.GetRequiredService<IEvaluatingService>();
        return await eval.Evaluate(request);
    }
}

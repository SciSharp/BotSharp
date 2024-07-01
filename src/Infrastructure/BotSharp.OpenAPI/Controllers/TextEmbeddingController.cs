using BotSharp.Core.Infrastructures;
using BotSharp.OpenAPI.ViewModels.Embeddings;

namespace BotSharp.OpenAPI.Controllers;

[Authorize]
[ApiController]
public class TextEmbeddingController : ControllerBase
{
    private readonly IServiceProvider _services;
    private readonly ILogger<TextEmbeddingController> _logger;

    public TextEmbeddingController(
        IServiceProvider services,
        ILogger<TextEmbeddingController> logger)
    {
        _services = services;
        _logger = logger;
    }

    [HttpPost("/text-embedding/generation")]
    public async Task<List<float[]>> GenerateTextEmbeddings(EmbeddingInputModel input)
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        input.States.ForEach(x => state.SetState(x.Key, x.Value, activeRounds: x.ActiveRounds, source: StateSource.External));

        try
        {
            var completion = CompletionProvider.GetTextEmbedding(_services, provider: input.Provider ?? "openai", model: input.Model ?? "text-embedding-3-small");
            completion.Dimension = input.Dimension;

            var embeddings = await completion.GetVectorsAsync(input.Texts?.ToList() ?? []);
            return embeddings;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error when generating text embeddings... {ex.Message}");
            throw;
        }
    }
}

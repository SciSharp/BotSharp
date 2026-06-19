namespace BotSharp.Abstraction.MLTasks;

public interface ITextEmbedding
{
    /// <summary>
    /// The Embedding provider like Microsoft Azure, OpenAI, ClaudAI
    /// </summary>
    string Provider { get; }
    string Model { get; }

    void SetModelName(string model);

    string? ApiKey => null;
    void SetApiKey(string apiKey) { }

    /// <summary>
    /// Optional task/prompt hint for prompt-conditioned ("asymmetric") embedding models such as
    /// EmbeddingGemma, which embed a search query and a stored document with different prompt prefixes.
    /// Pass <c>"query"</c> when embedding a search query and <c>"document"</c> when embedding content to
    /// be stored/indexed. Symmetric providers (e.g. OpenAI) ignore this. Defaults to <c>"document"</c>.
    /// </summary>
    void SetTaskType(string taskType) { }

    /// <summary>
    /// Generates an embedding vector for the given text.
    /// </summary>
    /// <param name="text">The text to embed.</param>
    /// <param name="dimension">
    /// Optional output dimension. When provided, it overrides the dimension set via
    /// <see cref="SetDimension(int)"/> for this call; when null, the provider's current
    /// dimension (its <c>_dimension</c>) is used.
    /// </param>
    Task<float[]> GetVectorAsync(string text, int? dimension = null);
    /// <summary>
    /// Generates embedding vectors for a batch of texts.
    /// </summary>
    /// <param name="texts">The texts to embed.</param>
    /// <param name="dimension">
    /// Optional output dimension. When provided, it overrides the dimension set via
    /// <see cref="SetDimension(int)"/> for this call; when null, the provider's current
    /// dimension is used.
    /// </param>
    Task<List<float[]>> GetVectorsAsync(List<string> texts, int? dimension = null);
    
    void SetDimension(int dimension);
    int GetDimension();
}

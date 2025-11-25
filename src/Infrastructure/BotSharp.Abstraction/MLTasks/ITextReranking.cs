namespace BotSharp.Abstraction.MLTasks;

/// <summary>
/// Interface for text reranking services.
/// Reranking is used to improve search results by scoring query-document pairs
/// using more sophisticated models (e.g., cross-encoders, vector embeddings) than initial retrieval.
/// </summary>
public interface ITextReranking
{
    /// <summary>
    /// The provider name (e.g., "google-ai", "cohere", "jina", "local").
    /// </summary>
    string Provider { get; }

    /// <summary>
    /// The model name (e.g., "gemma3-reranker", "rerank-english-v2.0", "jina-reranker-v1").
    /// </summary>
    string Model { get; }

    /// <summary>
    /// Rerank multiple documents for a given query and return their relevance scores.
    /// </summary>
    /// <param name="query">The search query</param>
    /// <param name="documents">List of document texts to score</param>
    /// <returns>List of relevance scores in the same order as input documents</returns>
    Task<List<float>> GetRerankScoresAsync(string query, List<string> documents);

    /// <summary>
    /// Set the model name to use for reranking.
    /// </summary>
    /// <param name="model">Model name</param>
    void SetModelName(string model);
}


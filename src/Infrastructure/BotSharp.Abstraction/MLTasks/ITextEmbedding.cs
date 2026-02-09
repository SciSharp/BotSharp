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

    Task<float[]> GetVectorAsync(string text);
    Task<List<float[]>> GetVectorsAsync(List<string> texts);
    
    void SetDimension(int dimension);
    int GetDimension();
}

namespace BotSharp.Abstraction.MLTasks;

public interface ITextEmbedding
{
    /// <summary>
    /// The Embedding provider like Microsoft Azure, OpenAI, ClaudAI
    /// </summary>
    string Provider { get; }
    Task<float[]> GetVectorAsync(string text);
    Task<List<float[]>> GetVectorsAsync(List<string> texts);
    void SetModelName(string model);
    void SetDimension(int dimension);
    int GetDimension();
}

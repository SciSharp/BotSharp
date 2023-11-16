using System.Threading;

namespace BotSharp.Abstraction.MLTasks;

public interface ITextEmbedding
{
    int Dimension { get; }
    Task<float[]> GetVectorAsync(string text);
    Task<List<float[]>> GetVectorsAsync(List<string> texts);
}

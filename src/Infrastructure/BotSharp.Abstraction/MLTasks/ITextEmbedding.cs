namespace BotSharp.Abstraction.MLTasks;

public interface ITextEmbedding
{
    int Dimension { get; }
    float[] GetVector(string text);
    List<float[]> GetVectors(List<string> texts);
}

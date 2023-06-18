namespace BotSharp.Abstraction.MLTasks;

public interface ITextEmbedding
{
    int Dimension { get; }
    float[] GetVector(string text);
}

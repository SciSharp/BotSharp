namespace BotSharp.Abstraction.MLTasks;

public interface ITextEmbedding
{
    float[] GetVector(string text);
}

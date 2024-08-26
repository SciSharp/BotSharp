using System.IO;

namespace BotSharp.Abstraction.MLTasks;

public interface ISpeechToText
{
    string Provider { get; }

    Task<string> GenerateTextFromAudioAsync(string filePath);
    Task<string> GenerateTextFromAudioAsync(Stream audio, string audioFileName);
    Task SetModelName(string model);
}

using System.IO;

namespace BotSharp.Abstraction.MLTasks;

public interface IAudioCompletion
{
    string Provider { get; }

    Task<string> GenerateTextFromAudioAsync(Stream audio, string audioFileName, string? text = null);
    Task<BinaryData> GenerateSpeechFromTextAsync(string text);

    void SetModelName(string model);
}

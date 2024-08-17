namespace BotSharp.Abstraction.MLTasks;

public interface ISpeechToText
{
    string Provider { get; }

    Task<string> GenerateTextFromAudioAsync(string filePath);
    // Task<string> AudioToTextTranscript(Stream stream);
    Task SetModelName(string modelType);
}

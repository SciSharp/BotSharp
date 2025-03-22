using System.IO;

namespace BotSharp.Abstraction.MLTasks;

/// <summary>
/// Audio transcription service
/// </summary>
public interface IAudioTranscription
{
    string Provider { get; }

    string Model { get; }

    Task<string> TranscriptTextAsync(Stream audio, string audioFileName, string? text = null);

    void SetModelName(string model);
}

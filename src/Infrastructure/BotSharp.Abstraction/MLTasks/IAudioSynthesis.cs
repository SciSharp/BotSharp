namespace BotSharp.Abstraction.MLTasks;

/// <summary>
/// Text to speech synthesis
/// </summary>
public interface IAudioSynthesis
{
    string Provider { get; }

    string Model { get; }

    void SetModelName(string model);

    Task<BinaryData> GenerateAudioAsync(string text, string? voice = "alloy", string? format = "mp3", string? instructions = null);
}

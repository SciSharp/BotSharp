using System.IO;

namespace BotSharp.Core.Files.Services;

public partial class FileInstructService
{
    public async Task<string> ReadAudio(string? provider, string? model, BotSharpFile audio)
    {
        var completion = CompletionProvider.GetSpeechToText(_services, provider: provider ?? "openai", model: model ?? "whisper-1");
        var audioBytes = await DownloadFile(audio);
        using var stream = new MemoryStream();
        stream.Write(audioBytes, 0, audioBytes.Length);
        stream.Position = 0;

        var content = await completion.GenerateTextFromAudioAsync(stream, audio.FileName ?? string.Empty);
        stream.Close();
        return content;
    }
}

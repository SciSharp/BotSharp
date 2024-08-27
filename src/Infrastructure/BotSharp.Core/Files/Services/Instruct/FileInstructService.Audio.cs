using System.IO;

namespace BotSharp.Core.Files.Services;

public partial class FileInstructService
{
    public async Task<string> ReadAudio(string? provider, string? model, InstructFileModel audio)
    {
        var completion = CompletionProvider.GetSpeechToText(_services, provider: provider ?? "openai", model: model ?? "whisper-1");
        var audioBytes = await DownloadFile(audio);
        using var stream = new MemoryStream();
        stream.Write(audioBytes, 0, audioBytes.Length);
        stream.Position = 0;

        var fileName = $"{audio.FileName ?? "audio"}.{audio.FileExtension ?? "wav"}";
        var content = await completion.GenerateTextFromAudioAsync(stream, fileName);
        stream.Close();
        return content;
    }
}

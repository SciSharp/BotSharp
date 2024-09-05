using System.IO;

namespace BotSharp.Core.Files.Services;

public partial class FileInstructService
{
    public async Task<string> SpeechToText(string? provider, string? model, InstructFileModel audio, string? text = null)
    {
        var completion = CompletionProvider.GetAudioCompletion(_services, provider: provider ?? "openai", model: model ?? "whisper-1");
        var audioBytes = await DownloadFile(audio);
        using var stream = new MemoryStream();
        stream.Write(audioBytes, 0, audioBytes.Length);
        stream.Position = 0;

        var fileName = $"{audio.FileName ?? "audio"}.{audio.FileExtension ?? "wav"}";
        var content = await completion.GenerateTextFromAudioAsync(stream, fileName, text);
        stream.Close();
        return content;
    }
}

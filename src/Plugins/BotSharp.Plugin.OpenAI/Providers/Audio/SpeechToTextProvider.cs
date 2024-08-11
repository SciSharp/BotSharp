using OpenAI.Audio;

namespace BotSharp.Plugin.OpenAI.Providers.Audio;

public class SpeechToTextProvider : ISpeechToText
{
    public Task<string> AudioToTextTranscript(string filePath)
    {
        throw new NotImplementedException();
    }

    public void SetModelType(string modelType)
    {
        throw new NotImplementedException();
    }
}

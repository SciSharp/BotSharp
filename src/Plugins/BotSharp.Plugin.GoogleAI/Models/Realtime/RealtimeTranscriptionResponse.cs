using System.IO;

namespace BotSharp.Plugin.GoogleAI.Models.Realtime;

internal class RealtimeTranscriptionResponse : IDisposable
{
    public RealtimeTranscriptionResponse()
    {
        
    }

    private MemoryStream _contentStream = new();
    public Stream? ContentStream
    {
        get
        {
            return _contentStream != null ? _contentStream : new MemoryStream();
        }
    }

    public void Collect(string text)
    {
        var binary = BinaryData.FromString(text);
        var bytes = binary.ToArray();

        _contentStream.Position = _contentStream.Length;
        _contentStream.Write(bytes, 0, bytes.Length);
        _contentStream.Position = 0;
    }

    public string GetText()
    {
        if (_contentStream.Length == 0)
        {
            return string.Empty;
        }

        var bytes = _contentStream.ToArray();
        var text = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        return text;
    }

    public void Clear()
    {
        _contentStream.SetLength(0);
        _contentStream.Position = 0;
    }

    public void Dispose()
    {
        _contentStream?.Dispose();
    }
}

using System.IO;

namespace BotSharp.Plugin.GoogleAI.Models.Realtime;

internal class RealtimeTranscriptionResponse : IDisposable
{
    public RealtimeTranscriptionResponse()
    {
        
    }

    private bool _disposed = false;

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
        if (_disposed) return;

        var binary = BinaryData.FromString(text);
        var bytes = binary.ToArray();

        _contentStream.Position = _contentStream.Length;
        _contentStream.Write(bytes, 0, bytes.Length);
        _contentStream.Position = 0;
    }

    public string GetText()
    {
        if (_disposed || _contentStream.Length == 0)
        {
            return string.Empty;
        }

        var bytes = _contentStream.ToArray();
        var text = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        return text;
    }

    public void Clear()
    {
        try
        {
            if (_disposed) return;

            _contentStream.Position = 0;
            _contentStream.SetLength(0);
        }
        catch { }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _contentStream?.Dispose();
    }
}

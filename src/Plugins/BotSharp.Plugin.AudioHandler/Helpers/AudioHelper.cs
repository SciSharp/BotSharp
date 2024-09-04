using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace BotSharp.Plugin.AudioHandler.Helpers;

public static class AudioHelper
{
    private const int DEFAULT_SAMPLE_RATE = 16000;

    public static Stream ConvertToStream(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            throw new ArgumentNullException("fileName is Null when converting to stream in audio processor");
        }

        var fileExtension = Path.GetExtension(fileName).ToLower().TrimStart('.');
        if (!Enum.TryParse(fileExtension, out AudioType fileType))
        {
            throw new NotSupportedException($"File extension: '{fileExtension}' is not supported!");
        }

        var stream = fileType switch
        {
            AudioType.mp3 => ConvertMp3ToStream(fileName),
            _ => ConvertWavToStream(fileName)
        };

        return stream;
    }

    public static Stream Transform(Stream stream, string fileName)
    {
        var fileExtension = Path.GetExtension(fileName).ToLower().TrimStart('.');
        if (!Enum.TryParse(fileExtension, out AudioType fileType))
        {
            throw new NotSupportedException($"File extension: '{fileExtension}' is not supported!");
        }

        Stream resultStream = new MemoryStream();
        stream.CopyTo(resultStream);
        resultStream.Seek(0, SeekOrigin.Begin);

        WaveStream reader = fileType switch
        {
            AudioType.mp3 => new Mp3FileReader(resultStream),
            _ => new WaveFileReader(resultStream)
        };

        resultStream = ChangeSampleRate(reader);
        reader.Close();
        return resultStream;
    }

    private static Stream ConvertMp3ToStream(string fileName)
    {
        using var fileStream = File.OpenRead(fileName);
        using var reader = new Mp3FileReader(fileStream);
        return ChangeSampleRate(reader);
    }

    private static Stream ConvertWavToStream(string fileName)
    {
        using var fileStream = File.OpenRead(fileName);
        using var reader = new WaveFileReader(fileStream);
        return ChangeSampleRate(reader);
    }

    private static Stream ChangeSampleRate(WaveStream ws)
    {
        var ms = new MemoryStream();
        if (ws.WaveFormat.SampleRate != DEFAULT_SAMPLE_RATE)
        {
            var resampler = new WdlResamplingSampleProvider(ws.ToSampleProvider(), DEFAULT_SAMPLE_RATE);
            WaveFileWriter.WriteWavFileToStream(ms, resampler.ToWaveProvider16());
        }
        else
        {
            ws.CopyTo(ms);
        }

        ms.Seek(0, SeekOrigin.Begin);
        return ms;
    }
}

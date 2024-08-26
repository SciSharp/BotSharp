using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace BotSharp.Plugin.AudioHandler.Helpers;

public class AudioHelper : IAudioHelper
{
    private readonly IServiceProvider _services;
    private readonly ILogger<AudioHelper> _logger;

    public AudioHelper(
        IServiceProvider services,
        ILogger<AudioHelper> logger)
    {
        _services = services;
        _logger = logger;
    }

    public Stream ConvertToStream(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            var error = "fileName is Null when converting to stream in audio processor";
            _logger.LogWarning(error);
            throw new ArgumentNullException(error);
        }

        var fileExtension = Path.GetExtension(fileName).ToLower().TrimStart('.');
        if (!Enum.TryParse(fileExtension, out AudioType fileType))
        {
            var error = $"File extension: '{fileExtension}' is not supported!";
            _logger.LogWarning(error);
            throw new NotSupportedException(error);
        }

        var stream = fileType switch
        {
            AudioType.mp3 => ConvertMp3ToStream(fileName),
            AudioType.wav => ConvertWavToStream(fileName),
            _ => throw new NotSupportedException("File extension not supported"),
        };

        return stream;
    }


    private Stream ConvertMp3ToStream(string fileName)
    {
        var fileStream = File.OpenRead(fileName);
        using var reader = new Mp3FileReader(fileStream);
        if (reader.WaveFormat.SampleRate != 16000)
        {
            var wavStream = new MemoryStream();
            var resampler = new WdlResamplingSampleProvider(reader.ToSampleProvider(), 16000);
            WaveFileWriter.WriteWavFileToStream(wavStream, resampler.ToWaveProvider16());
            wavStream.Seek(0, SeekOrigin.Begin);
            return wavStream;
        }

        fileStream.Seek(0, SeekOrigin.Begin);
        return fileStream;
    }

    private Stream ConvertWavToStream(string fileName)
    {
        var fileStream = File.OpenRead(fileName);
        using var reader = new WaveFileReader(fileStream);
        if (reader.WaveFormat.SampleRate != 16000)
        {
            var wavStream = new MemoryStream();
            var resampler = new WdlResamplingSampleProvider(reader.ToSampleProvider(), 16000);
            WaveFileWriter.WriteWavFileToStream(wavStream, resampler.ToWaveProvider16());
            wavStream.Seek(0, SeekOrigin.Begin);
            return wavStream;
        }

        fileStream.Seek(0, SeekOrigin.Begin);
        return fileStream;
    }
}

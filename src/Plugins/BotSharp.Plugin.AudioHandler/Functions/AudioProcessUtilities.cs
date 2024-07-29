using BotSharp.Plugin.AudioHandler.Enums;
using NAudio;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace BotSharp.Plugin.AudioHandler.Functions;

public class AudioProcessUtilities : IAudioProcessUtilities
{
    public AudioProcessUtilities()
    {
    }

    public Stream ConvertMp3ToStream(string mp3FileName)
    {
        var fileStream = File.OpenRead(mp3FileName);
        var reader = new Mp3FileReader(fileStream);
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

    public Stream ConvertWavToStream(string wavFileName)
    {
        var fileStream = File.OpenRead(wavFileName);
        var reader = new WaveFileReader(fileStream);
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

    public Stream ConvertToStream(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            throw new ArgumentNullException("fileName is Null");
        }
        string fileExtension = Path.GetExtension(fileName).ToLower().TrimStart('.');
        if (!Enum.TryParse<AudioType>(fileExtension, out AudioType fileType))
        {
            throw new NotSupportedException($"File extension: '{fileExtension}' not supported");
        }

        var stream = fileType switch
        {
            AudioType.mp3 => ConvertMp3ToStream(fileName),
            AudioType.wav => ConvertWavToStream(fileName),
            _ => throw new NotSupportedException("File extension not supported"),
        };

        return stream;
    }
}

using NAudio.Wave;

namespace BotSharp.Test.RealtimeVoice.Audio;

internal class AudioOut : IDisposable
{
    private const int SAMPLE_RATE = 24000;
    private const int BYTES_PER_SAMPLE = 2;
    private const int CHANNELS = 1;
    private const int BUFFER_MINS = 10;

    private readonly BufferedWaveProvider _waveProvider;
    private readonly WaveOutEvent _waveOutEvent;

    private AudioOut()
    {
        var audioFormat = new WaveFormat(
            rate: SAMPLE_RATE,
            bits: BYTES_PER_SAMPLE * 8,
            channels: CHANNELS);

        _waveProvider = new BufferedWaveProvider(audioFormat)
        {
            BufferDuration = TimeSpan.FromMinutes(BUFFER_MINS),
            DiscardOnBufferOverflow = true
        };

        _waveOutEvent = new WaveOutEvent()
        {
            DeviceNumber = 0
        };
        _waveOutEvent.Init(_waveProvider);
        _waveOutEvent.Play();
    }

    public static AudioOut Init() => new();

    public void Enqueue(BinaryData data)
    {
        var buffer = data?.ToArray() ?? [];
        _waveProvider.AddSamples(buffer, 0, buffer.Length);
    }

    public void ClearBuffer()
    {
        _waveProvider.ClearBuffer();
    }

    public void Dispose()
    {
        _waveOutEvent?.Dispose();
    }
}

using System.Collections.Concurrent;
using BotSharp.Abstraction.Realtime.Enums;
using NAudio.Wave;

namespace BotSharp.Core.Realtime.Services;

public class WaveStreamChannel : IStreamChannel
{
    private readonly IServiceProvider _services;
    private WaveInEvent _waveIn;
    private WaveOutEvent _waveOut;
    private BufferedWaveProvider _bufferedWaveProvider;
    private readonly ConcurrentQueue<byte[]> _audioBufferQueue = [];
    private readonly ILogger _logger;

    public WaveStreamChannel(IServiceProvider services, ILogger<WaveStreamChannel> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task ConnectAsync(string conversationId)
    {
        // Initialize the WaveInEvent
        _waveIn = new WaveInEvent
        {
            DeviceNumber = 0, // Default recording device
            WaveFormat = new WaveFormat(16000, 16, 1), // 24000 Hz, 16-bit PCM, Mono
            BufferMilliseconds = 100
        };

        // Set up the DataAvailable event handler
        _waveIn.DataAvailable += WaveIn_DataAvailable;

        // Start recording
        _waveIn.StartRecording();
        
        // Initialize audio output for streaming
        var waveFormat = new WaveFormat(24000, 16, 1); // 24000 Hz, 16-bit PCM, Mono
        _bufferedWaveProvider = new BufferedWaveProvider(waveFormat);
        _bufferedWaveProvider.BufferDuration = TimeSpan.FromMinutes(10);
        _bufferedWaveProvider.DiscardOnBufferOverflow = true;

        _waveOut = new WaveOutEvent()
        {
            DeviceNumber = 0
        };
        _waveOut.Init(_bufferedWaveProvider);
        _waveOut.Play();
    }

    public async Task<StreamReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellation)
    {
        // Poll the queue until data is available or cancellation is requested
        while (!cancellation.IsCancellationRequested)
        {
            // Try to dequeue audio data
            if (_audioBufferQueue.TryDequeue(out byte[]? audioData))
            {
                // Copy data to the provided buffer
                int bytesToCopy = Math.Min(audioData.Length, buffer.Count);
                Array.Copy(audioData, 0, buffer.Array, buffer.Offset, bytesToCopy);

                // Return the result
                return new StreamReceiveResult
                {
                    Status = StreamChannelStatus.Open,
                    Count = bytesToCopy
                };
            }
            
            // No data available yet, wait a short time before checking again
            await Task.Delay(10, cancellation);
        }
        
        // Cancellation was requested
        return new StreamReceiveResult();
    }

    public Task SendAsync(byte[] data, CancellationToken cancellation)
    {
        _logger.LogDebug($"Sending audio data of length {data.Length} to the stream channel.");
        // Add the incoming data to the buffer for continuous playback
        _bufferedWaveProvider.AddSamples(data, 0, data.Length);
        return Task.CompletedTask;
    }

    public void ClearBuffer()
    {
        _bufferedWaveProvider?.ClearBuffer();
        _audioBufferQueue?.Clear();
    }

    private void WaveIn_DataAvailable(object? sender, WaveInEventArgs e)
    {
        // Add the buffer to the queue
        _audioBufferQueue.Enqueue(e.Buffer);
    }

    public async Task CloseAsync(StreamChannelStatus status, string description, CancellationToken cancellation)
    {
        // Stop recording and clean up
        _waveIn?.StopRecording();
        _waveIn?.Dispose();
        _waveIn = null;

        // Stop playback and clean up
        _waveOut?.Stop();
        _waveOut?.Dispose();
        _waveOut = null;
    }
}

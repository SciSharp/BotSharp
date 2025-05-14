using System.Buffers;
using System.Text.Json;

namespace BotSharp.Test.RealtimeVoice.Session;

internal partial class ConsoleChatSession
{
    private readonly IServiceProvider _services;

    private ConsoleChatSession(
        IServiceProvider services)
    {
        _services = services;
    }

    public static ConsoleChatSession Init(IServiceProvider services)
    {
        return new(services);
    }

    /// <summary>
    /// Start a new session
    /// </summary>
    /// <param name="agentId"></param>
    /// <param name="mode"></param>
    /// <returns></returns>
    public async Task StartAsync(string agentId, SessionMode mode)
    {
        switch (mode)
        {
            case SessionMode.StreamChannel:
                await StartStreamChannelAsync(agentId);
                break;
            case SessionMode.CustomStream:
                await StartCustomStreamAsync(agentId);
                break;
        }
    }

    private void DisplayRemarks()
    {
        Console.WriteLine("PCM-16 Microphone Capture (24kHz Sample Rate)");
        Console.WriteLine("-----------------------------------------------");
    }

    private async Task SendAudio(IRealtimeHub hub, Stream stream)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(1024 * 16);

        try
        {
            while (true)
            {
                var bytesNum = await stream.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None);
                if (bytesNum == 0) break;

                var audioBytes = buffer.AsMemory(0, bytesNum);
                var data = BinaryData.FromBytes(audioBytes);
                await hub.Completer.AppenAudioBuffer(data.ToArray(), data.Length);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Create a new conversation and set up the events
    /// </summary>
    /// <param name="agentId"></param>
    /// <returns></returns>
    private async Task<(IRealtimeHub, string)> Setup(string agentId)
    {
        
        var convService = _services.GetRequiredService<IConversationService>();
        var hub = _services.GetRequiredService<IRealtimeHub>();

        var conv = new Conversation
        {
            AgentId = agentId,
            Channel = ConversationChannel.Phone,
            Title = $"Test",
            Tags = [],
        };
        conv = await convService.NewConversation(conv);

        var conn = hub.SetHubConnection(conv.Id);
        conn.CurrentAgentId = conv.AgentId;

        conn.OnModelReady = () =>
            JsonSerializer.Serialize(new
            {
                @event = "init"
            });

        conn.OnModelMessageReceived = message =>
            JsonSerializer.Serialize(new
            {
                @event = "media",
                media = message
            });

        conn.OnModelAudioResponseDone = () =>
            JsonSerializer.Serialize(new
            {
                @event = "mark",
                mark = new { name = "responsePart" }
            });

        conn.OnModelUserInterrupted = () =>
            JsonSerializer.Serialize(new
            {
                @event = "interrupted"
            });

        conn.OnUserSpeechDetected = () =>
            JsonSerializer.Serialize(new
            {
                @event = "speech_detected"
            });

        return (hub, conv.Id);
    }


    private int CalculateAudioLevel(byte[] buffer, int bytesRecorded)
    {
        // Simple audio level calculation (RMS)
        int bytesPerSample = 2; // 16-bit PCM = 2 bytes per sample
        int sampleCount = bytesRecorded / bytesPerSample;
        if (sampleCount == 0) return 0;

        double sum = 0;
        for (int i = 0; i < bytesRecorded; i += 2)
        {
            if (i + 1 < bytesRecorded)
            {
                short sample = (short)((buffer[i + 1] << 8) | buffer[i]);
                double normalized = sample / (short.MaxValue * 1.0 + 1);
                sum += normalized * normalized;
            }
        }

        double rms = Math.Sqrt(sum / sampleCount);
        double db = 20 * Math.Log10(rms);

        if (double.IsInfinity(db) || double.IsNaN(db))
        {
            return 0;
        }

        db = Math.Clamp(db, -100, 0);
        return (int)((db + 100) * 1);
    }

    private void DisplayAudioLevel(int level)
    {
        const int sep = 50;
        // Normalize level to 0-50 range for display
        int displayLevel = (level * sep) / 100;

        // Clear the current line
        Console.Write("\r" + new string(' ', 60));

        // Display audio level as a bar
        Console.Write("\rMicrophone: [");
        Console.Write(new string('#', displayLevel).PadRight(sep, ' '));
        Console.Write("]\r");
    }
}

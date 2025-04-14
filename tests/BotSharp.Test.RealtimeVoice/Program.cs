using BotSharp.Abstraction.Conversations.Enums;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Conversations;
using BotSharp.OpenAPI;
using System.Text.Json;
using System.Reflection;

var services = ServiceBuilder.CreateHostBuilder(Assembly.GetExecutingAssembly());
var channel = services.GetRequiredService<IStreamChannel>();

Console.WriteLine("PCM-16 Microphone Capture (24kHz Sample Rate)");
Console.WriteLine("-----------------------------------------------");

var convService = services.GetRequiredService<IConversationService>();
var conv = new Conversation
{
    AgentId = "01e2fc5c-2c89-4ec7-8470-7688608b496c",
    Channel = ConversationChannel.Phone,
    Title = $"Test",
    Tags = [],
};
conv = await convService.NewConversation(conv);

await channel.ConnectAsync(conv.Id);

var hub = services.GetRequiredService<IRealtimeHub>();
var conn = hub.SetHubConnection(conv.Id);

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


await hub.ConnectToModel(async data =>
{
    var response = JsonSerializer.Deserialize<ModelResponseEvent>(data);
    if (response.Event == "speech_detected")
    {
        channel.ClearBuffer();
    }
    else if (response.Event == "media")
    {
        var message = JsonSerializer.Deserialize<ModelResponseMediaEvent>(data);
        await channel.SendAsync(Convert.FromBase64String(message.Media), CancellationToken.None);
    }
});

StreamReceiveResult result;
var buffer = new byte[1024 * 8];

do
{
    var seg = new ArraySegment<byte>(buffer);
    result = await channel.ReceiveAsync(seg, CancellationToken.None);

    await hub.Completer.AppenAudioBuffer(seg, result.Count);

    // Display the audio level
    int audioLevel = CalculateAudioLevel(buffer, result.Count);
    DisplayAudioLevel(audioLevel);
} while (result.Status == StreamChannelStatus.Open);


int CalculateAudioLevel(byte[] buffer, int bytesRecorded)
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

void DisplayAudioLevel(int level)
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

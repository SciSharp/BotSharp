using BotSharp.Abstraction.Conversations.Enums;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Conversations;
using BotSharp.OpenAPI;
using System.Text.Json;
using Google.Ai.Generativelanguage.V1Beta2;

var services = ServiceBuilder.CreateHostBuilder();
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
var completer = hub.SetCompleter("openai");

await hub.ConnectToModel(async data =>
{
    var response = JsonSerializer.Deserialize<ModelResponseEvent>(data);
    if (response.Event == "media")
    {
        var message = JsonSerializer.Deserialize<ModelResponseMediaEvent>(data);
        await channel.SendAsync(Convert.FromBase64String(message.Media), CancellationToken.None);
    }
});

StreamReceiveResult result;
var buffer = new byte[1024 * 8];

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
        @event = "clear"
    });

do
{
    var seg = new ArraySegment<byte>(buffer);
    result = await channel.ReceiveAsync(seg, CancellationToken.None);

    await completer.AppenAudioBuffer(seg, result.Count);

    // Display the audio level
    int audioLevel = CalculateAudioLevel(buffer, result.Count);
    DisplayAudioLevel(audioLevel);
} while (result.Status == StreamChannelStatus.Open);

int CalculateAudioLevel(byte[] buffer, int bytesRecorded)
{
    // Simple audio level calculation (RMS)
    int sum = 0;
    for (int i = 0; i < bytesRecorded; i += 2)
    {
        if (i + 1 < bytesRecorded)
        {
            short sample = (short)((buffer[i + 1] << 8) | buffer[i]);
            sum += Math.Abs(sample);
        }
    }
    return bytesRecorded > 0 ? sum / (bytesRecorded / 2) : 0;
}

void DisplayAudioLevel(int level)
{
    // Normalize level to 0-50 range for display
    int displayLevel = Math.Min(50, level / 100);

    // Clear the current line
    Console.Write("\r" + new string(' ', 60));

    // Display audio level as a bar
    Console.Write("\rMicrophone: [");
    Console.Write(new string('#', displayLevel));
    Console.Write(new string(' ', 50 - displayLevel));
    Console.Write("]");
}

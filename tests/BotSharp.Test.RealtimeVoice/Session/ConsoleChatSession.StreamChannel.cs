using System.Text.Json;

namespace BotSharp.Test.RealtimeVoice.Session;

internal partial class ConsoleChatSession
{
    /// <summary>
    /// Start a new chat session via stream channel
    /// </summary>
    /// <param name="agentId"></param>
    /// <returns></returns>
    private async Task StartStreamChannelAsync(string agentId)
    {
        DisplayRemarks();

        var (hub, conversationId) = await Setup(agentId);

        var channel = _services.GetRequiredService<IStreamChannel>();
        await channel.ConnectAsync(conversationId);

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
        var buffer = new byte[1024 * 32];

        do
        {
            var seg = new ArraySegment<byte>(buffer);
            result = await channel.ReceiveAsync(seg, CancellationToken.None);

            await hub.Completer.AppenAudioBuffer(seg, result.Count);

            // Display the audio level
            int audioLevel = CalculateAudioLevel(buffer, result.Count);
            DisplayAudioLevel(audioLevel);
        } while (result.Status == StreamChannelStatus.Open);
    }
}

using System.Text.Json;

namespace BotSharp.Test.RealtimeVoice.Session;

internal partial class ConsoleChatSession
{
    /// <summary>
    /// Start a new chat session via custom stream
    /// </summary>
    /// <param name="agentId"></param>
    /// <returns></returns>
    private async Task StartCustomStreamAsync(string agentId)
    {
        DisplayRemarks();

        var (hub, conversationId) = await Setup(agentId);
        var audioOut = AudioOut.Init();

        await hub.ConnectToModel(
            responseToUser: async data =>
            {
                var response = JsonSerializer.Deserialize<ModelResponseEvent>(data);
                if (response.Event == "speech_detected")
                {
                    audioOut.ClearBuffer();
                }
                else if (response.Event == "media")
                {
                    var message = JsonSerializer.Deserialize<ModelResponseMediaEvent>(data);
                    var binaryData = BinaryData.FromBytes(Convert.FromBase64String(message.Media));
                    audioOut.Enqueue(binaryData);
                }
            },
            init: async data =>
            {
                _ = Task.Run(async () =>
                {
                    using var audioIn = AudioInStream.Init();
                    Console.WriteLine("\r\nListening microphone...\r\n");
                    await SendAudio(hub, audioIn);
                });
            });

        while (true) { }
    }
}

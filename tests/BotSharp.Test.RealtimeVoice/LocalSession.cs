using BotSharp.Abstraction.MLTasks;
using System.Buffers;
using System.ClientModel.Primitives;
using System.Threading;

namespace BotSharp.Test.RealtimeVoice;

public class LocalSession
{
    private readonly IRealTimeCompletion _completion;

    public LocalSession(
        IRealTimeCompletion completion)
    {
        _completion = completion;
    }

    public async Task SendInputAudioAsync(Stream audio)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(1024 * 16);
        while (true)
        {
            int bytesRead = await audio.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                break;
            }

            ReadOnlyMemory<byte> audioMemory = buffer.AsMemory(0, bytesRead);
            BinaryData audioData = BinaryData.FromBytes(audioMemory);;
            await _completion.AppenAudioBuffer(audioData.ToArray(), audioData.Length);
        }
    }
}

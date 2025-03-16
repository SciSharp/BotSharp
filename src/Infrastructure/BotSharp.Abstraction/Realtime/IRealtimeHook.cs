namespace BotSharp.Abstraction.Realtime;

public interface IRealtimeHook
{
    string[] OnModelTranscriptPrompt(Agent agent);
}

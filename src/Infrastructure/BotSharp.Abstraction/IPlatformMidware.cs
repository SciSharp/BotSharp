namespace BotSharp.Abstraction;

public interface IPlatformMidware
{
    Task GetChatCompletionsAsync(string text, Func<string, bool, Task> onChunkReceived);
}

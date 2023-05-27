namespace BotSharp.Abstraction;

public interface IChatCompletionHandler
{
    Task GetChatCompletionsAsync(string text, Func<string, bool, Task> onChunkReceived);
}

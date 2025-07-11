using BotSharp.Abstraction.Browsing.Models;

namespace BotSharp.Abstraction.Browsing;

public interface IWebDriverHook
{
    Task<List<string>> GetUploadFiles(MessageInfo message) => Task.FromResult(new List<string>());
    Task OnLocateElement(MessageInfo message, string content) => Task.CompletedTask;
}

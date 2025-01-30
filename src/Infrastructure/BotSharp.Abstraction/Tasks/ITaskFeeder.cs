using BotSharp.Abstraction.Tasks.Models;

namespace BotSharp.Abstraction.Tasks;

public interface ITaskFeeder
{
    Task<List<AgentTask>> GetTasks();
}

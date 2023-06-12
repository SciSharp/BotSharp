using BotSharp.Abstraction.Conversations.Models;

namespace BotSharp.Core.Conversations.ViewModels;

public class SessionCreationModel
{
    public string AgentId { get; set; }

    public Session ToSession()
    {
        return new Session
        {
            AgentId = AgentId
        };
    }
}

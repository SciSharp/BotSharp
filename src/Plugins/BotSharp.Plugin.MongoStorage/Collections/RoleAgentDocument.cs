using BotSharp.Abstraction.Roles.Models;

namespace BotSharp.Plugin.MongoStorage.Collections;

public class RoleAgentDocument : MongoBase
{
    public string RoleId { get; set; }
    public string AgentId { get; set; }
    public IEnumerable<string> Actions { get; set; } = [];
    public DateTime CreatedTime { get; set; }
    public DateTime UpdatedTime { get; set; }

    public RoleAgent ToRoleAgent()
    {
        return new RoleAgent
        {
            Id = Id,
            RoleId = RoleId,
            AgentId = AgentId,
            Actions = Actions,
            CreatedTime = CreatedTime,
            UpdatedTime = UpdatedTime
        };
    }
}

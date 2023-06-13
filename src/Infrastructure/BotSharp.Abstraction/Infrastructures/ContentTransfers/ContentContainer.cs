using BotSharp.Abstraction.Models;

namespace BotSharp.Abstraction.Infrastructures.ContentTransmitters;

public class ContentContainer
{
    public string UserId { get; set; }
    public string SessionId { get; set; }
    public string AgentId { get; set; }
    public List<RoleDialogModel> Conversations { get; set; }
    public RoleDialogModel Output { get; set; }
}

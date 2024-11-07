using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BotSharp.OpenAPI.ViewModels.Users;
public class UserDashboardModel
{

    [JsonPropertyName("conversation_list")]
    public IList<UserDashboardConversationModel> ConversationList { get; set; } = [];
}

public class UserDashboardConversationModel
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("conversation_id")]
    public string? ConversationId { get; set; }

    [JsonPropertyName("instruction")]
    public string? Instruction { get; set; }
}

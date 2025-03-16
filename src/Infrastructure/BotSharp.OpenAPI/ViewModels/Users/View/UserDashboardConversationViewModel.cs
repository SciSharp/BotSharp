using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Users;

public class UserDashboardViewModel
{
    [JsonPropertyName("conversation_list")]
    public IList<UserDashboardConversationViewModel> ConversationList { get; set; } = [];
}

public class UserDashboardConversationViewModel
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("conversation_id")]
    public string? ConversationId { get; set; }

    [JsonPropertyName("instruction")]
    public string? Instruction { get; set; }
}

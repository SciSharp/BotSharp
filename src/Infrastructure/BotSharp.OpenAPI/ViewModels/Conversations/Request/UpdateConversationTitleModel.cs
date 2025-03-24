using System.ComponentModel.DataAnnotations;

namespace BotSharp.OpenAPI.ViewModels.Conversations;

public class UpdateConversationTitleModel
{
    [Required]
    public string NewTitle { get; set; }
}

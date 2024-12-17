using System.ComponentModel.DataAnnotations;

namespace BotSharp.OpenAPI.ViewModels.Conversations;

public class UpdateConversationTitleAliasModel
{
    [Required]
    public string NewTitleAlias { get; set; }
}

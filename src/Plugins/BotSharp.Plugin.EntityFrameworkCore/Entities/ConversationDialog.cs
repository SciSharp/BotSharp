using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace BotSharp.Plugin.EntityFrameworkCore.Entities;

public class ConversationDialog
{
    public string Id { get; set; }
    public string ConversationId { get; set; }

    [Column(TypeName = "json")]
    public List<Dialog> Dialogs { get; set; }
}

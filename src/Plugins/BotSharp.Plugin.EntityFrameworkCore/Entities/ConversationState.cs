using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace BotSharp.Plugin.EntityFrameworkCore.Entities;

public class ConversationState
{
    public string Id { get; set; }
    public string ConversationId { get; set; }
    public List<State> States { get; set; } = new List<State>();

    [Column(TypeName = "json")]
    public List<BreakpointInfo> Breakpoints { get; set; } = new List<BreakpointInfo>();
}

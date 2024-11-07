using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Abstraction.Users.Models;

public class Dashboard
{
    public IList<DashboardConversation> ConversationList { get; set; } = [];
}

public class DashboardComponent
{
    public required string Id { get; set; }
    public string? Name { get; set; }
}

public class DashboardConversation : DashboardComponent
{
    public string? ConversationId { get; set; }
    public string? Instruction { get; set; } = "Default instruction: Ask bot to do something";
}


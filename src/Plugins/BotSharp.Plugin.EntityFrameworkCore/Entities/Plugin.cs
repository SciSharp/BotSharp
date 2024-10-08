using System.ComponentModel.DataAnnotations.Schema;

namespace BotSharp.Plugin.EntityFrameworkCore.Entities;

public class Plugin
{
    public string Id { get; set; }

    [Column(TypeName = "json")]
    public List<string> EnabledPlugins { get; set; }
}

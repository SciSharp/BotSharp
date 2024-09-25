using BotSharp.Abstraction.VectorStorage.Enums;

namespace BotSharp.Abstraction.VectorStorage.Models;

public class VectorCreateModel
{
    public string Text { get; set; }
    public string DataSource { get; set; } = VectorDataSource.Api;
    public Dictionary<string, string>? Payload { get; set; }
}

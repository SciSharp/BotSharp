using BotSharp.Abstraction.NER.Models;

namespace BotSharp.Abstraction.NER.Responses;

public class NERResponse : ResponseBase
{
    public List<NERResult> Results { get; set; } = [];
}

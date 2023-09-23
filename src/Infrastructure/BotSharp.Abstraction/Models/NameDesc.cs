namespace BotSharp.Abstraction.Models;

public class NameDesc
{
    public string Name { get; set; }
    public string Description { get; set; }

    public NameDesc(string name, string description) 
    { 
        Name = name;
        Description = description;
    }
}

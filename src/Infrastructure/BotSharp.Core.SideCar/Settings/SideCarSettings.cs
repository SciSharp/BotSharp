namespace BotSharp.Core.SideCar.Settings;

public class SideCarSettings
{
    public BaseSetting Conversation { get; set; }
}

public class BaseSetting
{
    public string Provider { get; set; }
}
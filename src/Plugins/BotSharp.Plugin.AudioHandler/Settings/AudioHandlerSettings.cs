using BotSharp.Abstraction.Models;

namespace BotSharp.Plugin.AudioHandler.Settings;

public class AudioHandlerSettings
{
    public AudioSettings? Audio { get; set; }
}

#region Audio
public class AudioSettings
{
    public AudioReadSettings? Reading { get; set; }
}

public class AudioReadSettings : LlmBase
{
}
#endregion
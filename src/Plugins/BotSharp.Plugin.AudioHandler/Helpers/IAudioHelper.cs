namespace BotSharp.Plugin.AudioHandler.Helpers;

public interface IAudioHelper
{
    Stream ConvertToStream(string fileName);
}
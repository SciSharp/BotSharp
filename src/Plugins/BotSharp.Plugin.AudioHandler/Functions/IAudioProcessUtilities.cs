
namespace BotSharp.Plugin.AudioHandler.Functions
{
    public interface IAudioProcessUtilities
    {
        Stream ConvertMp3ToStream(string mp3FileName);
        Stream ConvertWavToStream(string wavFileName);
        Stream ConvertToStream(string fileName);
    }
}
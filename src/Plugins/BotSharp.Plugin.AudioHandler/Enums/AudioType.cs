using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Whisper.net.Wave;

namespace BotSharp.Plugin.AudioHandler.Enums
{
    public enum AudioType
    {
        wav,
        mp3,
    }

    public static class AudioTypeExtensions
    {
        public static string ToFileExtension(this AudioType audioType) => $".{audioType}";
        public static string ToFileType(this AudioType audioType)
        {
            string type = audioType switch
            {
                AudioType.mp3 => "audio/mpeg",
                AudioType.wav => "audio/wav",
                _ => throw new NotImplementedException($"No support found for {audioType}")
            };
            return type;
        }
    }
}


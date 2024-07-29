using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BotSharp.Plugin.AudioHandler.Models;
using Whisper.net;
using Whisper.net.Ggml;


namespace BotSharp.Plugin.AudioHandler.Provider
{
    public interface IAudioService
    {
        Task LoadWhisperModel(GgmlType modelType);
        Task<string> AudioToTextTranscript(AudioInput audioInput);
    }
}

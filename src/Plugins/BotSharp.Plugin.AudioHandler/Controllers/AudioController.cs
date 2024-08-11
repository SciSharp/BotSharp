using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BotSharp.Plugin.AudioHandler.Models;
using BotSharp.Plugin.AudioHandler.Provider;

namespace BotSharp.Plugin.AudioHandler.Controllers
{
#if DEBUG
    [AllowAnonymous]
#endif
    [ApiController]
    public class AudioController : ControllerBase
    {
        private readonly ISpeechToText _nativeWhisperProvider;

        public AudioController(ISpeechToText nativeWhisperProvider)
        {
            _nativeWhisperProvider = nativeWhisperProvider;
        }

        [HttpGet("audio/transcript")]
        public async Task<IActionResult> GetTextFromAudioController(string audioInputString, string modelType = "")
        {
#if DEBUG
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
#endif
            if (!string.IsNullOrEmpty(audioInputString))
            {
                _nativeWhisperProvider.SetModelType(modelType);
            }

            var result = await _nativeWhisperProvider.AudioToTextTranscript(audioInputString);
#if DEBUG
            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            ts.Hours, ts.Minutes, ts.Seconds,
            ts.Milliseconds / 10);
            Console.WriteLine("RunTime " + elapsedTime);
#endif
            return Ok(result);
        }
    }
}

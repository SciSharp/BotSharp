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
    [Route("[controller]/text/[action]")]
    [ApiController]
    public class AudioController : ControllerBase
    {
        private readonly IAudioService _audioService;

        public AudioController(IAudioService audioService)
        {
            _audioService = audioService;
        }

        [HttpGet]
        public async Task<IActionResult> GetTextFromAudioController(string audioInputString)
        {
#if DEBUG
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
#endif
            var audioInput = new AudioInput
            {
                FilePath = audioInputString
            };
            var result = await _audioService.AudioToTextTranscript(audioInput);
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

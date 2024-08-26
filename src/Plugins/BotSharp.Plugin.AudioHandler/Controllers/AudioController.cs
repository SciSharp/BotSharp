using System.Diagnostics;
using BotSharp.Core.Infrastructures;

namespace BotSharp.Plugin.AudioHandler.Controllers
{
#if DEBUG
    [AllowAnonymous]
#endif
    [ApiController]
    public class AudioController : ControllerBase
    {
        private readonly ISpeechToText _nativeWhisperProvider;
        private readonly IServiceProvider _services;

        public AudioController(ISpeechToText nativeWhisperProvider, IServiceProvider service)
        {
            _nativeWhisperProvider = nativeWhisperProvider;
            _services = service;
        }

        [HttpGet("audio/transcript")]
        public async Task<IActionResult> GetTextFromAudioController(string audioInputString, string modelType = "")
        {
#if DEBUG
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
#endif
            await _nativeWhisperProvider.SetModelName(modelType);

            var result = await _nativeWhisperProvider.GenerateTextFromAudioAsync(audioInputString);
#if DEBUG
            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
            Console.WriteLine("RunTime " + elapsedTime);
#endif
            return Ok(result);
        }

        [HttpPost("openai/audio/transcript")]
        public async Task<IActionResult> GetTextFromAudioOpenAiController(string filePath)
        {
#if DEBUG
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
#endif
            var client = CompletionProvider.GetSpeechToText(_services, "openai", "whisper-1");
            var result = await client.GenerateTextFromAudioAsync(filePath);
#if DEBUG
            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
            Console.WriteLine("RunTime " + elapsedTime);
#endif
            return Ok(result);
        }
    }
}

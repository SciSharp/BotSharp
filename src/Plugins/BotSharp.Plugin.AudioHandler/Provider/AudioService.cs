using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.Runtime.SharedInterfaces;
using BotSharp.Plugin.AudioHandler.Enums;
using BotSharp.Plugin.AudioHandler.Models;
using BotSharp.Plugin.AudioHandler.Functions;
using Whisper;
using Whisper.net;
using Whisper.net.Ggml;

namespace BotSharp.Plugin.AudioHandler.Provider
{
    public class AudioService : IAudioService
    {
        private readonly IAudioProcessUtilities _audioProcessUtilities;
        private WhisperProcessor _processor;
        
        private string _modelName;

        public AudioService(IAudioProcessUtilities audioProcessUtilities)
        {
            _audioProcessUtilities = audioProcessUtilities;
        }

        public async Task LoadWhisperModel(GgmlType modelType)
        {
            try
            {
                _modelName = $"ggml-{modelType}.bin";

                if (!File.Exists(_modelName))
                {
                    using var modelStream = await WhisperGgmlDownloader.GetGgmlModelAsync(GgmlType.TinyEn);
                    using var fileWriter = File.OpenWrite(_modelName);
                    await modelStream.CopyToAsync(fileWriter);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load whisper model: {ex.Message}");
            }
        }

        public async Task<string> AudioToTextTranscript(AudioInput audioInput)
        {
            string fileExtension = Path.GetExtension(audioInput.FilePath);
            if (!Enum.TryParse<AudioType>(fileExtension.TrimStart('.').ToLower(), out AudioType audioType))
            {
                throw new Exception($"Unsupported audio type: {fileExtension}");
            }
            await InitModel();
            // var _streamHandler = _audioHandlerFactory.CreateAudioHandler(audioType);
            using var stream = _audioProcessUtilities.ConvertToStream(audioInput.FilePath);

            if (stream == null)
            {
                throw new Exception($"Failed to convert {fileExtension} to stream");
            }

            var textResult = new List<SegmentData>();

            await foreach (var result in _processor.ProcessAsync((Stream)stream).ConfigureAwait(false))
            {
                textResult.Add(result);
            }

            await stream.DisposeAsync();

            var audioOutput = new AudioOutput
            {
                Segments = textResult
            };

            return audioOutput.ToString();
        }

        private async Task InitModel(GgmlType modelType = GgmlType.TinyEn)
        {
            if (_processor == null)
            {

               await LoadWhisperModel(modelType);
                _processor = WhisperFactory
                    .FromPath(_modelName)
                    .CreateBuilder()
                    .WithLanguage("en")
                    .Build();
            }
        }
    }
}

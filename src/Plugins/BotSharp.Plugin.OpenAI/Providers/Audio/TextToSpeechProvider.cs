using OpenAI.Audio;

namespace BotSharp.Plugin.OpenAI.Providers.Audio
{
    public partial class TextToSpeechProvider : ITextToSpeech
    {
        private readonly IServiceProvider _services;

        public string Provider => "openai";
        private string? _model;

        public TextToSpeechProvider(
            IServiceProvider services)
        {
            _services = services;
        }

        public async Task<BinaryData> GenerateSpeechFromTextAsync(string text, ITextToSpeechOptions? options = null)
        {
            var client = ProviderHelper.GetClient(Provider, _model, _services)
                                       .GetAudioClient(_model);

            return await client.GenerateSpeechFromTextAsync(text, GeneratedSpeechVoice.Alloy);
        }

        public void SetModelName(string model)
        {
            _model = model;
        }
    }
}

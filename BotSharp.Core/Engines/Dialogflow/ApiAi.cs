using BotSharp.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BotSharp.Core.Engines.Dialogflow
{
    public class ApiAi : ApiAiBase, IBotPlatform
    {
        private readonly AIConfiguration config;
        private readonly AIDataService dataService;

        public ApiAi(AIConfiguration config)
        {
            this.config = config;

            dataService = new AIDataService(this.config);
        }

        public AIResponse TextRequest(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                throw new ArgumentNullException("text");
            }

            return TextRequest(new AIRequest(text));
        }

        public AIResponse TextRequest(AIRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            return dataService.Request(request);
        }

        public AIResponse TextRequest(string text, RequestExtras requestExtras)
        {
            if (string.IsNullOrEmpty(text))
            {
                throw new ArgumentNullException("text");
            }

            return TextRequest(new AIRequest(text, requestExtras));
        }

        public AIResponse VoiceRequest(Stream voiceStream, RequestExtras requestExtras = null)
        {
            if (config.Language == SupportedLanguage.Italian)
            {
                throw new AIServiceException("Sorry, but Italian language now is not supported in Speaktoit recognition. Please use some another speech recognition engine.");
            }

            return dataService.VoiceRequest(voiceStream, requestExtras);
        }
    }
}

using BotSharp.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BotSharp.Core.Engines.Dialogflow
{
    public class ApiAi : ApiAiBase, IBotPlatform
    {
        private AIDataService dataService;

        public AIResponse TextRequest(string text)
        {
            if (dataService == null)
            {
                dataService = new AIDataService(AiConfig);
            }

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

            if(dataService == null)
            {
                dataService = new AIDataService(AiConfig);
            }

            return dataService.Request(request);
        }

        public AIResponse TextRequest(string text, RequestExtras requestExtras)
        {
            if (string.IsNullOrEmpty(text))
            {
                throw new ArgumentNullException("text");
            }

            if (dataService == null)
            {
                dataService = new AIDataService(AiConfig);
            }

            return TextRequest(new AIRequest(text, requestExtras));
        }

        public void Train()
        {
            throw new NotImplementedException();
        }
    }
}

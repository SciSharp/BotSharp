using Platform.Dialogflow.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BotSharp.Core.Engines.Dialogflow
{
    public class ApiAi : ApiAiBase
    {
        private AIDataService dataService;

        public AIResponse TextRequest(AIRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            if(dataService == null)
            {
                // dataService = new AIDataService(AiConfig);
            }

            return dataService.Request(request);
        }

        public void Train()
        {
            
        }
    }
}

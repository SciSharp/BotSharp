using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.RestApi.Rasa
{
    public class RasaUiMiddlewareRequestDataModel
    {
        public string Project { get; set; }
        public string Agent { get; set; }
        public RasaTrainRequestModel Data { get; set; }
    }
}

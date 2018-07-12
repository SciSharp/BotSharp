using BotSharp.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Engines.Dialogflow
{
    public class AIServiceException : Exception
    {
        public AIResponse Response { get; set; }

        public AIServiceException()
        {
        }

        public AIServiceException(string message) : base(message)
        {
        }

        public AIServiceException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public AIServiceException(Exception e) : base(e.Message, e)
        {
        }

        public AIServiceException(AIResponse response)
        {
            Response = response;
        }

        public override string Message
        {
            get
            {
                if (Response != null && Response.IsError)
                {
                    if (!string.IsNullOrEmpty(Response.Status.ErrorDetails))
                    {
                        return Response.Status.ErrorDetails;
                    }
                }

                return base.Message;
            }
        }
    }
}

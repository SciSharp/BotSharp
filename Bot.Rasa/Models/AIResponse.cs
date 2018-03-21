using System;
using System.Collections.Generic;
using System.Text;

namespace Bot.Rasa.Models
{
    public class AIResponse
    {
        public string Id { get; set; }

        public DateTime Timestamp { get; set; }

        public string Lang { get; set; }

        public AIResponseResult Result { get; set; }

        public AIResponseStatus Status { get; set; }

        public string SessionId { get; set; }

        public bool IsError
        {
            get
            {
                if (Status != null && Status.Code >= 400)
                {
                    return true;
                }

                return false;
            }
        }
    }
}

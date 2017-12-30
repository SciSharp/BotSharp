using Bot.Rasa.Expressions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bot.Rasa.Models
{
    public class UserSay
    {
        public UserSay()
        {
            Entities = new List<EntitiyOfSpeech>();
        }

        public String Text { get; set; }
        public String Intent { get; set; }
        public List<EntitiyOfSpeech> Entities { get; set; }
    }
}

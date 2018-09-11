using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace BotSharp.RestApi.Dialogflow
{
    public class QueryModel
    {
        public QueryModel()
        {
            Contexts = new List<string>();
        }

        /// <summary>
        /// Array of additional context objects. Should be sent via a POST /query request. 
        /// Contexts sent in a query are activated before the query.
        /// </summary>
        public List<String> Contexts { get; set; }

        /// <summary>
        /// Object containing event name and additional data. 
        /// The "data" parameter can be submitted only in POST requests.
        /// </summary>
        public String Event { get;set; }

        /// <summary>
        /// Language tag, e.g., en, es etc.
        /// </summary>
        public String Lang { get; set; }

        /// <summary>
        /// Natural language text to be processed. Query length should not exceed 256 characters.
        /// </summary>
        [Required]
        public String Query { get; set; }

        /// <summary>
        /// A string token up to 36 symbols long, used to identify the client and to manage session parameters (including contexts) per client.
        /// </summary>
        [Required]
        public String SessionId { get; set; }

        /// <summary>
        /// Time zone from IANA Time Zone Database Examples: America/New_York, Europe/Paris
        /// </summary>
        public String Timezone { get; set; }
    }
}

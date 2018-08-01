using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace BotSharp.RestApi.Dialogs
{
    public class QueryModel
    {
        public List<String> Contexts { get; set; }

        public String Event { get;set; }

        public String Lang { get; set; }

        /// <summary>
        /// User says
        /// </summary>
        [Required]
        public String Query { get; set; }

        [Required]
        public String SessionId { get; set; }

        public String Timezone { get; set; }
    }
}

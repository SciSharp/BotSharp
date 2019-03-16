using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace BotSharp.Platform.Articulate.Models
{
    public class ScenarioModel
    {
        /// <summary>
        /// Guid
        /// </summary>
        [StringLength(36)]
        public String Id { get; set; }

        public string ScenarioName { get; set; }

        public List<String> IntentResponses { get; set; }

        public List<SlotModel> Slots { get; set; }
    }
}

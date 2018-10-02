using BotSharp.Core.Adapters.Dialogflow;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;

namespace Platform.Dialogflow.Controllers
{
#if DIALOGFLOW
    /// <summary>
    /// The /intents endpoint is used to create, retrieve, update, and delete intent objects.
    /// </summary>
    [Authorize]
    [Route("v1/[controller]")]
    public class IntentsController : ControllerBase
    {
        /// <summary>
        /// Retrieves a list of all intents for the agent.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IEnumerable<DialogflowIntent> GetIntents()
        {
            return new List<DialogflowIntent>();
        }

        /// <summary>
        /// The POST request creates a new intent. When a new intent is created via the API, the agent is trained with the new data.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public string PostIntent([FromBody] DialogflowIntent intent)
        {
            return "";
        }

        /// <summary>
        /// Retrieves the specified intent. {id} is the ID of the intent to retrieve.
        /// </summary>
        /// <returns></returns>
        [HttpGet("{id}")]
        public DialogflowIntent GetIntent()
        {
            return new DialogflowIntent();
        }

        [HttpPut("{id}")]
        public bool PutIntent([FromBody] DialogflowIntent intent)
        {
            return true;
        }
    }
#endif
}

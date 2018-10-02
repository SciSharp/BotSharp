using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Dialogflow.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace Platform.Dialogflow.Controllers
{
#if DIALOGFLOW
    /// <summary>
    /// The /entities endpoint is used to create, retrieve, update, and delete developer-defined entity objects.
    /// </summary>
    [Authorize]
    [Route("v1/[controller]")]
    public class EntitiesController : ControllerBase
    {
        [HttpGet]
        public IEnumerable<DialogflowAllEntityViewModel> All()
        {
            return new List<DialogflowAllEntityViewModel>();
        }
    }
#endif
}

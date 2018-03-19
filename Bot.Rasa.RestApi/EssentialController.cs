using Bot.Rasa.Consoles;
using DotNetToolkit;
using EntityFrameworkCore.BootKit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;

namespace Bot.Rasa.RestApi
{
    //[Authorize]
    [Produces("application/json")]
    [Route("bot/[controller]")]
    public class EssentialController : ControllerBase
    {
        protected Database dc { get; set; }

        public EssentialController()
        {
            dc = new DefaultDataContextLoader().GetDefaultDc();
        }

        [HttpPatch("{table}/{id}")]
        public IActionResult Patch([FromRoute] String table, [FromRoute] String id, [FromBody] JObject jObject)
        {
            var patch = new DbPatchModel
            {
                Table = table,
                Id = id,
                Values = jObject.ToDictionary()
            };

            dc.Patch<IDbRecord>(patch);

            return Ok();
        }
    }
}

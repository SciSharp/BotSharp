using Bot.Rasa.Console;
using DotNetToolkit;
using EntityFrameworkCore.BootKit;
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
#if !DEBUG
    [Authorize]
#endif
    [Produces("application/json")]
    [Route("bot/[controller]")]
    public class EssentialController : ControllerBase
    {
        protected Database dc { get; set; }

        public EssentialController()
        {
            dc = new Database();

            string db = RasaConsole.Options.DbName;
            string connectionString = RasaConsole.Options.DbConnectionString;

            if (db.Equals("SqlServer"))
            {
                dc.BindDbContext<IDbRecord, DbContext4SqlServer>(new DatabaseBind
                {
                    MasterConnection = new SqlConnection(connectionString),
                    CreateDbIfNotExist = true,
                    AssemblyNames = RasaConsole.Options.Assembles
                });
            }
            else if (db.Equals("Sqlite"))
            {
                connectionString = connectionString.Replace("|DataDirectory|\\", RasaConsole.Options.ContentRootPath + "\\App_Data\\");
                dc.BindDbContext<IDbRecord, DbContext4Sqlite>(new DatabaseBind
                {
                    MasterConnection = new SqliteConnection(connectionString),
                    CreateDbIfNotExist = true,
                    AssemblyNames = RasaConsole.Options.Assembles
                });
            }
            else if (db.Equals("MySql"))
            {
                dc.BindDbContext<IDbRecord, DbContext4MySql>(new DatabaseBind
                {
                    MasterConnection = new MySqlConnection(connectionString),
                    CreateDbIfNotExist = true,
                    AssemblyNames = RasaConsole.Options.Assembles
                });
            }
            else if (db.Equals("InMemory"))
            {
                dc.BindDbContext<IDbRecord, DbContext4Memory>(new DatabaseBind
                {
                    AssemblyNames = RasaConsole.Options.Assembles
                });
            }
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

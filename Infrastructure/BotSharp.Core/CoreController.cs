using EntityFrameworkCore.BootKit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Core
{
    [Authorize]
    [Produces("application/json")]
    [Route("v1/[controller]")]
    public class CoreController : ControllerBase
    {
        protected Database dc { get; set; }

        public CoreController()
        {
            dc = new DefaultDataContextLoader().GetDefaultDc();
        }

        /*protected string GetConfig(string path)
        {
            if (string.IsNullOrEmpty(path)) return String.Empty;

            return Database.Configuration.GetSection(path).Value;
        }

        protected List<KeyValuePair<string, string>> GetSection(string path)
        {
            return Database.Configuration.GetSection(path).AsEnumerable().ToList();
        }*/

        protected string CurrentUserId
        {
            get
            {
                return this.User.Claims.FirstOrDefault(x => x.Type.Equals("UserId")).Value;
            }
        }
    }
}

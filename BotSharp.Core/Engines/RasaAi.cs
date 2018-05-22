using BotSharp.Core.Agents;
using BotSharp.Core.Entities;
using BotSharp.Core.Models;
using EntityFrameworkCore.BootKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BotSharp.Core.Engines
{
    /// <summary>
    /// Rasa nlu 0.11.x
    /// </summary>
    public class RasaAi : IBotEngine
    {
        public Database dc { get; set; }
        public AIConfiguration AiConfig { get; set; }
        public static IConfiguration Configuration { get; set; }

        public Agent agent { get; set; }

        public RasaAi(Database dc)
        {
            this.dc = dc;
        }

        public RasaAi(Database dc, AIConfiguration aiConfig)
        {
            this.dc = dc;

            AiConfig = aiConfig;
            agent = this.LoadAgent(dc, aiConfig);
            aiConfig.DevMode = agent.DeveloperAccessToken == aiConfig.ClientAccessToken;
        }
    }
}

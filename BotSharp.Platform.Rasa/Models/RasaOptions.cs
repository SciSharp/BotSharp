using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Platform.Rasa.Models
{
    public class RasaOptions
    {
        public string HostUrl { get; set; }
        public String[] Assembles { get; set; }
        public string ContentRootPath { get; set; }
        public String DbName { get; set; }
        public String DbConnectionString { get; set; }
    }
}

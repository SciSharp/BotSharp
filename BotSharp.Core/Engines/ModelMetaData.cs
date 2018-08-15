using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Engines
{
    public class ModelMetaData
    {
        public string Platform { get; set; }
        public string Language { get; set; }

        public string Version { get; set; }
        public DateTime TrainingDate { get; set; }

        /// <summary>
        /// Model file fullpath
        /// </summary>
        [JsonIgnore]
        public string Model { get; set; }

        public List<PipeModel> Pipeline { get; set; }
    }
}

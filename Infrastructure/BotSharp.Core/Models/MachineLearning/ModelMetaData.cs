using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Platform.Models.MachineLearning
{
    public class ModelMetaData
    {
        public string Platform { get; set; }
        public string BotEngine { get; set; }
        public string Language { get; set; }

        public string Version { get; set; }
        public DateTime TrainingDate { get; set; }

        /// <summary>
        /// Model file fullpath
        /// </summary>
        public string Model { get; set; }

        public List<PipeModel> Pipeline { get; set; }
    }
}

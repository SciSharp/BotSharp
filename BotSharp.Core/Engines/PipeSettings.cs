using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BotSharp.Core.Engines
{
    public class PipeSettings
    {
        public string ProjectDir { get; set; }
        public string ModelDir { get; set; }
        public string AlgorithmDir { get; set; }
        public string TempDir
        {
            get
            {
                return Path.Join(ProjectDir, "Temp");
            }
        }
    }
}

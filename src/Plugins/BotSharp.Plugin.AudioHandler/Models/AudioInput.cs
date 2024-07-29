using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BotSharp.Plugin.AudioHandler.Enums;

namespace BotSharp.Plugin.AudioHandler.Models
{
    public class AudioInput
    {
        public string FilePath { get; set; }
        public Stream Stream { get; set; }
    }
}

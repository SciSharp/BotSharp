using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Whisper.net;

namespace BotSharp.Plugin.AudioHandler.Models
{
    public class AudioOutput
    {
        public List<SegmentData> Segments { get; set; }

        public override string ToString()
        {
            return this.Segments.Count > 0 ? string.Join(" ", this.Segments.Select(x => x.Text)) : string.Empty;
        }
    }
}

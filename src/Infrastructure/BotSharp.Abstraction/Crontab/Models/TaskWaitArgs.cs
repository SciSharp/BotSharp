using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Abstraction.Crontab.Models;

public class TaskWaitArgs
{

    [JsonPropertyName("delay_time")]
    public int DelayTime { get; set; }
}

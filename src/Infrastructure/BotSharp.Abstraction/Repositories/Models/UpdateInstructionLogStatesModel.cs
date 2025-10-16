using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Abstraction.Repositories.Models
{
    public class UpdateInstructionLogStatesModel
    {
        public string LogId { get; set; }
        public string StateKeyPrefix { get; set; } = "new_";
        public Dictionary<string, string> States { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Engines.Articulate
{
    public class SlotModel
    {
        public string Entity { get; set; }

        public bool IsList { get; set; }

        public bool IsRequired { get; set; }

        public string SlotName { get; set; }

        public List<String> TextPrompts { get; set; }
    }
}

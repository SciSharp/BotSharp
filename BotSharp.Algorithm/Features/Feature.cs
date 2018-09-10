using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Algorithm.Features
{
    public class Feature
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public Feature(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }
}

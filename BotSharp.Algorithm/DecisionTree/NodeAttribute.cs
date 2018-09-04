using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace BotSharp.Algorithm.DecisionTree
{
    public class NodeAttribute
    {
        public NodeAttribute(string name, List<string> differentAttributenames)
        {
            Name = name;
            DifferentAttributeNames = differentAttributenames;
        }

        public string Name { get; }

        public List<string> DifferentAttributeNames { get; }

        public double InformationGain { get; set; }
    }
}
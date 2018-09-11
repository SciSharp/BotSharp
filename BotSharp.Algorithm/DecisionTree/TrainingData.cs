using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Algorithm.DecisionTree
{
    public class TrainingData
    {
        public String[] Columns { get; set; }

        public String[][] Rows { get; set; }

        public int Index(string column)
        {
            for(int i = 0; i < Columns.Length; i++)
            {
                if (Columns[i] == column) return i;
            }

            return -1;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.DecisionTree
{
    public class DataReader
    {
        public TrainingData Read(string filePath)
        {
            var data = new TrainingData { };
            string[] lines = File.ReadAllLines(filePath);
            data.Rows = new string[lines.Length - 1][];

            for(int rowIndex = 0; rowIndex < lines.Length; rowIndex++)
            {
                string line = lines[rowIndex];
                // Columns
                if (rowIndex == 0)
                {
                    data.Columns = line.Split(';').Where(x => !String.IsNullOrEmpty(x)).ToArray();
                }
                else
                {
                    data.Rows[rowIndex - 1] = line.Split(';').Where(x => !String.IsNullOrEmpty(x)).ToArray();
                }
            }

            return data;
        }
    }
}

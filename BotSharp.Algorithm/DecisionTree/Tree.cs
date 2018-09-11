using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace BotSharp.Algorithm.DecisionTree
{
    public class Tree
    {
        public TreeNode Root { get; set; }

        public static void Print(TreeNode node, string result)
        {
            if (node?.ChildNodes == null || node.ChildNodes.Count == 0)
            {
                var seperatedResult = result.Split(' ');

                foreach (var item in seperatedResult)
                {
                    if (item.Equals(seperatedResult[0]))
                    {
                        Console.ForegroundColor = ConsoleColor.Magenta;
                    }
                    else if (item.Equals("--") || item.Equals("-->"))
                    {
                        // empty if but better than checking at .ToUpper() and .ToLower() if
                    }
                    else if (item.Equals("YES") || item.Equals("NO"))
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                    }
                    else if (item.ToUpper().Equals(item))
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                    }

                    Console.Write($"{item} ");
                    Console.ResetColor();
                }

                Console.WriteLine();

                return;
            }

            foreach (var child in node.ChildNodes)
            {
                Print(child, result + " -- " + child.Edge.ToLower() + " --> " + child.Name.ToUpper());
            }
        }

        public static void PrintLegend(string headline)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"\n{headline}");
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("Magenta color indicates the root node");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Yellow color indicates an edge");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Cyan color indicates a node");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Green color indicates a decision");
            Console.ResetColor();
        }

        public static string CalculateResult(TreeNode root, IDictionary<string, string> valuesForQuery, string result)
        {
            var valueFound = false;

            result += root.Name.ToUpper() + " -- ";

            if (root.IsLeaf)
            {
                result = root.Edge.ToLower() + " --> " + root.Name.ToUpper();
                valueFound = true;
            }
            else
            {
                foreach (var childNode in root.ChildNodes)
                {
                    foreach (var entry in valuesForQuery)
                    {
                        if (childNode.Edge.ToUpper().Equals(entry.Value.ToUpper()) && root.Name.ToUpper().Equals(entry.Key.ToUpper()))
                        {
                            valuesForQuery.Remove(entry.Key);

                            return result + CalculateResult(childNode, valuesForQuery, $"{childNode.Edge.ToLower()} --> ");
                        }
                    }
                }
            }

            // if the user entered an invalid attribute
            if (!valueFound)
            {
                result = "Attribute not found";
            }

            return result;
        }

        public static TreeNode Learn(TrainingData data, string edgeName)
        {
            var root = GetRootNode(data, edgeName);

            foreach (var item in root.NodeAttribute.DifferentAttributeNames)
            {
                // if a leaf, leaf will be added in this method
                var isLeaf = CheckIfIsLeaf(root, data, item);

                // make a recursive call as long as the node is not a leaf
                if (!isLeaf)
                {
                    var reducedTable = new TrainingData
                    {
                        Columns = data.Columns.Skip(root.TableIndex + 1).Select(x => x).ToArray(),
                        Rows = data.Rows.Where(x => x[root.TableIndex].Equals(item))
                                .Select(x => x.Skip(root.TableIndex + 1).ToArray())
                                .ToArray()
                    };

                    root.ChildNodes.Add(Learn(reducedTable, item));
                }
            }

            return root;
        }

        private static bool CheckIfIsLeaf(TreeNode root, TrainingData data, string attributeToCheck)
        {
            var isLeaf = true;
            var allEndValues = new List<string>();

            // get all leaf values for the attribute in question
            for (var i = 0; i < data.Rows.Length; i++)
            {
                if (data.Rows[i][root.TableIndex].ToString().Equals(attributeToCheck))
                {
                    allEndValues.Add(data.Rows[i][data.Columns.Length - 1].ToString());
                }
            }

            // check whether all elements of the list have the same value
            if (allEndValues.Count > 0 && allEndValues.Any(x => x != allEndValues[0]))
            {
                isLeaf = false;
            }

            // create leaf with value to display and edge to the leaf
            if (isLeaf)
            {
                root.ChildNodes.Add(new TreeNode(true, allEndValues[0], attributeToCheck));
            }

            return isLeaf;
        }

        private static TreeNode GetRootNode(TrainingData data, string edge)
        {
            var attributes = new List<NodeAttribute>();
            var highestInformationGainIndex = -1;
            var highestInformationGain = double.MinValue;

            // Get all names, amount of attributes and attributes for every column             
            for (var i = 0; i < data.Columns.Length - 1; i++)
            {
                var differentAttributenames = data.Rows.Select(x => x[i]).Distinct().ToList();
                attributes.Add(new NodeAttribute(data.Columns[i].ToString(), differentAttributenames));
            }

            // Calculate Entropy (S)
            var tableEntropy = CalculateEntropy(data);

            for (var i = 0; i < attributes.Count; i++)
            {
                attributes[i].InformationGain = GetGainForAllAttributes(data, i, tableEntropy);

                if (attributes[i].InformationGain > highestInformationGain)
                {
                    highestInformationGain = attributes[i].InformationGain;
                    highestInformationGainIndex = i;
                }
            }

            return new TreeNode(attributes[highestInformationGainIndex].Name, highestInformationGainIndex, attributes[highestInformationGainIndex], edge);
        }

        private static double GetGainForAllAttributes(TrainingData data, int colIndex, double entropyOfDataset)
        {
            var totalRows = data.Rows.Length;
            var amountForDifferentValue = GetAmountOfEdgesAndTotalPositivResults(data, colIndex);
            var stepsForCalculation = new List<double>();

            foreach (var item in amountForDifferentValue)
            {
                // helper for calculation
                var firstDivision = item[0, 1] / (double)item[0, 0];
                var secondDivision = (item[0, 0] - item[0, 1]) / (double)item[0, 0];

                // prevent dividedByZeroException
                if (firstDivision == 0 || secondDivision == 0)
                {
                    stepsForCalculation.Add(0.0);
                }
                else
                {
                    stepsForCalculation.Add(-firstDivision * Math.Log(firstDivision, 2) - secondDivision * Math.Log(secondDivision, 2));
                }
            }

            var gain = stepsForCalculation.Select((t, i) => amountForDifferentValue[i][0, 0] / (double)totalRows * t).Sum();

            gain = entropyOfDataset - gain;

            return gain;
        }

        private static double CalculateEntropy(TrainingData data)
        {
            var totalRows = data.Rows.Length;
            var amountForDifferentValue = GetAmountOfEdgesAndTotalPositivResults(data, data.Columns.Length - 1);

            var stepsForCalculation = amountForDifferentValue
                .Select(item => item[0, 0] / (double)totalRows)
                .Select(division => -division * Math.Log(division, 2))
                .ToList();

            return stepsForCalculation.Sum();
        }

        private static List<int[,]> GetAmountOfEdgesAndTotalPositivResults(TrainingData data, int indexOfColumnToCheck)
        {
            var foundValues = new List<int[,]>();
            var knownValues = data.Rows.Select(x => x[indexOfColumnToCheck]).Distinct().ToList();

            foreach (var item in knownValues)
            {
                var amount = 0;
                var positiveAmount = 0;

                for (var i = 0; i < data.Rows.Length; i++)
                {
                    if (data.Rows[i][indexOfColumnToCheck].ToString().Equals(item))
                    {
                        amount++;

                        // Counts the positive cases and adds the sum later to the array for the calculation
                        if (data.Rows[i][data.Columns.Length - 1].ToString().Equals(data.Rows[0][data.Columns.Length - 1]))
                        {
                            positiveAmount++;
                        }
                    }
                }

                int[,] array = { { amount, positiveAmount } };
                foundValues.Add(array);
            }

            return foundValues;
        }
    }
}
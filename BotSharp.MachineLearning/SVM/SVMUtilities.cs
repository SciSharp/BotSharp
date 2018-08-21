using SVM;
using SVM.BotSharp.MachineLearning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SVM.BotSharp.MachineLearningTest
{
    public static class SVMUtilities
    {
        private const double SCALE = 100;
        public const int TRAINING_SEED = 20080524;
        public const int TESTING_SEED = 20140407;

        public static Problem CreateTwoClassProblem(int count, bool isTraining = true)
        {
            Problem prob = new Problem();
            prob.Count = count;
            prob.MaxIndex = 2;
            
            Random rand = new Random(isTraining ? TRAINING_SEED : TESTING_SEED);
            // create points on either side of the vertical axis
            int positive = count / 2;
            List<double> labels = new List<double>();
            List<Node[]> data = new List<Node[]>();
            for (int i = 0; i < count; i++)
            {
                double x = rand.NextDouble() * SCALE + 10;
                double y = rand.NextDouble() * SCALE - (SCALE * .5);
                x = i < positive ? x : -x;
                data.Add(new Node[] { new Node(1, x), new Node(2, y) });
                labels.Add(i < positive ? 1 : -1);
            }
            prob.X = data.ToArray();
            prob.Y = labels.ToArray();

            return prob;
        }

        public static Problem CreateMulticlassProblem(int numberOfClasses, int count, bool isTraining = true)
        {
            if (numberOfClasses > 8)
                throw new ArgumentException("Number of classes must be < 8");

            Problem prob = new Problem();
            prob.Count = count;
            prob.MaxIndex = 3;
            
            int[] samplesPerClass = new int[numberOfClasses];
            double countPerClass = (double)count / numberOfClasses;
            double current = countPerClass;
            for (int i = 1; i < samplesPerClass.Length; i++)
            {
                samplesPerClass[i] = (int)current;
                current += countPerClass;
                samplesPerClass[i - 1] = samplesPerClass[i] - samplesPerClass[i - 1];
            }
            samplesPerClass[samplesPerClass.Length - 1] = count - samplesPerClass.Last();

            int[] xSigns = new int[8] { -1, 1, 1, -1, -1, 1, 1, -1 };
            int[] ySigns = new int[8] { 1, 1, -1, -1, 1, 1, -1, -1 };
            int[] zSigns = new int[8] { 1, 1, 1, 1, -1, -1, -1, -1 };

            Random rand = new Random(isTraining ? TRAINING_SEED : TESTING_SEED);

            List<double> labels = new List<double>();
            List<Node[]> data = new List<Node[]>();
            for (int i = 0; i < numberOfClasses; i++)
            {
                for (int j = 0; j < samplesPerClass[i]; j++)
                {
                    double x = rand.NextDouble() * SCALE + 10;
                    double y = rand.NextDouble() * SCALE + 10;
                    double z = rand.NextDouble() * SCALE + 10;
                    x *= xSigns[i];
                    y *= ySigns[i];
                    z *= zSigns[i];

                    data.Add(new Node[] { new Node(1, x), new Node(2, y), new Node(3, z) });
                    labels.Add(i);
                }
            }

            prob.X = data.ToArray();
            prob.Y = labels.ToArray();

            return prob;
        }

        public static Problem CreateRegressionProblem(int count, bool isTraining = true)
        {
            Problem prob = new Problem();
            prob.Count = count;
            prob.MaxIndex = 2;

            Random rand = new Random(isTraining ? TRAINING_SEED : TESTING_SEED);

            List<double> labels = new List<double>();
            List<Node[]> data = new List<Node[]>();
            for (int i = 0; i < count; i++)
            {
                double y = rand.NextDouble() * 10 - 5;
                double z = rand.NextDouble() * 10 - 5;
                double x = 2 * y + z;
                data.Add(new Node[] { new Node(1, y), new Node(2, z) });
                labels.Add(x);
            }
            prob.X = data.ToArray();
            prob.Y = labels.ToArray();

            return prob;
        }        
    }
}

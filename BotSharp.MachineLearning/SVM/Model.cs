/*
 * SVM.NET Library
 * Copyright (C) 2008 Matthew Johnson
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */



using System;
using System.IO;
using System.Threading;
using System.Globalization;

namespace SVM.BotSharp.MachineLearning
{
    /// <summary>
    /// Encapsulates an SVM Model.
    /// </summary>
	[Serializable]
	public class Model
	{
        internal Model()
        {
        }

        /// <summary>
        /// Parameter object.
        /// </summary>
        public Parameter Parameter{get;set;}

        /// <summary>
        /// Number of classes in the model.
        /// </summary>
        public int NumberOfClasses{get;set;}

        /// <summary>
        /// Total number of support vectors.
        /// </summary>
        public int SupportVectorCount { get; set; }

        /// <summary>
        /// The support vectors.
        /// </summary>
        public Node[][] SupportVectors{get;set;}

        /// <summary>
        /// The coefficients for the support vectors.
        /// </summary>
        public double[][] SupportVectorCoefficients{get;set;}

        /// <summary>
        /// Values in [1,...,num_training_data] to indicate SVs in the training set
        /// </summary>
        public int[] SupportVectorIndices { get; set; }

        /// <summary>
        /// Constants in decision functions
        /// </summary>
        public double[] Rho{get;set;}

        /// <summary>
        /// First pairwise probability.
        /// </summary>
        public double[] PairwiseProbabilityA{get;set;}

        /// <summary>
        /// Second pairwise probability.
        /// </summary>
        public double[] PairwiseProbabilityB{get;set;}
		
		// for classification only

        /// <summary>
        /// Class labels.
        /// </summary>
        public int[] ClassLabels{get;set;}

        /// <summary>
        /// Number of support vectors per class.
        /// </summary>
        public int[] NumberOfSVPerClass{get;set;}

        public override bool Equals(object obj)
        {
            Model test = obj as Model;
            if (test == null)
                return false;

            bool same = ClassLabels.IsEqual(test.ClassLabels);
            same = same && NumberOfClasses == test.NumberOfClasses;
            same = same && NumberOfSVPerClass.IsEqual(test.NumberOfSVPerClass);
            if(PairwiseProbabilityA != null)
                same = same && PairwiseProbabilityA.IsEqual(test.PairwiseProbabilityA);
            if(PairwiseProbabilityB != null)
                same = same && PairwiseProbabilityB.IsEqual(test.PairwiseProbabilityB);
            same = same && Parameter.Equals(test.Parameter);
            same = same && Rho.IsEqual(test.Rho);
            same = same && SupportVectorCoefficients.IsEqual(test.SupportVectorCoefficients);
            same = same && SupportVectorCount == test.SupportVectorCount;
            same = same && SupportVectors.IsEqual(test.SupportVectors);
            return same;
        }
        
        public override int GetHashCode()
        {
            return ClassLabels.ComputeHashcode() +
                NumberOfClasses.GetHashCode() +
                NumberOfSVPerClass.ComputeHashcode() +
                PairwiseProbabilityA.ComputeHashcode() +
                PairwiseProbabilityB.ComputeHashcode() +
                Parameter.GetHashCode() +
                Rho.ComputeHashcode() +
                SupportVectorCoefficients.ComputeHashcode() +
                SupportVectorCount.GetHashCode() +
                SupportVectors.ComputeHashcode();
        }

        /// <summary>
        /// Reads a Model from the provided file.
        /// </summary>
        /// <param name="filename">The name of the file containing the Model</param>
        /// <returns>the Model</returns>
        public static Model Read(string filename)
        {
            FileStream input = File.OpenRead(filename);
            try
            {
                return Read(input);
            }
            finally
            {
                input.Close();
            }
        }

        /// <summary>
        /// Reads a Model from the provided stream.
        /// </summary>
        /// <param name="stream">The stream from which to read the Model.</param>
        /// <returns>the Model</returns>
        public static Model Read(Stream stream)
        {
            TemporaryCulture.Start();

            StreamReader input = new StreamReader(stream);

            // read parameters

            Model model = new Model();
            Parameter param = new Parameter();
            model.Parameter = param;
            model.Rho = null;
            model.PairwiseProbabilityA = null;
            model.PairwiseProbabilityB = null;
            model.ClassLabels = null;
            model.NumberOfSVPerClass = null;

            bool headerFinished = false;
            while (!headerFinished)
            {
                string line = input.ReadLine();
                string cmd, arg;
                int splitIndex = line.IndexOf(' ');
                if (splitIndex >= 0)
                {
                    cmd = line.Substring(0, splitIndex);
                    arg = line.Substring(splitIndex + 1);
                }
                else
                {
                    cmd = line;
                    arg = "";
                }
                arg = arg.ToLower();

                int i,n;
                switch(cmd){
                    case "svm_type":
                        param.SvmType = (SvmType)Enum.Parse(typeof(SvmType), arg.ToUpper());
                        break;
                        
                    case "kernel_type":
                        if (arg == "polynomial")
                            arg = "poly";
                        param.KernelType = (KernelType)Enum.Parse(typeof(KernelType), arg.ToUpper());
                        break;

                    case "degree":
                        param.Degree = int.Parse(arg);
                        break;

                    case "gamma":
                        param.Gamma = double.Parse(arg);
                        break;

                    case "coef0":
                        param.Coefficient0 = double.Parse(arg);
                        break;

                    case "nr_class":
                        model.NumberOfClasses = int.Parse(arg);
                        break;

                    case "total_sv":
                        model.SupportVectorCount = int.Parse(arg);
                        break;

                    case "rho":
                        n = model.NumberOfClasses * (model.NumberOfClasses - 1) / 2;
                        model.Rho = new double[n];
                        string[] rhoParts = arg.Split();
                        for(i=0; i<n; i++)
                            model.Rho[i] = double.Parse(rhoParts[i]);
                        break;

                    case "label":
                        n = model.NumberOfClasses;
                        model.ClassLabels = new int[n];
                        string[] labelParts = arg.Split();
                        for (i = 0; i < n; i++)
                            model.ClassLabels[i] = int.Parse(labelParts[i]);
                        break;

                    case "probA":
                        n = model.NumberOfClasses * (model.NumberOfClasses - 1) / 2;
                        model.PairwiseProbabilityA = new double[n];
                            string[] probAParts = arg.Split();
                        for (i = 0; i < n; i++)
                            model.PairwiseProbabilityA[i] = double.Parse(probAParts[i]);
                        break;

                    case "probB":
                        n = model.NumberOfClasses * (model.NumberOfClasses - 1) / 2;
                        model.PairwiseProbabilityB = new double[n];
                        string[] probBParts = arg.Split();
                        for (i = 0; i < n; i++)
                            model.PairwiseProbabilityB[i] = double.Parse(probBParts[i]);
                        break;

                    case "nr_sv":
                        n = model.NumberOfClasses;
                        model.NumberOfSVPerClass = new int[n];
                        string[] nrsvParts = arg.Split();
                        for (i = 0; i < n; i++)
                            model.NumberOfSVPerClass[i] = int.Parse(nrsvParts[i]);
                        break;

                    case "SV":
                        headerFinished = true;
                        break;

                    default:
                        throw new Exception("Unknown text in model file");  
                }
            }

            // read sv_coef and SV

            int m = model.NumberOfClasses - 1;
            int l = model.SupportVectorCount;
            model.SupportVectorCoefficients = new double[m][];
            for (int i = 0; i < m; i++)
            {
                model.SupportVectorCoefficients[i] = new double[l];
            }
            model.SupportVectors = new Node[l][];

            for (int i = 0; i < l; i++)
            {
                string[] parts = input.ReadLine().Trim().Split();

                for (int k = 0; k < m; k++)
                    model.SupportVectorCoefficients[k][i] = double.Parse(parts[k]);
                int n = parts.Length-m;
                model.SupportVectors[i] = new Node[n];
                for (int j = 0; j < n; j++)
                {
                    string[] nodeParts = parts[m + j].Split(':');
                    model.SupportVectors[i][j] = new Node();
                    model.SupportVectors[i][j].Index = int.Parse(nodeParts[0]);
                    model.SupportVectors[i][j].Value = double.Parse(nodeParts[1]);
                }
            }

            TemporaryCulture.Stop();

            return model;
        }

        /// <summary>
        /// Writes a model to the provided filename.  This will overwrite any previous data in the file.
        /// </summary>
        /// <param name="filename">The desired file</param>
        /// <param name="model">The Model to write</param>
        public static void Write(string filename, Model model)
        {
            FileStream stream = File.Open(filename, FileMode.Create);
            try
            {
                Write(stream, model);
            }
            finally
            {
                stream.Close();
            }
        }

        /// <summary>
        /// Writes a model to the provided stream.
        /// </summary>
        /// <param name="stream">The output stream</param>
        /// <param name="model">The model to write</param>
        public static void Write(Stream stream, Model model)
        {
            TemporaryCulture.Start();

            StreamWriter output = new StreamWriter(stream);

            Parameter param = model.Parameter;

            output.Write("svm_type {0}\n", param.SvmType);
            output.Write("kernel_type {0}\n", param.KernelType);

            if (param.KernelType == KernelType.POLY)
                output.Write("degree {0}\n", param.Degree);

            if (param.KernelType == KernelType.POLY || param.KernelType == KernelType.RBF || param.KernelType == KernelType.SIGMOID)
                output.Write("gamma {0:0.000000}\n", param.Gamma);

            if (param.KernelType == KernelType.POLY || param.KernelType == KernelType.SIGMOID)
                output.Write("coef0 {0:0.000000}\n", param.Coefficient0);

            int nr_class = model.NumberOfClasses;
            int l = model.SupportVectorCount;
            output.Write("nr_class {0}\n", nr_class);
            output.Write("total_sv {0}\n", l);

            {
                output.Write("rho");
                for (int i = 0; i < nr_class * (nr_class - 1) / 2; i++)
                    output.Write(" {0:0.000000}", model.Rho[i]);
                output.Write("\n");
            }

            if (model.ClassLabels != null)
            {
                output.Write("label");
                for (int i = 0; i < nr_class; i++)
                    output.Write(" {0}", model.ClassLabels[i]);
                output.Write("\n");
            }

            if (model.PairwiseProbabilityA != null)
            // regression has probA only
            {
                output.Write("probA");
                for (int i = 0; i < nr_class * (nr_class - 1) / 2; i++)
                    output.Write(" {0:0.000000}", model.PairwiseProbabilityA[i]);
                output.Write("\n");
            }
            if (model.PairwiseProbabilityB != null)
            {
                output.Write("probB");
                for (int i = 0; i < nr_class * (nr_class - 1) / 2; i++)
                    output.Write(" {0:0.000000}", model.PairwiseProbabilityB[i]);
                output.Write("\n");
            }

            if (model.NumberOfSVPerClass != null)
            {
                output.Write("nr_sv");
                for (int i = 0; i < nr_class; i++)
                    output.Write(" {0}", model.NumberOfSVPerClass[i]);
                output.Write("\n");
            }

            output.Write("SV\n");
            double[][] sv_coef = model.SupportVectorCoefficients;
            Node[][] SV = model.SupportVectors;

            for (int i = 0; i < l; i++)
            {
                for (int j = 0; j < nr_class - 1; j++)
                    output.Write("{0:0.000000} ", sv_coef[j][i]);

                Node[] p = SV[i];
                if (p.Length == 0)
                {
                    output.Write("\n");
                    continue;
                }
                if (param.KernelType == KernelType.PRECOMPUTED)
                    output.Write("0:{0:0.000000}", (int)p[0].Value);
                else
                {
                    output.Write("{0}:{1:0.000000}", p[0].Index, p[0].Value);
                    for (int j = 1; j < p.Length; j++)
                        output.Write(" {0}:{1:0.000000}", p[j].Index, p[j].Value);
                }
                output.Write("\n");
            }

            output.Flush();

            TemporaryCulture.Stop();
        }
	}
}
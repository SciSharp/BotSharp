using BotSharp.Algorithm.HiddenMarkovModel.MathHelpers;
using BotSharp.Algorithm.HiddenMarkovModel.MathUtils.SpecialFunctions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.HiddenMarkovModel.MathUtils.Distribution
{
    public class StudentT : DistributionModel
    {
        protected int mDegreesOfFreedom = 1;

        public int DegreesOfFreedom
        {
            get { return mDegreesOfFreedom; }
            set { mDegreesOfFreedom = value; }
        }

        public override double Next()
        {
            throw new NotImplementedException();
        }

        public override DistributionModel Clone()
        {
            StudentT clone = new StudentT();
            clone.DegreesOfFreedom = mDegreesOfFreedom;
            return clone;
        }

        public override double GetPDF(double x)
        {
            double nu = mDegreesOfFreedom;
            double v1 = GammaFunction.GetGamma((nu + 1.0) / 2.0);
            double v2 = GammaFunction.GetGamma(nu / 2.0);
            return v1 * System.Math.Pow(1 + x * x / nu, -(nu + 1.0) / 2.0) / (System.Math.Sqrt(nu * System.Math.PI) * v2);
        }

        public static double GetPDF(double x, int df)
        {
            double nu = df;
            double v1 = GammaFunction.GetGamma((nu + 1.0) / 2.0);
            double v2 = GammaFunction.GetGamma(nu / 2.0);
            return v1 * System.Math.Pow(1 + x * x / nu, -(nu + 1.0) / 2.0) / (System.Math.Sqrt(nu * System.Math.PI) * v2);
        }

        public override double GetCDF(double x)
        {
            return GetPercentile(x, mDegreesOfFreedom);
        }

        public override double LogProbabilityFunction(double x)
        {
            throw new NotImplementedException();
        }

        public override void Process(double[] values)
        {
            throw new NotImplementedException();
        }

        public override void Process(double[] values, double[] weights)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Holds the Percentiles corresponding to the columns of the array StudentTTable.
        /// </summary>
        private static double[] Percentiles = new double[6] { 0.970, 0.975, 0.980, 0.985, 0.990, 0.995 };

        /// <summary>
        /// Holds the StudentTTable of the normal distribution corresponding to the Percentiles in Percentiles
        /// </summary>
        private static double[] NormalTable = new double[6] { 1.880794, 1.959964, 2.053749, 2.170090, 2.326348, 2.575829 };

        public static double[][] StudentTTable = new double[100][]
        {
            new double[] {10.57889, 12.7062, 15.89454, 21.20495, 31.82052, 63.65674},
            new double[] {3.896425, 4.302653, 4.848732, 5.642778, 6.964557, 9.924843},
            new double[] {2.95051, 3.182446, 3.481909, 3.896046, 4.540703, 5.840909},
            new double[] {2.600762, 2.776445, 2.998528, 3.29763, 3.746947, 4.604095},
            new double[] {2.421585, 2.570582, 2.756509, 3.002875, 3.36493, 4.032143},
            new double[] {2.313263, 2.446912, 2.612242, 2.828928, 3.142668, 3.707428},
            new double[] {2.240879, 2.364624, 2.516752, 2.714573, 2.997952, 3.499483},
            new double[] {2.189155, 2.306004, 2.448985, 2.633814, 2.896459, 3.355387},
            new double[] {2.150375, 2.262157, 2.398441, 2.573804, 2.821438, 3.249836},
            new double[] {2.120234, 2.228139, 2.359315, 2.527484, 2.763769, 3.169273},
            new double[] {2.096139, 2.200985, 2.32814, 2.490664, 2.718079, 3.105807},
            new double[] {2.076441, 2.178813, 2.302722, 2.4607, 2.680998, 3.05454},
            new double[] {2.060038, 2.160369, 2.281604, 2.435845, 2.650309, 3.012276},
            new double[] {2.046169, 2.144787, 2.263781, 2.414898, 2.624494, 2.976843},
            new double[] {2.034289, 2.13145, 2.24854, 2.397005, 2.60248, 2.946713},
            new double[] {2.024, 2.119905, 2.235358, 2.381545, 2.583487, 2.920782},
            new double[] {2.015002, 2.109816, 2.223845, 2.368055, 2.566934, 2.898231},
            new double[] {2.007067, 2.100922, 2.213703, 2.35618, 2.55238, 2.87844},
            new double[] {2.000017, 2.093024, 2.204701, 2.345648, 2.539483, 2.860935},
            new double[] {1.993713, 2.085963, 2.196658, 2.336242, 2.527977, 2.84534},
            new double[] {1.988041, 2.079614, 2.189427, 2.327792, 2.517648, 2.83136},
            new double[] {1.982911, 2.073873, 2.182893, 2.32016, 2.508325, 2.818756},
            new double[] {1.978249, 2.068658, 2.176958, 2.313231, 2.499867, 2.807336},
            new double[] {1.973994, 2.063899, 2.171545, 2.306913, 2.492159, 2.79694},
            new double[] {1.970095, 2.059539, 2.166587, 2.30113, 2.485107, 2.787436},
            new double[] {1.966509, 2.055529, 2.162029, 2.295815, 2.47863, 2.778715},
            new double[] {1.9632, 2.051831, 2.157825, 2.290914, 2.47266, 2.770683},
            new double[] {1.960136, 2.048407, 2.153935, 2.28638, 2.46714, 2.763262},
            new double[] {1.957293, 2.04523, 2.150325, 2.282175, 2.462021, 2.756386},
            new double[] {1.954645, 2.042272, 2.146966, 2.278262, 2.457262, 2.749996},
            new double[] {1.952175, 2.039513, 2.143833, 2.274614, 2.452824, 2.744042},
            new double[] {1.949865, 2.036933, 2.140904, 2.271203, 2.448678, 2.738481},
            new double[] {1.9477, 2.034515, 2.138159, 2.268008, 2.444794, 2.733277},
            new double[] {1.945666, 2.032245, 2.135581, 2.265009, 2.44115, 2.728394},
            new double[] {1.943752, 2.030108, 2.133157, 2.262188, 2.437723, 2.723806},
            new double[] {1.941948, 2.028094, 2.130871, 2.259529, 2.434494, 2.719485},
            new double[] {1.940244, 2.026192, 2.128714, 2.25702, 2.431447, 2.715409},
            new double[] {1.938633, 2.024394, 2.126674, 2.254648, 2.428568, 2.711558},
            new double[] {1.937106, 2.022691, 2.124742, 2.252401, 2.425841, 2.707913},
            new double[] {1.935659, 2.021075, 2.12291, 2.250271, 2.423257, 2.704459},
            new double[] {1.934283, 2.019541, 2.12117, 2.248249, 2.420803, 2.701181},
            new double[] {1.932975, 2.018082, 2.119515, 2.246326, 2.41847, 2.698066},
            new double[] {1.93173, 2.016692, 2.11794, 2.244495, 2.41625, 2.695102},
            new double[] {1.930542, 2.015368, 2.116438, 2.24275, 2.414134, 2.692278},
            new double[] {1.929409, 2.014103, 2.115005, 2.241085, 2.412116, 2.689585},
            new double[] {1.928326, 2.012896, 2.113636, 2.239494, 2.410188, 2.687013},
            new double[] {1.92729, 2.011741, 2.112327, 2.237974, 2.408345, 2.684556},
            new double[] {1.926298, 2.010635, 2.111073, 2.236518, 2.406581, 2.682204},
            new double[] {1.925348, 2.009575, 2.109873, 2.235124, 2.404892, 2.679952},
            new double[] {1.924437, 2.008559, 2.108721, 2.233787, 2.403272, 2.677793},
            new double[] {1.923562, 2.007584, 2.107616, 2.232503, 2.401718, 2.675722},
            new double[] {1.922722, 2.006647, 2.106555, 2.231271, 2.400225, 2.673734},
            new double[] {1.921914, 2.005746, 2.105534, 2.230086, 2.39879, 2.671823},
            new double[] {1.921136, 2.004879, 2.104552, 2.228946, 2.39741, 2.669985},
            new double[] {1.920388, 2.004045, 2.103607, 2.227849, 2.396081, 2.668216},
            new double[] {1.919666, 2.003241, 2.102696, 2.226792, 2.394801, 2.666512},
            new double[] {1.918971, 2.002465, 2.101818, 2.225772, 2.393568, 2.66487},
            new double[] {1.9183, 2.001717, 2.100971, 2.224789, 2.392377, 2.663287},
            new double[] {1.917652, 2.000995, 2.100153, 2.22384, 2.391229, 2.661759},
            new double[] {1.917026, 2.000298, 2.099363, 2.222923, 2.390119, 2.660283},
            new double[] {1.916421, 1.999624, 2.098599, 2.222038, 2.389047, 2.658857},
            new double[] {1.915836, 1.998972, 2.097861, 2.221181, 2.388011, 2.657479},
            new double[] {1.915269, 1.998341, 2.097146, 2.220352, 2.387008, 2.656145},
            new double[] {1.914721, 1.99773, 2.096455, 2.219549, 2.386037, 2.654854},
            new double[] {1.91419, 1.997138, 2.095785, 2.218772, 2.385097, 2.653604},
            new double[] {1.913676, 1.996564, 2.095135, 2.218019, 2.384186, 2.652394},
            new double[] {1.913176, 1.996008, 2.094506, 2.217289, 2.383302, 2.65122},
            new double[] {1.912692, 1.995469, 2.093895, 2.21658, 2.382446, 2.650081},
            new double[] {1.912222, 1.994945, 2.093302, 2.215893, 2.381615, 2.648977},
            new double[] {1.911766, 1.994437, 2.092727, 2.215226, 2.380807, 2.647905},
            new double[] {1.911323, 1.993943, 2.092168, 2.214577, 2.380024, 2.646863},
            new double[] {1.910892, 1.993464, 2.091625, 2.213948, 2.379262, 2.645852},
            new double[] {1.910474, 1.992997, 2.091097, 2.213335, 2.378522, 2.644869},
            new double[] {1.910066, 1.992543, 2.090584, 2.21274, 2.377802, 2.643913},
            new double[] {1.90967, 1.992102, 2.090084, 2.212161, 2.377102, 2.642983},
            new double[] {1.909285, 1.991673, 2.089598, 2.211597, 2.37642, 2.642078},
            new double[] {1.908909, 1.991254, 2.089124, 2.211048, 2.375757, 2.641198},
            new double[] {1.908544, 1.990847, 2.088663, 2.210514, 2.375111, 2.64034},
            new double[] {1.908187, 1.99045, 2.088214, 2.209993, 2.374482, 2.639505},
            new double[] {1.90784, 1.990063, 2.087777, 2.209485, 2.373868, 2.638691},
            new double[] {1.907501, 1.989686, 2.08735, 2.208991, 2.37327, 2.637897},
            new double[] {1.907171, 1.989319, 2.086934, 2.208508, 2.372687, 2.637123},
            new double[] {1.906849, 1.98896, 2.086528, 2.208038, 2.372119, 2.636369},
            new double[] {1.906535, 1.98861, 2.086131, 2.207578, 2.371564, 2.635632},
            new double[] {1.906228, 1.988268, 2.085745, 2.20713, 2.371022, 2.634914},
            new double[] {1.905928, 1.987934, 2.085367, 2.206692, 2.370493, 2.634212},
            new double[] {1.905636, 1.987608, 2.084998, 2.206265, 2.369977, 2.633527},
            new double[] {1.90535, 1.98729, 2.084638, 2.205847, 2.369472, 2.632858},
            new double[] {1.90507, 1.986979, 2.084286, 2.205439, 2.368979, 2.632204},
            new double[] {1.904797, 1.986675, 2.083942, 2.205041, 2.368497, 2.631565},
            new double[] {1.90453, 1.986377, 2.083605, 2.204651, 2.368026, 2.63094},
            new double[] {1.904269, 1.986086, 2.083276, 2.204269, 2.367566, 2.63033},
            new double[] {1.904013, 1.985802, 2.082954, 2.203896, 2.367115, 2.629732},
            new double[] {1.903763, 1.985523, 2.08264, 2.203531, 2.366674, 2.629148},
            new double[] {1.903519, 1.985251, 2.082331, 2.203174, 2.366243, 2.628576},
            new double[] {1.903279, 1.984984, 2.08203, 2.202824, 2.365821, 2.628016},
            new double[] {1.903045, 1.984723, 2.081734, 2.202482, 2.365407, 2.627468},
            new double[] {1.902815, 1.984467, 2.081445, 2.202147, 2.365002, 2.626931},
            new double[] {1.90259, 1.984217, 2.081162, 2.201819, 2.364606, 2.626405},
            new double[] {1.90237, 1.983972, 2.080884, 2.201497, 2.364217, 2.625891}
        };

        /// <summary>
        /// Holds the difference between the Percentiles contained in Percentiles. Currently = 0.005
        /// </summary>
        private const double delta = 0.005; //(Percentiles[Percentiles.Length - 1] - Percentiles[0]) / (Percentiles.Length - 1);

        /// <summary>
        /// Gets the quantile for the given degrees of freedom and percentile. 
        /// p is rounded off to the nearest percentile in the internal table.
        /// The Percentiles in the table are: 0.970, 0.975, 0.980, 0.985, 0.990, 0.995.
        /// If df is greater than 100, then uses the normal approximation.
        /// </summary>
        /// <param name="p">percentile value</param>
        /// <param name="df">degrees of freedom</param>
        /// <returns></returns>
        public static double GetQuantile(double p, int df)
        {
            if (df <= 0) { return double.NaN; }
            int row = df - 1;
            int column = (int)System.Math.Round((p - Percentiles[0]) / delta);
            column = (column >= 0) ? column : 0;
            column = (column < Percentiles.Length) ? column : Percentiles.Length - 1;
            return (row < StudentTTable.Length) ? StudentTTable[row][column] : NormalTable[column];
        }

        /// <summary>
        /// Implementation adopted from the javascript code at http://www.math.ucla.edu/~tom/distributions/tDist.html
        /// </summary>
        /// <param name="X"></param>
        /// <param name="df"></param>
        /// <returns></returns>
        public static double GetPercentile(double X, int df)
        {
            double tcdf = -1;
            if (df <= 0)
            {
                throw new Exception("Degrees of freedom must be positive");
            }
            else
            {
                double A = df / 2;
                double S = A + .5;
                double Z = df / (df + X * X);
                double BT = System.Math.Exp(Gamma.Log(S) - Gamma.Log(.5) - Gamma.Log(A) + A * System.Math.Log(Z) + .5 * System.Math.Log(1 - Z));

                double betacdf = 0;
                if (Z < (A + 1) / (S + 2))
                {
                    betacdf = BT * Betinc(Z, A, .5);
                }
                else
                {
                    betacdf = 1 - BT * Betinc(1 - Z, .5, A);
                }
                if (X < 0)
                {
                    tcdf = betacdf / 2;
                }
                else
                {
                    tcdf = 1 - betacdf / 2;
                }
            }
            tcdf = System.Math.Round(tcdf * 100000) / 100000;

            return tcdf;
        }



        private static double Betinc(double X, double A, double B)
        {
            double A0 = 0;
            double B0 = 1;
            double A1 = 1;
            double B1 = 1;
            double M9 = 0;
            double A2 = 0;
            double C9;
            while (System.Math.Abs((A1 - A2) / A1) > .00001)
            {
                A2 = A1;
                C9 = -(A + M9) * (A + B + M9) * X / (A + 2 * M9) / (A + 2 * M9 + 1);
                A0 = A1 + C9 * A0;
                B0 = B1 + C9 * B0;
                M9 = M9 + 1;
                C9 = M9 * (B - M9) * X / (A + 2 * M9 - 1) / (A + 2 * M9);
                A1 = A0 + C9 * A1;
                B1 = B0 + C9 * B1;
                A0 = A0 / B1;
                B0 = B0 / B1;
                A1 = A1 / B1;
                B1 = 1;
            }
            return A1 / A;
        }

        /// <summary>
        /// Returns the percentile for the given degrees of freedom and quantile value
        /// q is rounded off to the nearest quantile in the student's t table.
        /// If df is greater than 100, then uses the normal approximation.
        /// </summary>
        /// <param name="q"></param>
        /// <param name="df"></param>
        /// <returns></returns>
        public static double GetPercentile2(double q, int df)
        {
            int row = df - 1;
            double[] tRow = null;
            if (row < StudentTTable.Length)
            {
                tRow = StudentTTable[row];
            }
            else
            {
                tRow = NormalTable;
            }

            for (int j = 0; j < tRow.Length - 1; ++j)
            {
                if (q > tRow[j] && q <= tRow[j + 1])
                {
                    return (Percentiles[j + 1] - Percentiles[j]) * (q - tRow[j]) / delta + Percentiles[j];
                }
            }
            return double.NaN;
        }
    }

}

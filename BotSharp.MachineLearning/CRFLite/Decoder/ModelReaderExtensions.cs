using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BotSharp.MachineLearning.CRFLite.Decoder
{
    internal static class ModelReaderExtensions
    {
        private static readonly string featureFileNameExtension = ".feature";
        private static readonly string weightFileNameExtension = ".alpha";

        internal static string ToMetadataModelName(this string modelName)
        {
            return modelName;
        }

        internal static string ToFeatureSetFileName(this string modelName)
        {
            return String.Concat(modelName, featureFileNameExtension);
        }

        internal static string ToFeatureWeightFileName(this string modelName)
        {
            return String.Concat(modelName, weightFileNameExtension);
        }

        internal static void ThrowIfNotExists(this string fileName)
        {
            if (String.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException("fileName",
                    "Please specify a valid model path");

            if (!File.Exists(fileName))
                throw new FileNotFoundException("fileName",
                    "Please specify a valid model path");
        }
    }
}

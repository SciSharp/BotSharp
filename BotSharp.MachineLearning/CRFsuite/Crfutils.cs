using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BotSharp.MachineLearning.CRFsuite
{   
    public class Crfutils
    {
        /// <summary>
        /// Generate features for an item sequence by applying feature templates.
        /// A feature template consists of a tuple of (name, offset) pairs,
        /// where name and offset specify a field name and offset from which
        /// the template extracts a feature valreaditerue. Generated features are stored
        /// in the 'F' field of each item in the sequence.
        /// </summary>
        /// <param name="X">Token features for a sentence</param>
        /// <param name="template">the template which contains what feature to extract</param>

        public void ApplyTemplates (List<Dictionary<string, Object>> X, Template templates) 
        {
            foreach (List<CRFFeature> template in templates.Features) 
            {
                List<string> list = new List<string>();
                template.ForEach(t => list.Add($"{t.Field}[{t.Offset}]"));
                string name = string.Join("|", list);

                for (int t = 0 ; t < X.Count() ; t++) {
                    List<string> values = new List<string>();
                    foreach (CRFFeature crffeature in template) 
                    {
                        string field = crffeature.Field;
                        int offset = crffeature.Offset;
                        int p = t + offset;
                        if (p < 0 || p >= X.Count) 
                        {
                            values.Clear();
                            break;
                        }
                        values.Add(X[p][field].ToString());
                    }
                    if (values != null && values.Count > 0) 
                    {
                        string value = string.Join("|", values);
                        ((List<string>)X[t]["F"]).Add($"{name}={value}");
                    }
                }
            }
        }
        /// <summary>
        /// Return an iterator for item sequences read from a file object.
        /// This function reads a sequence from a file object L{fi}, and
        /// yields the sequence as a list of mapping objects. Each line
        /// (item) from the file object is split by the separator character
        /// L{sep}. Separated values of the item are named by L{names},
        /// and stored in a mapping object. Every item has a field 'F' that
        /// is reserved for storing features.
        /// </summary>
        /// <param name="fiPath">source file which contains crf style training data</param>
        /// <param name="names">each attribute name in fields</param>
        /// <param name="sep">seperate by</param>
        
        public List<List<Dictionary<string, Object>>> Readiter (string fiPath, List<string> names, string sep = " ") 
        {
            List<List<Dictionary<string, Object>>> Xs = new List<List<Dictionary<string, Object>>>();
            List<Dictionary<string, Object>> X = new List<Dictionary<string, Object>>();
            StreamReader sr = new StreamReader(fiPath, Encoding.Default);
            string line;
            while ((line = sr.ReadLine()) != null) 
            {
                line = line.Replace("\n","");
                if (line == null || line.Length == 0) 
                {
                    Xs.Add(new List<Dictionary<string, Object>>(X));
                    X.Clear();
                }
                else
                {
                    String[] fields = line.Split(sep);
                    if (fields.Count() < names.Count)
                    {
                        // Error Exception
                    }
                    Dictionary<string, Object> item = new Dictionary<string, Object>();
                    item.Add("F", new List<string>());
                    for (int i = 0 ; i < names.Count ; i++)
                    {
                        item.Add(names[i], fields[i]);
                    }
                    X.Add(item);
                }
            }
            return Xs;
        }
        /// <summary>
        /// Escape colon characters from feature names.
        /// </summary>
        /// <param name="src">a feature name</param>

        public string Escape(string src)
        {
            return src.Replace(":", "__COLON__");
        }
        /// <summary>
        /// Output features (and reference labels) of a sequence in CRFSuite
        /// format. For each item in the sequence, this function writes a
        /// reference label (if L{field} is a non-empty string) and features.
        /// </summary>
        /// <param name="sw">destination file stream writer</param>
        /// <param name="X">Token features for a sentence</param>
        /// <param name="field">one attribute name in fields</param>
        
        public void OutputFeatures (StreamWriter sw, List<Dictionary<string, Object>> X, string field = "")
        {
            for (int t = 0; t < X.Count; t++) 
            {
                if (field.Length != 0)
                {
                    sw.Write(X[t][field]);
                }
                foreach (string a in (List<string>)X[t]["F"])
                {
                    sw.Write($"\t{Escape(a)}");
                }
                sw.Write("\n");
            }
            sw.Write("\n");
        }
        /// <summary>
        /// CRFFileGenerator
        /// </summary>
        /// <param name="FeatureExtractor">an extractor which to do the feature extracting work</param>
        /// <param name="fields">attributes name seperated by space</param>
        /// <param name="sep">string whihch seperated by</param>
        public void CRFFileGenerator (System.Action<List<Dictionary<string, Object>>> FeatureExtractor, string fields, string sep= " ")
        {
            String fiPath = "/home/bolo/Desktop/BotSharp/TrainingFiles/rawTrain.txt";
            FileStream fs = new FileStream("/home/bolo/Desktop/BotSharp/TrainingFiles/1.txt", FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);
            List<string> F = fields.Split(" ").ToList();
            List<List<Dictionary<string, Object>>> Xs = Readiter(fiPath, F, " ");

            foreach (List<Dictionary<string, Object>> X in Xs)
            {
                FeatureExtractor(X);
                OutputFeatures(sw, X, "y");
            }
            sw.Flush();
            sw.Close();
            fs.Close();
        }
    }
}
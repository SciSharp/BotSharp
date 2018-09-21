using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace JiebaNet.Segmenter.Common
{
    public static class FileExtension
    {
        public static string ReadEmbeddedAllLine(string path)
        {
            return ReadEmbeddedAllLine(path, Encoding.UTF8);
        }

        public static string ReadEmbeddedAllLine(string path,Encoding encoding)
        {
            using (var sr = new StreamReader(path))
            {
                return sr.ReadToEnd();
            }
        }

        public static List<string> ReadEmbeddedAllLines(string path, Encoding encoding)
        {
            List<string> list = new List<string>();
            using (var sr = new StreamReader(path))
            {
                string item;
                while ((item = sr.ReadLine()) != null)
                {
                    list.Add(item);
                }
            }
            return list;
        }

        public static List<string> ReadEmbeddedAllLines(string path)
        {
            return ReadEmbeddedAllLines(path, Encoding.UTF8);
        }
    }
}

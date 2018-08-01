using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace BotSharp.MachineLearning.CRFsuite
{
    public class Ner
    {
        // Separator of field values.
        string separator = " ";
        // Field names of the input data.
        string fields = "y w pos chk";
        Template templates = new Template();
        
        public string GetShape (string token) 
        {
            string r = "";
            foreach (char c in token.ToCharArray())
            {
                if (IsSupperChar(c))
                {
                    r += "U";
                }
                else if (IsLowerChar(c))
                {
                    r += "L";
                }
                else if (IsDigitChar(c))
                {               
                    r += "D";
                }
                else if (((IList)new char[]{'.', ','}).Contains(c))
                {
                    r += ".";
                }
                else if (((IList)new char[]{';', ':', '?', '!'}).Contains(c))
                {
                    r += ";";
                }
                else if (((IList)new char[]{'+', '-', '*', '/', '=', '|', '_'}).Contains(c))
                {
                    r += "-";
                }
                else if (((IList)new char[]{'(', '{', '[', '<'}).Contains(c))
                {
                    r += "(";
                }
                else if (((IList)new char[]{')', '}', ']', '>'}).Contains(c))
                {
                    r += ")";
                }
                else
                {
                    r += c;
                }
            }
            return r;
        }

        public string Degenerate (string src)
        {
            string dst = "";
            foreach (char c in src)
            {
                if (dst.Trim() == "" || char.Parse(dst.Substring(dst.Length - 1, 1)) != c) 
                {
                    dst += c;
                }
            }
            return dst;
        }

        public string GetType (string token) 
        {
            List<string> T =  new List<String>{"AllUpper", "AllDigit", "AllSymbol","AllUpperDigit", "AllUpperSymbol", "AllDigitSymbol",
            "AllUpperDigitSymbol","InitUpper","AllLetter","AllAlnum"};
            HashSet<string> R = new HashSet<string>(T);
            if (token == null || token.Trim() == "") 
            {
                return "EMPTY";
            }
            for (int i = 0 ; i < token.Length ; i++)
            {
                char c = token[i];
                if (IsSupperChar(c))
                {
                    if (R.Contains("AllDigit")) 
                    {
                        R.Remove("AllDigit");
                    }
                    if (R.Contains("AllSymbol")) 
                    {
                        R.Remove("AllSymbol");
                    }
                    if (R.Contains("AllDigitSymbol")) 
                    {
                        R.Remove("AllDigitSymbol");
                    }
                }
                else if (IsDigitChar(c) || ((IList)new char[]{'.', ','}).Contains(c)) 
                {
                    if (R.Contains("AllUpper")) 
                    {
                        R.Remove("AllUpper");
                    }
                    if (R.Contains("AllSymbol")) 
                    {
                        R.Remove("AllSymbol");
                    }
                    if (R.Contains("AllUpperSymbol")) 
                    {
                        R.Remove("AllUpperSymbol");
                    }
                    if (R.Contains("AllLetter")) 
                    {
                        R.Remove("AllLetter");
                    }
                }
                else if (IsLowerChar(c)) 
                {
                    if (R.Contains("AllUpper")) 
                    {
                        R.Remove("AllUpper");
                    }
                    if (R.Contains("AllDigit")) 
                    {
                        R.Remove("AllDigit");
                    }
                    if (R.Contains("AllSymbol")) 
                    {
                        R.Remove("AllSymbol");
                    }
                    if (R.Contains("AllUpperDigit")) 
                    {
                        R.Remove("AllUpperDigit");
                    }
                    if (R.Contains("AllUpperSymbol")) 
                    {
                        R.Remove("AllUpperSymbol");
                    }
                    if (R.Contains("AllDigitSymbol")) 
                    {
                        R.Remove("AllDigitSymbol");
                    }
                    if (R.Contains("AllUpperDigitSymbol")) 
                    {
                        R.Remove("AllUpperDigitSymbol");
                    }
                }
                else 
                {
                    if (R.Contains("AllUpper")) 
                    {
                        R.Remove("AllUpper");
                    }
                    if (R.Contains("AllDigit")) 
                    {
                        R.Remove("AllDigit");
                    }
                    if (R.Contains("AllUpperDigit")) 
                    {
                        R.Remove("AllUpperDigit");
                    }
                    if (R.Contains("AllLetter")) 
                    {
                        R.Remove("AllLetter");
                    }
                    if (R.Contains("AllAlnum")) 
                    {
                        R.Remove("AllAlnum");
                    }
                }

                if (i == 0 && !IsSupperChar(c)) 
                {
                    if (R.Contains("InitUpper")) 
                    {
                        R.Remove("InitUpper");
                    }
                }
            }
            foreach (string tag in T)
            {
                if (R.Contains(tag)) 
                {
                    return tag;
                }
            }
            return "NO";
        }

        public Boolean Get2d (string token) 
        {
            return token.Length == 2 && IsDigit(token);
        }

        public Boolean Get4d (string token) 
        {
            return token.Length == 4 && IsDigit(token);
        }

        // is token digit, alpha or not
        public Boolean GetDa (string token) 
        {
            Boolean bd = false;
            Boolean ba = false;
            foreach (char c in token) 
            {
                if (IsDigitChar(c)) 
                {
                    bd = true;
                }
                else if (IsAlphaChar(c)) 
                {
                    ba = true;
                }
                else
                {
                    return false;
                }
            }

            return bd && ba;
        }

        public Boolean GetDand (string token, char p)
        {
            Boolean bd = false;
            Boolean bdd = false;
            foreach (char c in token) 
            {
                if (IsDigitChar(c))
                {
                    bd = true;
                }
                else if (c == p) 
                {
                    bdd = true;
                }
                else
                {
                    return false;
                }
            }   
            return bd && bdd;         
        } 

        public Boolean GetAllOther (string token) 
        {
            foreach (char c in token) 
            {
                if (IsDigitChar(c) || IsAlphaChar(c))
                {
                    return false;
                }
            }
            return true;
        }

        public Boolean GetCapPeriod (string token)
        {
            return token.Length == 2 && IsSupperChar(token[0]) && token[1] == '.';
        }

        public Boolean ContainsUpper (string token) 
        {
            foreach (char c in token) 
            {
                if (IsSupperChar(c)) 
                {
                    return true;
                }
            }
            return false;
        }

        public Boolean ContainsLower (string token) 
        {
            foreach (char c in token) 
            {
                if (IsLowerChar(c)) 
                {
                    return true;
                }
            }
            return false;
        }

        public Boolean ContainsAlpha (string token) 
        {
            foreach (char c in token) 
            {
                if (IsAlphaChar(c)) 
                {
                    return true;
                }
            }
            return false;
        }

        public Boolean ContainsDigit (string token) 
        {
            foreach (char c in token) 
            {
                if (IsDigitChar(c)) 
                {
                    return true;
                }
            }
            return false;
        }

        public Boolean ContainsSymbol (string token) 
        {
            foreach (char c in token) 
            {
                if (!IsAlnumChar(c)) 
                {
                    return true;
                }
            }
            return false;
        }

        public string B (Boolean v)
        {
            return v ? "yes" : "no";
        }

        public void Observation (Dictionary<string, Object> v, string defval = "")
        {
            // Lowercased token.
            v.Add("wl", v["w"].ToString().ToLower());
            // Token shape.
            v.Add("shape", GetShape(v["w"].ToString()));
            // Token shape degenerated.
            v.Add("shaped", Degenerate(v["shape"].ToString()));
            // Token type.
            v.Add("type", GetType(v["w"].ToString()));
            // Prefixes (length between one to four).
            if (v["w"].ToString().Length >= 1)
            {
                v.Add("p1", v["w"].ToString().Substring(0, 1));
            }
            else
            {
                v.Add("p1", defval);
            }

            if (v["w"].ToString().Length >= 2)
            {
                v.Add("p2", v["w"].ToString().Substring(0, 2));
            }
            else
            {
                v.Add("p2", defval);
            }

            if (v["w"].ToString().Length >= 3)
            {
                v.Add("p3", v["w"].ToString().Substring(0, 3));
            }
            else
            {
                v.Add("p3", defval);
            }

            if (v["w"].ToString().Length >= 4)
            {
                v.Add("p4", v["w"].ToString().Substring(0, 4));
            }
            else
            {
                v.Add("p4", defval);
            }

            // Suffixes (length between one to four).
            string word = v["w"].ToString();
            if (v["w"].ToString().Length >= 1) 
            {
                v.Add("s1", word.Substring(word.Length - 1, 1));
            }
            else
            {
                v.Add("s1", defval);
            }
            
            if (v["w"].ToString().Length >= 2) 
            {
                v.Add("s2", word.Substring(word.Length - 2, 2));
            }
            else
            {
                v.Add("s2", defval);
            }
            
            if (v["w"].ToString().Length >= 3) 
            {
                v.Add("s3", word.Substring(word.Length - 3, 3));
            }
            else
            {
                v.Add("s3", defval);
            }
            
            if (v["w"].ToString().Length >= 4) 
            {
                v.Add("s4", word.Substring(word.Length - 4, 4));
            }
            else
            {
                v.Add("s4", defval);
            }
            
            //  Two digits
            v.Add("2d", B(Get2d(v["w"].ToString())));
            //  Four digits
            v.Add("4d", B(Get4d(v["w"].ToString())));
            // Alphanumeric token.
            v.Add("d&a", B(GetDa(v["w"].ToString())));
            // Digits and '-'.
            v.Add("d&-", B(GetDand(v["w"].ToString(), '-')));
            // Digits and '/'.
            v.Add("d&/", B(GetDand(v["w"].ToString(), '/')));
            // Digits and ','.
            v.Add("d&,", B(GetDand(v["w"].ToString(), ',')));
            // Digits and '.'.
            v.Add("d&.", B(GetDand(v["w"].ToString(), '.')));
            // A uppercase letter followed by '.'
            v.Add("up", B(GetCapPeriod(v["w"].ToString())));
            // An initial uppercase letter.
            v.Add("iu", B(IsSupperChar(v["w"].ToString()[0])));
            //  All uppercase letters.
            v.Add("au", B(IsSupper(v["w"].ToString())));
            //  All lowercase letters.
            v.Add("al", B(IsLower(v["w"].ToString())));
            // All digit letters.
            v.Add("ad", B(IsDigit(v["w"].ToString())));
            // All other (non-alphanumeric) letters.
            v.Add("ao",B(GetAllOther(v["w"].ToString())));

            // Contains a uppercase letter.
            v.Add("cu", B(ContainsUpper(v["w"].ToString())));
            // Contains a lowercase letter.
            v.Add("cl", B(ContainsLower(v["w"].ToString())));
            //  Contains a alphabet letter.
            v.Add("ca", B(ContainsAlpha(v["w"].ToString())));
            // Contains a digit.
            v.Add("cd", B(ContainsUpper(v["w"].ToString())));
            // Contains a symbol.
            v.Add("cs", B(ContainsSymbol(v["w"].ToString())));
        }

        public void DisJunctive(List<Dictionary<string, Object>> X, int t, string field, int begin, int end)
        {
            string name = $"{field}[{begin}..{end}]";
            for (int offset = begin; offset < end + 1; offset++)
            {
                int p = t + offset;
                if (p < 0 || p >= X.Count) 
                {
                    continue;
                }
                List<string> F = (List<string>)X[t]["F"];
                F.Add($"{name}={X[p][field]}");
            }
        }

        string[] Uique = new string[]{"w", "wl", "pos", "chk", "shape", "shaped", "type",
                                  "p1", "p2", "p3", "p4","s1", "s2", "s3", "s4",
                                  "2d", "4d", "d&a", "d&-", "d&/", "d&,", "d&.", "up",
                                  "iu", "au", "al", "ad", "ao", "cu", "cl", "ca", "cd", "cs"};
        string[] Bi = new string[]{"w", "pos", "chk", "shaped", "type"};

        public void InitialTemplate () 
        {
            foreach (string name in Uique)
            {
                for (int i = -2 ; i < 3; i++)
                {
                    List<CRFFeature> templateRowFeature = new List<CRFFeature>();
                    templateRowFeature.Add(new CRFFeature(name, i));
                    templates.Features.Add(templateRowFeature);
                }
            }

            foreach (string name in Bi)
            {
                for (int i = -2 ; i < 2; i++)
                {
                    List<CRFFeature> templateRowFeature = new List<CRFFeature>();
                    templateRowFeature.Add(new CRFFeature(name, i));
                    templateRowFeature.Add(new CRFFeature(name, i + 1));
                    templates.Features.Add(templateRowFeature);
                }
            }
        }

        public void FeatureExtractor (List<Dictionary<string, Object>> X) 
        {
            // Append observations.
            foreach (Dictionary<string, Object> d in X) 
            {
                Observation(d);
            }
            // Apply the feature templates.
            new Crfutils().ApplyTemplates(X, templates);
            
            // Append disjunctive features.
            for (int t = 0; t < X.Count ; t++) 
            {
                DisJunctive(X, t, "w", -4, -1);
                DisJunctive(X, t, "w", 1, 4);
            }

            if (X != null && X.Count > 0) {
                ((List<string>)X[0]["F"]).Add("__BOS__");
                ((List<string>)X[X.Count - 1]["F"]).Add("__EOS__");
            }
        }

        public void NerStart () 
        {
            InitialTemplate();
            new Crfutils().CRFFileGenerator(FeatureExtractor, fields, separator);
        }


        private Boolean IsSupperChar(char c)
        {
            return ((int) c - 'A' >= 0) && ('Z' - (int) c >= 0) ;
        }
        private Boolean IsLowerChar(char c)
        {
            return ((int) c - 'a' >= 0)&&('z' - (int) c >= 0) ;
        }
        private Boolean IsDigitChar(char c)
        {
            return ((int) c - '0' >= 0)&&('9' - (int) c >= 0) ;
        }
        private Boolean IsAlphaChar (char c) 
        {
            return IsLowerChar(c) || IsSupperChar(c);
        }
        private Boolean IsAlnumChar (char c)
        {
            return IsAlphaChar(c) || IsDigitChar(c);
        }
        private Boolean IsDigit (String s) 
        {
            string patternAllDigit = @"^[0-9]+$";
            return new Regex(patternAllDigit).IsMatch(s);
        }
        private Boolean IsSupper (string s) 
        {
            string patternAllCaptain = @"^[A-Z]+$";
            return new Regex(patternAllCaptain).IsMatch(s);
        }
        private Boolean IsLower (string s) 
        {
            string patternAllCaptain = @"^[a-z]+$";
            return new Regex(patternAllCaptain).IsMatch(s);
        }
    }

    public class Template 
    {
        public List<List<CRFFeature>> Features{ get; set; }
        public Template () 
        {
            this.Features = new List<List<CRFFeature>>();
        }

    }

    public class CRFFeature
    {
        public string Field { get; set; }
        public int Offset { get; set; }
        public CRFFeature (string field, int offset) 
        {
            this.Field = field;
            this.Offset = offset;
        }
    }
}
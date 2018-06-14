using BotSharp.Core.Intents;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace BotSharp.Core.Models
{
    public class AIResponseResult
    {
        String action;

        public Boolean ActionIncomplete { get; set; }

        public String Action
        {
            get
            {
                if (string.IsNullOrEmpty(action))
                {
                    return string.Empty;
                }
                return action;
            }
            set
            {
                action = value;
            }
        }

        public Dictionary<string, string> Parameters { get; set; }

        public AIContext[] Contexts { get; set; }

        public AIResponseMetadata Metadata { get; set; }

        public String ResolvedQuery { get; set; }

        public AIResponseFulfillment Fulfillment { get; set; }

        public string Source { get; set; }

        public decimal Score { get; set; }

        [JsonIgnore]
        public bool HasParameters
        {
            get
            {
                return Parameters != null && Parameters.Count > 0;
            }
        }

        public string GetStringParameter(string name, string defaultValue = "")
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            if (Parameters.ContainsKey(name))
            {
                return Parameters[name].ToString();
            }

            return defaultValue;
        }

        public int GetIntParameter(string name, int defaultValue = 0)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            if (Parameters.ContainsKey(name))
            {
                var parameterValue = Parameters[name].ToString();
                int result;
                if (int.TryParse(parameterValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
                {
                    return result;
                }

                float floatResult;
                if (float.TryParse(parameterValue, NumberStyles.Float, CultureInfo.InvariantCulture, out floatResult))
                {
                    result = Convert.ToInt32(floatResult);
                    return result;
                }
            }

            return defaultValue;
        }

        public float GetFloatParameter(string name, float defaultValue = 0)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            if (Parameters.ContainsKey(name))
            {
                var parameterValue = Parameters[name].ToString();
                float result;
                if (float.TryParse(parameterValue, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
                {
                    return result;
                }
            }

            return defaultValue;
        }

        public JObject GetJsonParameter(string name, JObject defaultValue = null)
        {
            if (string.IsNullOrEmpty("name"))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (Parameters.ContainsKey(name))
            {
                var parameter = Parameters[name].ToString();
                if (parameter != null)
                {
                    return JObject.FromObject(parameter);
                }
            }

            return defaultValue;
        }

        public AIContext GetContext(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Name must be not empty", nameof(name));
            }

            return Contexts?.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.CurrentCultureIgnoreCase));
        }
    }
}

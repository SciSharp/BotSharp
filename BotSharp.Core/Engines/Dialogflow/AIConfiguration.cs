using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Models
{
    public class AIConfiguration
    {
        private const string SERVICE_PROD_URL = "https://api.api.ai/v1/";
        private const string SERVICE_DEV_URL = "https://dev.api.ai/api/";

        private const string CURRENT_PROTOCOL_VERSION = "20150910";

        public string ClientAccessToken { get; private set; }

        public SupportedLanguage Language { get; set; }

        public bool VoiceActivityDetectionEnabled { get; set; }

        public string SessionId { get; set; }

        /// <summary>
        /// If true, will be used Testing API.AI server instead of Production server. This option for TESTING PURPOSES ONLY.
        /// </summary>
        public bool DevMode { get; set; }

        /// <summary>
        /// If true, all request and response content will be printed to the console. Use this option only FOR DEVELOPMENT.
        /// </summary>
        public bool DebugLog { get; set; }

        string protocolVersion;
        public string ProtocolVersion
        {
            get
            {
                return protocolVersion;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }
                protocolVersion = value;
            }
        }

        public AIConfiguration(string clientAccessToken, SupportedLanguage language)
        {
            this.ClientAccessToken = clientAccessToken;
            this.Language = language;

            DevMode = false;
            DebugLog = false;
            VoiceActivityDetectionEnabled = true;

            ProtocolVersion = CURRENT_PROTOCOL_VERSION;
        }

        public string RequestUrl
        {
            get
            {
                var baseUrl = DevMode ? SERVICE_DEV_URL : SERVICE_PROD_URL;
                return string.Format("{0}{1}?v={2}", baseUrl, "query", ProtocolVersion);
            }
        }
    }
}

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Engines.Articulate
{
    public class SettingsModel
    {
        public string DucklingURL { get; set; }

        public string UiLanguage { get; set; }

        public string DefaultAgentLanguage { get; set; }

        public string DefaultTimezone { get; set; }

        public List<String> Timezones { get; set; }

        public List<PipelineModel> DomainClassifierPipeline { get; set; }

        public List<PipelineModel> IntentClassifierPipeline { get; set; }

        public List<String> DucklingDimension { get; set; }

        public List<PipelineModel> EntityClassifierPipeline { get; set; }

        public List<String> DefaultAgentFallbackResponses { get; set; }

        public string RasaURL { get; set; }

        public List<String> SpacyPretrainedEntities { get; set; }

        public List<LanguageModel> AgentLanguages { get; set; }

        public List<LanguageModel> UiLanguages { get; set; }
    }
}

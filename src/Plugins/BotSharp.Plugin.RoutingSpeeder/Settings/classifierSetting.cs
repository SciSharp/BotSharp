using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Plugin.RoutingSpeeder.Settings;

public class classifierSetting
{
    
    public Dictionary<string, float> labelMappingDict { get; set; } = new Dictionary<string, float>()
	{
		{"goodbye", 0f},
		{"greeting", 1f},
		{"other", 2f},
		{"wo-followup", 3f},
		{"wo-identifer", 4f},
		{"wo-scheduler", 5}
	};
    public string RAW_DATA_DIR { get; set; } = "C:\\new_wenbocao\\one_brain\\WebStarter\\data\\raw_data";
    public string MODEL_DIR { get; set; } = "C:\\new_wenbocao\\one_brain\\WebStarter\\data\\models";
}

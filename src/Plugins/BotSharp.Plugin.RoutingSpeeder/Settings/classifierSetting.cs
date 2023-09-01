using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Plugin.RoutingSpeeder.Settings;

public class ClassifierSetting
{
    public Dictionary<string, float> LabelMappingDict { get; set; } = new Dictionary<string, float>()
	{
		{"goodbye", 0f},
		{"greeting", 1f},
		{"other", 2f}
	};

    public string RAW_DATA_DIR { get; set; } = "raw_data";
    public string MODEL_DIR { get; set; } = "models";
    public string LABEL_FILE_NAME { get; set; } = "label.txt";
}

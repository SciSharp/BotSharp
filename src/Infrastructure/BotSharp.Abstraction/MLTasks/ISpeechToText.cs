using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Abstraction.MLTasks;

public interface ISpeechToText
{
    Task<string> AudioToTextTranscript(string filePath);
    // Task<string> AudioToTextTranscript(Stream stream);
    void SetModelType(string modelType);
}

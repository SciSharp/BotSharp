namespace BotSharp.Abstraction.Interpreters.Settings;

public class InterpreterSettings
{
    public PythonInterpreterSetting Python { get; set; }
}

public class PythonInterpreterSetting
{
    public string PythonDLL { get; set; }
}

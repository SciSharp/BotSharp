using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using BotSharp.Plugin.PythonInterpreter.Interfaces;
using BotSharp.Plugin.PythonInterpreter.Settings;
using Microsoft.Extensions.Logging;

namespace BotSharp.Plugin.PythonInterpreter.Services;

public class PyScriptRunner : IPyScriptRunner
{
    private readonly PythonInterpreterSettings _settings;
    private readonly ILogger<PyScriptRunner> _logger;

    public PyScriptRunner(PythonInterpreterSettings settings, ILogger<PyScriptRunner> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async Task<string> RunScript(string scriptPath, string args)
    {
        if (!File.Exists(scriptPath))
        {
            throw new FileNotFoundException($"Python script not found: {scriptPath}");
        }

        var cmd = _settings.PythonVersion == "python3" ? "python3" : "python";
        // 允许配置绝对路径
        if (!string.IsNullOrEmpty(_settings.InstallLocation))
        {
             cmd = Path.Combine(_settings.InstallLocation, cmd);
        }

        var processStartInfo = new ProcessStartInfo
        {
            FileName = cmd,
            Arguments = $"\"{scriptPath}\" {args}", // 注意参数转义，特别是包含空格的路径
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try 
        {
            using var process = Process.Start(processStartInfo);
            if (process == null) throw new Exception("Failed to start python process.");

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();
            
            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0)
            {
                _logger.LogError($"Python script execution failed. Exit code: {process.ExitCode}. Error: {error}");
                throw new Exception($"Script exited with code {process.ExitCode}: {error}");
            }
            
            return output;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running python script.");
            throw;
        }
    }
}

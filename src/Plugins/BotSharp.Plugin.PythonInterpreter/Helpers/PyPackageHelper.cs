using System.Diagnostics;
using System.Threading.Tasks;

namespace BotSharp.Plugin.PythonInterpreter.Helpers;

internal static class PyPackageHelper
{
    /// <summary>
    /// Install python packages
    /// </summary>
    /// <param name="packages"></param>
    /// <returns></returns>
    internal static async Task<PackageInstallResult> InstallPackages(List<string>? packages)
    {
        if (packages.IsNullOrEmpty())
        {
            return new PackageInstallResult { Success = true };
        }

        try
        {
            var packageList = string.Join(" ", packages);
            var startInfo = new ProcessStartInfo
            {
                FileName = "pip",
                Arguments = $"install {packageList}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return new PackageInstallResult
                {
                    Success = false,
                    ErrorMsg = "Failed to start pip process"
                };
            }

            await process.WaitForExitAsync();
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            if (process.ExitCode == 0)
            {
                return new PackageInstallResult { Success = true };
            }
            else
            {
                var errorMsg = $"Failed to install packages. Exit code: {process.ExitCode}. Error: {error}";
                return new PackageInstallResult
                {
                    Success = false,
                    ErrorMsg = errorMsg
                };
            }
        }
        catch (Exception ex)
        {
            var errorMsg = $"Exception occurred while installing packages: {ex.Message}";
            return new PackageInstallResult
            {
                Success = false,
                ErrorMsg = errorMsg
            };
        }
    }

    /// <summary>
    /// Get packages that are not installed
    /// </summary>
    /// <param name="packages"></param>
    /// <returns></returns>
    public static async Task<List<string>> GetUninstalledPackages(List<string>? packages)
    {
        var missingPackages = new List<string>();

        if (packages.IsNullOrEmpty())
        {
            return missingPackages;
        }

        try
        {
            var installedPackages = await GetInstalledPackages();
            foreach (var package in packages)
            {
                // Check for common package name mappings
                var mappedPackageName = MapToPackageName(package);
                var isInstalled = installedPackages.Any(x => x.IsEqualTo(mappedPackageName));
                if (!isInstalled)
                {
                    missingPackages.Add(mappedPackageName);
                }
            }
            return missingPackages;
        }
        catch (Exception ex)
        {
            throw;
        }
    }


    private static async Task<List<string>> GetInstalledPackages()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "pip",
                Arguments = "list --format=freeze",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                throw new InvalidOperationException("Failed to start pip process");
            }

            await process.WaitForExitAsync();
            var output = await process.StandardOutput.ReadToEndAsync();

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                throw new InvalidOperationException($"pip list failed with exit code {process.ExitCode}: {error}");
            }

            // Parse pip list output (format: package==version)
            var packages = output.Split("\n", StringSplitOptions.RemoveEmptyEntries)
                                 .Select(line => line.Split("==", StringSplitOptions.None)[0].Trim().ToLowerInvariant())
                                 .Where(pkg => !string.IsNullOrEmpty(pkg))
                                 .ToList();
            return packages;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <summary>
    /// Map common import name to actual package name
    /// </summary>
    /// <param name="importName"></param>
    /// <returns></returns>
    private static string MapToPackageName(string importName)
    {
        var packageMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "cv2", "opencv-python" },
            { "PIL", "Pillow" },
            { "sklearn", "scikit-learn" },
            { "yaml", "PyYAML" },
            { "bs4", "beautifulsoup4" },
            { "dateutil", "python-dateutil" },
            { "serial", "pyserial" },
            { "psutil", "psutil" },
            { "requests", "requests" },
            { "numpy", "numpy" },
            { "pandas", "pandas" },
            { "matplotlib", "matplotlib" },
            { "scipy", "scipy" },
            { "seaborn", "seaborn" },
            { "plotly", "plotly" }
        };

        return packageMappings.TryGetValue(importName, out var actualName) ? actualName : importName;
    }
}

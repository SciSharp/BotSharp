using System.Text;

namespace BotSharp.Plugin.AgentSkills.Skills;

internal class AgentSkillReader
{
    internal AgentSkill ReadSkill(string path)
    {
        string folderPath = Path.GetDirectoryName(path)!;
        string[] lines = File.ReadAllLines(path, Encoding.UTF8);
        bool firstFrontMatterDelimiterRead = false;
        string name = string.Empty;
        string description = string.Empty;
        string? license = null;
        string? compatibility = null;
        string? allowedTools = null;
        string body = string.Empty;
        Dictionary<string, string>? metadata = null;
        bool readingMetadata = false;
        for (int lineNumber = 0; lineNumber < lines.Length; lineNumber++)
        {
            string line = lines[lineNumber].Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (line == "---")
            {
                if (!firstFrontMatterDelimiterRead)
                {
                    firstFrontMatterDelimiterRead = true;
                    continue;
                }

                //Not first matter delimiter so must be second matter delimiter so rest of file is body
                for (int lineNumberAfterFrontMatter = lineNumber + 1; lineNumberAfterFrontMatter < lines.Length; lineNumberAfterFrontMatter++)
                {
                    body += lines[lineNumberAfterFrontMatter] + Environment.NewLine;
                }

                break;
            }

            if (line.StartsWith("name"))
            {
                name = ReadLineValue(line);
                readingMetadata = false;
                continue;
            }

            if (line.StartsWith("description"))
            {
                description = ReadLineValue(line);
                readingMetadata = false;
                continue;
            }

            if (line.StartsWith("compatibility"))
            {
                compatibility = ReadLineValue(line);
                readingMetadata = false;
                continue;
            }

            if (line.StartsWith("allowed-tools"))
            {
                allowedTools = ReadLineValue(line);
                readingMetadata = false;
                continue;
            }

            if (line.StartsWith("license"))
            {
                license = ReadLineValue(line);
                readingMetadata = false;
                continue;
            }

            if (line.StartsWith("metadata"))
            {
                readingMetadata = true;
                continue;
            }

            if (readingMetadata)
            {
                ReadMetadata(line);
            }
        }

        string[] allFiles = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);
        string[] scriptFiles = GetFilesIfFolderExist("scripts");
        string[] referenceFiles = GetFilesIfFolderExist("references");
        string[] assetFiles = GetFilesIfFolderExist("assets");
        string[] otherFiles = allFiles.Except(scriptFiles).Except(referenceFiles).Except(assetFiles).Except([path]).ToArray();

        if (string.IsNullOrWhiteSpace(body))
        {
            //Seems files content do not follow any standard, so let's assume entire file is the body (and return it with full file as body)
            body = File.ReadAllText(path, Encoding.UTF8);
        }

        return new AgentSkill
        {
            FolderPath = folderPath,
            Name = name,
            Description = description,
            License = license,
            Body = body.Trim(),
            ScriptFiles = scriptFiles,
            ReferenceFiles = referenceFiles,
            AssetFiles = assetFiles,
            OtherFiles = otherFiles,
            Compatibility = compatibility,
            Metadata = metadata,
            AllowedTools = allowedTools
        };

        void ReadMetadata(string line)
        {
            string[] parts = line.Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
            {
                metadata ??= [];
                metadata.Add(parts[0].Trim(), parts[1].Trim());
            }
        }

        static string ReadLineValue(string line)
        {
            string value = string.Empty;
            int index = line.IndexOf(":", StringComparison.Ordinal);
            if (index != -1 && line.Length - 1 > index)
            {
                value = line[(index + 1)..].Trim();
            }

            return value;
        }

        string[] GetFilesIfFolderExist(string knownSubFolder)
        {
            string subFolder = Path.Combine(folderPath, knownSubFolder);
            if (!Directory.Exists(subFolder))
            {
                return [];
            }

            return Directory.GetFiles(subFolder, "*", SearchOption.AllDirectories);
        }
    }
}

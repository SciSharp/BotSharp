using BotSharp.Abstraction.Statistics.Model;
using System.IO;

namespace BotSharp.Core.Repository
{
    public partial class FileRepository
    {
        public void IncrementConversationCount()
        {
            var statsFileDirectory = FindCurrentStatsDirectory();
            if (statsFileDirectory == null)
            {
                statsFileDirectory = CreateStatsFileDirectory();
            }
            var fileName = GenerateStatsFileName();
            var statsFile = Path.Combine(statsFileDirectory, fileName);
            if (!File.Exists(statsFile))
            {
                File.WriteAllText(statsFile, JsonSerializer.Serialize(new Statistics()
                {
                    Id = Guid.NewGuid().ToString(),
                    UpdatedDateTime = DateTime.UtcNow
                }, _options));
            }
            var json = File.ReadAllText(statsFile);
            var stats = JsonSerializer.Deserialize<Statistics>(json, _options);
            stats.ConversationCount += 1;
            stats.UpdatedDateTime = DateTime.UtcNow;
            File.WriteAllText(statsFile, JsonSerializer.Serialize(stats, _options));
        }
        public string? CreateStatsFileDirectory()
        {
            var dir = GenerateStatsDirectoryName();
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return dir;
        }
        private string? FindCurrentStatsDirectory()
        {
            var dir = GenerateStatsDirectoryName();
            if (!Directory.Exists(dir)) return null;

            return dir;
        }
        private string GenerateStatsDirectoryName()
        {
            return Path.Combine(_dbSettings.FileRepository, _statisticsSetting.DataDir, DateTime.UtcNow.Year.ToString(), DateTime.UtcNow.ToString("MM"));
        }
        private string GenerateStatsFileName()
        {
            var fileName = DateTime.UtcNow.ToString("MMdd");
            return $"{fileName}-{STATS_FILE}";
        }
    }
}

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using BotSharp.Plugin.AgentSkills.Services;
using BotSharp.Plugin.AgentSkills.Settings;
using Xunit;
using Assert = Xunit.Assert;

namespace BotSharp.UnitTest
{
    public class AgentSkillsTest
    {
        [Fact]
        public async Task TestGetAvailableSkills()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), "BotSharpTests", "Skills");
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            Directory.CreateDirectory(tempDir);
            
            // Create a dummy SKILL.md
            var skillDir = Path.Combine(tempDir, "TestSkill");
            Directory.CreateDirectory(skillDir);
            var skillFile = Path.Combine(skillDir, "SKILL.md");
            var content = @"---
name: TestSkill
description: This is a test skill
version: 1.0.0
---
System Prompt";
            await File.WriteAllTextAsync(skillFile, content);

            // Mock Settings and Logger
            var settings = new AgentSkillsSettings { DataDir = tempDir };
            var loggerMock = new Mock<ILogger<AgentSkillService>>();

            // Act
            var service = new AgentSkillService(settings, loggerMock.Object);
            // Service constructor calls RefreshSkills().Wait(), so skils should be loaded.
            
            var skills = await service.GetAvailableSkills();

            // Assert
            Assert.Single(skills);
            Assert.Equal("TestSkill", skills.First().Name);
            Assert.Equal("This is a test skill", skills.First().Description);

            // Cleanup
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }

        [Fact]
        public async Task TestGetSkillDetails()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), "BotSharpTests", "Skills2");
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            Directory.CreateDirectory(tempDir);

            // Create a dummy SKILL.md
            var skillDir = Path.Combine(tempDir, "MathSkill");
            Directory.CreateDirectory(skillDir);
            var skillFile = Path.Combine(skillDir, "SKILL.md");
            var content = @"---
name: MathSkill
description: Performs math operations
version: 1.0.0
---
You are a math expert.";
            await File.WriteAllTextAsync(skillFile, content);

            // Mock Settings and Logger
            var settings = new AgentSkillsSettings { DataDir = tempDir };
            var loggerMock = new Mock<ILogger<AgentSkillService>>();

            var service = new AgentSkillService(settings, loggerMock.Object);
            
            // Act
            var skill = await service.GetSkill("MathSkill");

            // Assert
            Assert.NotNull(skill);
            Assert.Equal("MathSkill", skill.Name);
            Assert.Equal("You are a math expert.", skill.MarkdownBody);

             // Cleanup
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }
}

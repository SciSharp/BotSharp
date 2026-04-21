namespace BotSharp.Plugin.AgentSkills.Tests;

/// <summary>
/// Base class for all Agent Skills tests
/// Provides common setup and utilities
/// </summary>
public abstract class TestBase : IDisposable
{
    protected IServiceProvider ServiceProvider { get; private set; }
    protected IConfiguration Configuration { get; private set; }
    protected string TestSkillsDirectory { get; private set; }

    protected TestBase()
    {
        // Setup configuration
        Configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.test.json", optional: false)
            .Build();

        // Setup test skills directory
        TestSkillsDirectory = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..",
            "..",
            "..",
            "..",
            "test-skills"
        );

        // Ensure test skills directory exists
        if (!Directory.Exists(TestSkillsDirectory))
        {
            throw new DirectoryNotFoundException(
                $"Test skills directory not found: {TestSkillsDirectory}"
            );
        }

        // Setup service provider
        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    /// Configure services for testing
    /// Override in derived classes to add specific services
    /// </summary>
    protected virtual void ConfigureServices(IServiceCollection services)
    {
        // Add configuration
        services.AddSingleton(Configuration);

        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConfiguration(Configuration.GetSection("Logging"));
            builder.AddConsole();
            builder.AddDebug();
        });
    }

    /// <summary>
    /// Get a service from the service provider
    /// </summary>
    protected T GetService<T>() where T : notnull
    {
        return ServiceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Get the full path to a test skill
    /// </summary>
    protected string GetTestSkillPath(string skillName)
    {
        return Path.Combine(TestSkillsDirectory, skillName);
    }

    /// <summary>
    /// Verify that a test skill exists
    /// </summary>
    protected void AssertTestSkillExists(string skillName)
    {
        var skillPath = GetTestSkillPath(skillName);
        var skillFile = Path.Combine(skillPath, "SKILL.md");
        
        Directory.Exists(skillPath).Should().BeTrue(
            $"Test skill directory should exist: {skillPath}"
        );
        
        File.Exists(skillFile).Should().BeTrue(
            $"SKILL.md file should exist: {skillFile}"
        );
    }

    public virtual void Dispose()
    {
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
        
        GC.SuppressFinalize(this);
    }
}

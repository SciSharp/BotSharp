# BotSharp.Plugin.AgentSkills.Tests

Unit and integration tests for the Agent Skills plugin.

## Project Structure

```
BotSharp.Plugin.AgentSkills.Tests/
├── BotSharp.Plugin.AgentSkills.Tests.csproj  # Test project file
├── appsettings.test.json                      # Test configuration
├── Usings.cs                                  # Global using directives
├── TestBase.cs                                # Base class for all tests
├── TestSetupTests.cs                          # Tests verifying test setup
└── README.md                                  # This file
```

## Dependencies

### Test Framework
- **xUnit**: Test framework
- **xunit.runner.visualstudio**: Visual Studio test runner

### Assertion Library
- **FluentAssertions**: Fluent assertion library for readable tests

### Mocking Framework
- **Moq**: Mocking framework for creating test doubles

### Code Coverage
- **coverlet.collector**: Code coverage collector
- **coverlet.msbuild**: MSBuild integration for code coverage

### Property-Based Testing (Optional)
- **CsCheck**: Property-based testing library

### Configuration & DI
- **Microsoft.Extensions.Configuration**: Configuration support
- **Microsoft.Extensions.DependencyInjection**: Dependency injection
- **Microsoft.Extensions.Logging**: Logging support

## Running Tests

### Run All Tests
```bash
dotnet test
```

### Run Specific Test Class
```bash
dotnet test --filter "FullyQualifiedName~TestSetupTests"
```

### Run with Code Coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Generate Coverage Report
```bash
# Install ReportGenerator (once)
dotnet tool install -g dotnet-reportgenerator-globaltool

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Generate HTML report
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html
```

## Test Configuration

Test configuration is in `appsettings.test.json`:

```json
{
  "AgentSkills": {
    "EnableProjectSkills": true,
    "ProjectSkillsDir": "../../test-skills",
    "MaxOutputSizeBytes": 51200
  }
}
```

## Test Skills

Tests use skills from `tests/test-skills/`:
- **valid-skill**: Complete skill with all features
- **minimal-skill**: Minimal skill with only required elements
- **skill-with-scripts**: Skill with Python and Bash scripts
- **large-content-skill**: Skill with large SKILL.md (> 50KB)

## Writing Tests

### Basic Test Structure

```csharp
public class MyTests : TestBase
{
    [Fact]
    public void MyTest_ShouldDoSomething()
    {
        // Arrange
        var service = GetService<IMyService>();

        // Act
        var result = service.DoSomething();

        // Assert
        result.Should().NotBeNull();
    }
}
```

### Using Test Skills

```csharp
[Fact]
public void Test_WithValidSkill()
{
    // Arrange
    AssertTestSkillExists("valid-skill");
    var skillPath = GetTestSkillPath("valid-skill");

    // Act & Assert
    // ... your test logic
}
```

### Mocking Dependencies

```csharp
[Fact]
public void Test_WithMock()
{
    // Arrange
    var mockService = new Mock<IMyService>();
    mockService.Setup(s => s.GetData()).Returns("test data");

    // Act
    var result = mockService.Object.GetData();

    // Assert
    result.Should().Be("test data");
}
```

### Property-Based Testing

```csharp
[Fact]
public void Property_Test()
{
    Gen.Int.Sample(i =>
    {
        // Property: some condition should always hold
        var result = MyFunction(i);
        result.Should().BeGreaterThanOrEqualTo(0);
    });
}
```

## Test Categories

Tests are organized by functionality:

1. **Setup Tests** (`TestSetupTests.cs`): Verify test environment
2. **Configuration Tests**: Test AgentSkillsSettings
3. **Service Tests**: Test SkillService implementation
4. **Adapter Tests**: Test AIToolCallbackAdapter
5. **Hook Tests**: Test instruction and function hooks
6. **Integration Tests**: End-to-end workflow tests
7. **Property Tests** (optional): Property-based tests

## Code Coverage Goals

- **Target**: > 80% code coverage
- **Critical paths**: 100% coverage
- **Error handling**: All error paths tested

## Best Practices

1. **Use TestBase**: Inherit from TestBase for common setup
2. **Use FluentAssertions**: Write readable assertions
3. **Test one thing**: Each test should verify one behavior
4. **Arrange-Act-Assert**: Follow AAA pattern
5. **Descriptive names**: Test names should describe what they test
6. **Mock external dependencies**: Use Moq for external services
7. **Test error cases**: Don't just test happy paths
8. **Use test data**: Use test skills for realistic scenarios

## Continuous Integration

Tests run automatically on:
- Pull requests
- Commits to main branch
- Nightly builds

CI configuration includes:
- Run all tests
- Generate code coverage
- Fail if coverage < 80%
- Fail if any test fails

## Troubleshooting

### Test Skills Not Found
Ensure test skills directory exists:
```bash
ls tests/test-skills/
```

### Configuration Not Loaded
Check `appsettings.test.json` is copied to output:
```xml
<None Update="appsettings.test.json">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</None>
```

### Package Version Conflicts
Check `Directory.Packages.props` for centralized package versions.

## Resources

- [xUnit Documentation](https://xunit.net/)
- [FluentAssertions Documentation](https://fluentassertions.com/)
- [Moq Documentation](https://github.com/moq/moq4)
- [CsCheck Documentation](https://github.com/AnthonyLloyd/CsCheck)
- [Agent Skills Specification](https://agentskills.io)

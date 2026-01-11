#pragma warning disable IDE0005 // Using directive is unnecessary - false positive
using Acode.Application.Configuration;
using Acode.Domain.Configuration;
using Acode.Infrastructure.Configuration;
#pragma warning restore IDE0005
using FluentAssertions;

namespace Acode.Integration.Tests.Configuration;

/// <summary>
/// Integration tests for configuration pipeline.
/// Tests end-to-end config loading, validation, and processing.
/// </summary>
public class ConfigurationIntegrationTests
{
    [Fact]
    public async Task EndToEnd_LoadConfigWithDefaults_ShouldApplyDefaults()
    {
        // Arrange - minimal config file
        var tempFile = Path.GetTempFileName();
        var yaml = "schema_version: \"1.0.0\"\n";

        try
        {
            await File.WriteAllTextAsync(tempFile, yaml).ConfigureAwait(true);

            var reader = new YamlConfigReader();
            var defaultApplicator = new DefaultValueApplicator();
            var validator = new ConfigValidator();

            // Act - full pipeline
            var config = await reader.ReadAsync(tempFile).ConfigureAwait(true);
            var configWithDefaults = defaultApplicator.Apply(config);
            var validationResult = validator.Validate(configWithDefaults!);

            // Assert
            validationResult.IsValid.Should().BeTrue();
            configWithDefaults!.Mode!.Default.Should().Be("local-only", "default mode should be applied");
            configWithDefaults.Model!.Provider.Should().Be("ollama", "default model provider should be applied");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task EndToEnd_LoadConfigWithEnvInterpolation_ShouldInterpolateVariables()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        Environment.SetEnvironmentVariable("TEST_MODEL_NAME", "codellama:13b");
        var yaml = @"
schema_version: ""1.0.0""
model:
  name: ""${TEST_MODEL_NAME}""
";

        try
        {
            await File.WriteAllTextAsync(tempFile, yaml).ConfigureAwait(true);

            var reader = new YamlConfigReader();
            var interpolator = new EnvironmentInterpolator();

            // Act
            var yamlContent = await File.ReadAllTextAsync(tempFile).ConfigureAwait(true);
            var interpolatedYaml = interpolator.Interpolate(yamlContent);
            var config = reader.Read(interpolatedYaml!);

            // Assert
            config.Model!.Name.Should().Be("codellama:13b", "environment variable should be interpolated");
        }
        finally
        {
            File.Delete(tempFile);
            Environment.SetEnvironmentVariable("TEST_MODEL_NAME", null);
        }
    }

    [Fact]
    public async Task Integration_LocalOnlyMode_ShouldEnforceConstraints()
    {
        // Arrange - LocalOnly mode with default Ollama provider
        var tempFile = Path.GetTempFileName();
        var yaml = @"
schema_version: ""1.0.0""
mode:
  default: ""local-only""
  allow_burst: false
model:
  provider: ""ollama""
";

        try
        {
            await File.WriteAllTextAsync(tempFile, yaml).ConfigureAwait(true);

            var reader = new YamlConfigReader();
            var validator = new ConfigValidator();

            // Act
            var config = await reader.ReadAsync(tempFile).ConfigureAwait(true);
            var validationResult = validator.Validate(config);

            // Assert
            validationResult.IsValid.Should().BeTrue("LocalOnly mode with Ollama should be valid");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task Integration_AirgappedMode_ShouldEnforceConstraints()
    {
        // Arrange - Airgapped mode (most restrictive)
        var tempFile = Path.GetTempFileName();
        var yaml = @"
schema_version: ""1.0.0""
mode:
  default: ""airgapped""
  airgapped_lock: true
model:
  provider: ""ollama""
";

        try
        {
            await File.WriteAllTextAsync(tempFile, yaml).ConfigureAwait(true);

            var reader = new YamlConfigReader();
            var validator = new ConfigValidator();

            // Act
            var config = await reader.ReadAsync(tempFile).ConfigureAwait(true);
            var validationResult = validator.Validate(config);

            // Assert
            validationResult.IsValid.Should().BeTrue("Airgapped mode with lock should be valid");
            config.Mode!.AirgappedLock.Should().BeTrue("airgapped_lock should be set");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task Integration_BurstMode_ShouldAllowExternalProviders()
    {
        // Arrange - Burst mode is allowed when allow_burst is true
        var tempFile = Path.GetTempFileName();
        var yaml = @"
schema_version: ""1.0.0""
mode:
  default: ""local-only""
  allow_burst: true
model:
  provider: ""ollama""
";

        try
        {
            await File.WriteAllTextAsync(tempFile, yaml).ConfigureAwait(true);

            var reader = new YamlConfigReader();
            var validator = new ConfigValidator();

            // Act
            var config = await reader.ReadAsync(tempFile).ConfigureAwait(true);
            var validationResult = validator.Validate(config);

            // Assert
            validationResult.IsValid.Should().BeTrue("Config with allow_burst=true should be valid");
            config.Mode!.AllowBurst.Should().BeTrue("allow_burst should be true");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task Integration_ConcurrentConfigLoads_ShouldBeThreadSafe()
    {
        // Arrange - test thread safety with concurrent loads
        var tempFile = Path.GetTempFileName();
        var yaml = @"
schema_version: ""1.0.0""
mode:
  default: ""local-only""
";

        try
        {
            await File.WriteAllTextAsync(tempFile, yaml).ConfigureAwait(true);

            var reader = new YamlConfigReader();
            var validator = new ConfigValidator();

            // Act - 10 concurrent loads
            var tasks = Enumerable.Range(0, 10)
                .Select(async _ =>
                {
                    var config = await reader.ReadAsync(tempFile).ConfigureAwait(true);
                    var result = validator.Validate(config);
                    return result;
                })
                .ToArray();

            var results = await Task.WhenAll(tasks).ConfigureAwait(true);

            // Assert - all validations should succeed
            results.Should().OnlyContain(r => r.IsValid);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task Integration_ValidationWithRealYamlFile_ShouldReportErrors()
    {
        // Arrange - invalid config with multiple errors
        var tempFile = Path.GetTempFileName();
        var yaml = @"
schema_version: ""1.0.0""
mode:
  default: ""burst""
  airgapped_lock: true
";

        try
        {
            await File.WriteAllTextAsync(tempFile, yaml).ConfigureAwait(true);

            var reader = new YamlConfigReader();
            var validator = new ConfigValidator();

            // Act
            var config = await reader.ReadAsync(tempFile).ConfigureAwait(true);
            var validationResult = validator.Validate(config);

            // Assert - should have errors due to default burst + airgapped_lock conflict
            validationResult.IsValid.Should().BeFalse("default cannot be burst per FR-002b-51");
            validationResult.Errors.Should().Contain(e => e.Code == "INVALID_DEFAULT_MODE");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task Integration_ErrorReportingWithRealFile_ShouldIncludeFilePath()
    {
        // Arrange - config that will fail schema validation
        var tempFile = Path.GetTempFileName();
        var invalidYaml = "schema_version: [1, 2, 3]"; // Wrong type

        try
        {
            await File.WriteAllTextAsync(tempFile, invalidYaml).ConfigureAwait(true);

            var reader = new YamlConfigReader();

            // Act & Assert - should throw with file path in error
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await reader.ReadAsync(tempFile).ConfigureAwait(true)).ConfigureAwait(true);

            ex.Message.Should().Contain("line", "error should include line number");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task Integration_DotNetProjectConfig_ShouldValidate()
    {
        // Arrange - typical .NET project config
        var tempFile = Path.GetTempFileName();
        var yaml = @"
schema_version: ""1.0.0""
project:
  name: ""MyDotNetApp""
  type: ""dotnet""
  languages:
    - ""csharp""
mode:
  default: ""local-only""
  allow_burst: false
model:
  provider: ""ollama""
  name: ""codellama:7b""
commands:
  build: ""dotnet build""
  test: ""dotnet test""
ignore:
  patterns:
    - ""bin/""
    - ""obj/""
    - ""*.user""
";

        try
        {
            await File.WriteAllTextAsync(tempFile, yaml).ConfigureAwait(true);

            var reader = new YamlConfigReader();
            var defaultApplicator = new DefaultValueApplicator();
            var validator = new ConfigValidator();

            // Act
            var config = await reader.ReadAsync(tempFile).ConfigureAwait(true);
            var configWithDefaults = defaultApplicator.Apply(config);
            var validationResult = validator.Validate(configWithDefaults!);

            // Assert
            validationResult.IsValid.Should().BeTrue();
            config.Project!.Type.Should().Be("dotnet");
            config.Project.Languages.Should().Contain("csharp");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task Integration_NodeJsProjectConfig_ShouldValidate()
    {
        // Arrange - typical Node.js project config
        var tempFile = Path.GetTempFileName();
        var yaml = @"
schema_version: ""1.0.0""
project:
  name: ""my-node-app""
  type: ""node""
  languages:
    - ""typescript""
    - ""javascript""
mode:
  default: ""local-only""
model:
  provider: ""ollama""
  name: ""codellama:13b""
commands:
  build: ""npm run build""
  test: ""npm test""
  lint: ""npm run lint""
ignore:
  patterns:
    - ""node_modules/""
    - ""dist/""
    - ""coverage/""
";

        try
        {
            await File.WriteAllTextAsync(tempFile, yaml).ConfigureAwait(true);

            var reader = new YamlConfigReader();
            var validator = new ConfigValidator();

            // Act
            var config = await reader.ReadAsync(tempFile).ConfigureAwait(true);
            var validationResult = validator.Validate(config);

            // Assert
            validationResult.IsValid.Should().BeTrue();
            config.Project!.Type.Should().Be("node");
            config.Project.Languages.Should().Contain("typescript");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task Integration_PythonProjectConfig_ShouldValidate()
    {
        // Arrange - typical Python project config
        var tempFile = Path.GetTempFileName();
        var yaml = @"
schema_version: ""1.0.0""
project:
  name: ""my-python-app""
  type: ""python""
  languages:
    - ""python""
mode:
  default: ""local-only""
model:
  provider: ""ollama""
  name: ""codellama:34b""
commands:
  build: ""poetry build""
  test: ""pytest""
  lint: ""ruff check .""
ignore:
  patterns:
    - ""__pycache__/""
    - ""*.pyc""
    - "".venv/""
    - ""dist/""
";

        try
        {
            await File.WriteAllTextAsync(tempFile, yaml).ConfigureAwait(true);

            var reader = new YamlConfigReader();
            var validator = new ConfigValidator();

            // Act
            var config = await reader.ReadAsync(tempFile).ConfigureAwait(true);
            var validationResult = validator.Validate(config);

            // Assert
            validationResult.IsValid.Should().BeTrue();
            config.Project!.Type.Should().Be("python");
            config.Commands!.Test.Should().Be("pytest");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task Integration_ConfigWithAllOptionalFields_ShouldValidate()
    {
        // Arrange - config with ALL optional fields populated
        var tempFile = Path.GetTempFileName();
        var yaml = @"
schema_version: ""1.0.0""
project:
  name: ""comprehensive-project""
  type: ""dotnet""
  description: ""A project with all optional fields""
  version: ""1.0.0""
  languages:
    - ""csharp""
    - ""fsharp""
  root: ""./src""
  paths:
    source: ""./src""
    tests: ""./tests""
    output: ""./bin""
mode:
  default: ""local-only""
  allow_burst: true
  airgapped_lock: false
model:
  provider: ""ollama""
  name: ""codellama:13b""
  endpoint: ""http://localhost:11434""
  timeout_seconds: 120
  retry_count: 3
  parameters:
    temperature: 0.7
    max_tokens: 4096
    top_p: 0.95
commands:
  build: ""dotnet build""
  test: ""dotnet test""
  lint: ""dotnet format --verify-no-changes""
  run: ""dotnet run""
  clean: ""dotnet clean""
ignore:
  patterns:
    - ""bin/""
    - ""obj/""
    - ""*.user""
    - "".vs/""
    - ""temp/""
    - ""logs/""
";

        try
        {
            await File.WriteAllTextAsync(tempFile, yaml).ConfigureAwait(true);

            var reader = new YamlConfigReader();
            var validator = new ConfigValidator();

            // Act
            var config = await reader.ReadAsync(tempFile).ConfigureAwait(true);
            var validationResult = validator.Validate(config);

            // Assert
            var errorMessages = string.Join(", ", validationResult.Errors.Select(e => $"{e.Code}: {e.Message}"));
            validationResult.IsValid.Should().BeTrue($"config with all optional fields should validate. Errors: {errorMessages}");
            config.Project!.Name.Should().Be("comprehensive-project");
            config.Mode!.Default.Should().Be("local-only");
            config.Model!.Parameters.Temperature.Should().Be(0.7);
            config.Commands!.Build.Should().Be("dotnet build");
            config.Ignore!.Patterns.Should().HaveCount(6, "should have 6 ignore patterns");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task Integration_FileNotFound_ShouldReportError()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent-{Guid.NewGuid()}.yml");
        var validator = new ConfigValidator();

        // Act
        var result = await validator.ValidateFileAsync(nonExistentPath).ConfigureAwait(true);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ConfigErrorCodes.FileNotFound);
    }

    [Fact]
    public async Task Integration_FileSizeExceedsLimit_ShouldReportError()
    {
        // Arrange - create file >1MB
        var tempFile = Path.GetTempFileName();

        try
        {
            var largeContent = "schema_version: \"1.0.0\"\n" + new string('#', 1_048_577);
            await File.WriteAllTextAsync(tempFile, largeContent).ConfigureAwait(true);

            var reader = new YamlConfigReader();

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await reader.ReadAsync(tempFile).ConfigureAwait(true)).ConfigureAwait(true);

            ex.Message.Should().Contain("exceeds maximum size of 1MB");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task Integration_ReloadAfterFileChange_ShouldLoadNewContent()
    {
        // Arrange - test config reload scenario
        var tempFile = Path.GetTempFileName();
        var yaml1 = @"
schema_version: ""1.0.0""
project:
  name: ""original-name""
";
        var yaml2 = @"
schema_version: ""1.0.0""
project:
  name: ""updated-name""
";

        try
        {
            await File.WriteAllTextAsync(tempFile, yaml1).ConfigureAwait(true);
            var reader = new YamlConfigReader();

            // Act - load original
            var config1 = await reader.ReadAsync(tempFile).ConfigureAwait(true);

            // Update file
            await File.WriteAllTextAsync(tempFile, yaml2).ConfigureAwait(true);

            // Reload
            var config2 = await reader.ReadAsync(tempFile).ConfigureAwait(true);

            // Assert
            config1.Project!.Name.Should().Be("original-name");
            config2.Project!.Name.Should().Be("updated-name", "config should reflect file change");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}

#pragma warning disable IDE0005 // Using directive is unnecessary - false positive from analyzer
using Acode.Domain.Configuration;
#pragma warning restore IDE0005
using Acode.Infrastructure.Configuration;
using FluentAssertions;

namespace Acode.Infrastructure.Tests.Configuration;

/// <summary>
/// Tests for YamlConfigReader.
/// Verifies YAML deserialization to AcodeConfig domain model.
/// </summary>
public class YamlConfigReaderTests
{
    [Fact]
    public async Task ReadAsync_WithValidMinimalYaml_ShouldDeserializeCorrectly()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var yaml = @"
schema_version: ""1.0.0""
project:
  name: ""test-project""
";
        await File.WriteAllTextAsync(tempFile, yaml).ConfigureAwait(true);
        var reader = new YamlConfigReader();

        try
        {
            // Act
            var config = await reader.ReadAsync(tempFile).ConfigureAwait(true);

            // Assert
            config.Should().NotBeNull();
            config.SchemaVersion.Should().Be("1.0.0");
            config.Project.Should().NotBeNull();
            config.Project!.Name.Should().Be("test-project");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ReadAsync_WithFileNotFound_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var reader = new YamlConfigReader();
        var nonExistentFile = "/tmp/does-not-exist.yml";

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            async () => await reader.ReadAsync(nonExistentFile).ConfigureAwait(true)).ConfigureAwait(true);
    }

    [Fact]
    public void Read_WithValidYamlString_ShouldDeserializeCorrectly()
    {
        // Arrange
        var yaml = @"
schema_version: ""1.0.0""
project:
  name: ""test-project""
  type: ""dotnet""
  description: ""A test project""
mode:
  default: ""local-only""
  allow_burst: true
";
        var reader = new YamlConfigReader();

        // Act
        var config = reader.Read(yaml);

        // Assert
        config.Should().NotBeNull();
        config.SchemaVersion.Should().Be("1.0.0");
        config.Project!.Name.Should().Be("test-project");
        config.Project.Type.Should().Be("dotnet");
        config.Mode!.Default.Should().Be("local-only");
        config.Mode.AllowBurst.Should().BeTrue();
    }

    [Fact]
    public void Read_WithUnderscoreNamingConvention_ShouldMapToPascalCase()
    {
        // Arrange - YAML uses snake_case
        var yaml = @"
schema_version: ""1.0.0""
mode:
  default: ""local-only""
  allow_burst: true
  airgapped_lock: false
model:
  provider: ""ollama""
  timeout_seconds: 120
  retry_count: 3
";
        var reader = new YamlConfigReader();

        // Act
        var config = reader.Read(yaml);

        // Assert - C# model uses PascalCase
        config.Mode!.AllowBurst.Should().BeTrue();
        config.Mode.AirgappedLock.Should().BeFalse();
        config.Model!.TimeoutSeconds.Should().Be(120);
        config.Model.RetryCount.Should().Be(3);
    }

    [Fact]
    public void Read_WithModelParameters_ShouldDeserializeCorrectly()
    {
        // Arrange
        var yaml = @"
schema_version: ""1.0.0""
model:
  parameters:
    temperature: 0.8
    max_tokens: 8192
    top_p: 0.9
";
        var reader = new YamlConfigReader();

        // Act
        var config = reader.Read(yaml);

        // Assert
        config.Model!.Parameters.Should().NotBeNull();
        config.Model.Parameters.Temperature.Should().Be(0.8);
        config.Model.Parameters.MaxTokens.Should().Be(8192);
        config.Model.Parameters.TopP.Should().Be(0.9);
    }

    [Fact]
    public void Read_WithCommandsStringFormat_ShouldDeserializeAsString()
    {
        // Arrange
        var yaml = @"
schema_version: ""1.0.0""
commands:
  build: ""npm run build""
  test: ""npm test""
";
        var reader = new YamlConfigReader();

        // Act
        var config = reader.Read(yaml);

        // Assert
        config.Commands.Should().NotBeNull();
        config.Commands!.Build.Should().BeOfType<string>();
        config.Commands.Build.Should().Be("npm run build");
        config.Commands.Test.Should().BeOfType<string>();
        config.Commands.Test.Should().Be("npm test");
    }

    [Fact]
    public void Read_WithIgnorePatterns_ShouldDeserializeCorrectly()
    {
        // Arrange
        var yaml = @"
schema_version: ""1.0.0""
ignore:
  patterns:
    - ""*.log""
    - ""node_modules/""
    - "".env""
";
        var reader = new YamlConfigReader();

        // Act
        var config = reader.Read(yaml);

        // Assert
        config.Ignore.Should().NotBeNull();
        config.Ignore!.Patterns.Should().NotBeNull();
        config.Ignore.Patterns.Should().HaveCount(3);
        config.Ignore.Patterns.Should().Contain("*.log");
        config.Ignore.Patterns.Should().Contain("node_modules/");
        config.Ignore.Patterns.Should().Contain(".env");
    }

    [Fact]
    public void Read_WithUnmatchedProperties_ShouldIgnoreThem()
    {
        // Arrange - YAML has extra fields not in model
        var yaml = @"
schema_version: ""1.0.0""
project:
  name: ""test""
  unknown_field: ""this should be ignored""
extra_section:
  foo: ""bar""
";
        var reader = new YamlConfigReader();

        // Act
        var config = reader.Read(yaml);

        // Assert - should not throw, extra fields ignored
        config.Should().NotBeNull();
        config.SchemaVersion.Should().Be("1.0.0");
        config.Project!.Name.Should().Be("test");
    }

    [Fact]
    public void Read_WithMinimalYaml_ShouldDeserializeWithNullNestedObjects()
    {
        // Arrange - minimal YAML, no nested objects
        var yaml = @"
schema_version: ""1.0.0""
";
        var reader = new YamlConfigReader();

        // Act
        var config = reader.Read(yaml);

        // Assert - YamlConfigReader only deserializes, does not apply defaults
        // Defaults are applied by DefaultValueApplicator later in the pipeline
        config.SchemaVersion.Should().Be("1.0.0");
        config.Mode.Should().BeNull();  // Not in YAML, so null
        config.Model.Should().BeNull(); // Not in YAML, so null
    }

    [Fact]
    public void Read_WithNullYaml_ShouldThrowArgumentNullException()
    {
        // Arrange
        var reader = new YamlConfigReader();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => reader.Read(null!));
    }

    [Fact]
    public async Task ReadAsync_WithFileSizeExceeding1MB_ShouldThrowInvalidOperationException()
    {
        // Arrange - FR-002b-13: Enforce maximum file size of 1MB
        var tempFile = Path.GetTempFileName();
        var reader = new YamlConfigReader();

        try
        {
            // Create a file larger than 1MB
            var largeContent = "schema_version: \"1.0.0\"\n" + new string('#', 1_048_577);
            await File.WriteAllTextAsync(tempFile, largeContent).ConfigureAwait(true);

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
    public async Task ReadAsync_WithMultipleDocuments_ShouldThrowInvalidOperationException()
    {
        // Arrange - FR-002b-21: Reject YAML with multiple documents
        var tempFile = Path.GetTempFileName();
        var reader = new YamlConfigReader();
        var yamlWithMultipleDocs = @"
schema_version: ""1.0.0""
---
schema_version: ""2.0.0""
";

        try
        {
            await File.WriteAllTextAsync(tempFile, yamlWithMultipleDocs).ConfigureAwait(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await reader.ReadAsync(tempFile).ConfigureAwait(true)).ConfigureAwait(true);

            ex.Message.Should().Contain("multiple YAML documents");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ReadAsync_WithNestingDepthExceeding20_ShouldThrowInvalidOperationException()
    {
        // Arrange - FR-002b-14: Enforce maximum nesting depth of 20
        var tempFile = Path.GetTempFileName();
        var reader = new YamlConfigReader();

        try
        {
            // Create YAML with 21 levels of nesting (exceeds limit of 20)
            var deeplyNestedYaml = "schema_version: \"1.0.0\"\n";
            for (var i = 0; i < 21; i++)
            {
                deeplyNestedYaml += new string(' ', i * 2) + $"level{i}:\n";
            }

            deeplyNestedYaml += new string(' ', 21 * 2) + "value: \"too deep\"";

            await File.WriteAllTextAsync(tempFile, deeplyNestedYaml).ConfigureAwait(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await reader.ReadAsync(tempFile).ConfigureAwait(true)).ConfigureAwait(true);

            ex.Message.Should().Contain("nesting depth exceeds maximum of 20");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ReadAsync_WithKeyCountExceeding1000_ShouldThrowInvalidOperationException()
    {
        // Arrange - FR-002b-15: Enforce maximum key count of 1000
        var tempFile = Path.GetTempFileName();
        var reader = new YamlConfigReader();

        try
        {
            // Create YAML with 1001 keys (exceeds limit of 1000)
            var yamlBuilder = new System.Text.StringBuilder("schema_version: \"1.0.0\"\nproject:\n  name: \"test\"\n");
            for (var i = 0; i < 1001; i++)
            {
                yamlBuilder.AppendLine($"  key_{i}: value_{i}");
            }

            await File.WriteAllTextAsync(tempFile, yamlBuilder.ToString()).ConfigureAwait(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await reader.ReadAsync(tempFile).ConfigureAwait(true)).ConfigureAwait(true);

            ex.Message.Should().Contain("too many keys");
            ex.Message.Should().Contain("maximum: 1000");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ReadAsync_WithInvalidYamlSyntax_ShouldThrowWithLineNumber()
    {
        // Arrange - FR-002b-40: Enhanced error messages with line numbers
        var tempFile = Path.GetTempFileName();
        var reader = new YamlConfigReader();
        var invalidYaml = @"
schema_version: ""1.0.0""
project:
  name: ""test
  type: ""dotnet""
";

        try
        {
            await File.WriteAllTextAsync(tempFile, invalidYaml).ConfigureAwait(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await reader.ReadAsync(tempFile).ConfigureAwait(true)).ConfigureAwait(true);

            ex.Message.Should().Contain("line");
            ex.Message.Should().Contain("column");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Read_WithTabIndentation_ShouldThrowWithSuggestion()
    {
        // Arrange - FR-002b-41: Error suggestions for common YAML errors
        var reader = new YamlConfigReader();
        var yamlWithTabs = "schema_version: \"1.0.0\"\nproject:\n\tname: \"test\""; // Contains tab

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => reader.Read(yamlWithTabs));

        // Error message should contain suggestion about tabs
        ex.Message.Should().Contain("Suggestion");
    }

    [Fact]
    public void Read_WithDuplicateKeys_ShouldUseLastValue()
    {
        // Arrange - YamlDotNet default behavior: duplicate keys use last value
        var reader = new YamlConfigReader();
        var yamlWithDuplicates = @"
schema_version: ""1.0.0""
project:
  name: ""test1""
  name: ""test2""
";

        // Act
        var config = reader.Read(yamlWithDuplicates);

        // Assert - YamlDotNet uses the last value when keys are duplicated
        config.Project!.Name.Should().Be("test2", "duplicate keys should use the last value");
    }

    [Fact]
    public async Task ReadAsync_WithEmptyFile_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var reader = new YamlConfigReader();

        try
        {
            await File.WriteAllTextAsync(tempFile, string.Empty).ConfigureAwait(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await reader.ReadAsync(tempFile).ConfigureAwait(true)).ConfigureAwait(true);

            ex.Message.Should().Contain("Failed to deserialize");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ReadAsync_WithWhitespaceOnlyFile_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var reader = new YamlConfigReader();

        try
        {
            await File.WriteAllTextAsync(tempFile, "   \n\n  \t  \n").ConfigureAwait(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await reader.ReadAsync(tempFile).ConfigureAwait(true)).ConfigureAwait(true);

            ex.Message.Should().Contain("Failed to deserialize");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Read_WithComplexNestedStructure_ShouldDeserializeCorrectly()
    {
        // Arrange - Test complex valid YAML with multiple nested structures
        var complexYaml = @"
schema_version: ""1.0.0""
project:
  name: ""complex-project""
  type: ""dotnet""
  languages:
    - ""csharp""
    - ""fsharp""
mode:
  default: ""local-only""
  allow_burst: true
  airgapped_lock: false
model:
  provider: ""ollama""
  name: ""codellama:7b""
  timeout_seconds: 120
  parameters:
    temperature: 0.7
    max_tokens: 4096
    top_p: 0.95
commands:
  build: ""dotnet build""
  test: ""dotnet test""
  lint: ""dotnet format --verify-no-changes""
ignore:
  patterns:
    - ""*.log""
    - ""bin/""
    - ""obj/""
";
        var reader = new YamlConfigReader();

        // Act
        var config = reader.Read(complexYaml);

        // Assert
        config.Should().NotBeNull();
        config.SchemaVersion.Should().Be("1.0.0");
        config.Project!.Name.Should().Be("complex-project");
        config.Project.Languages.Should().HaveCount(2);
        config.Mode!.Default.Should().Be("local-only");
        config.Model!.Provider.Should().Be("ollama");
        config.Model.Parameters.Temperature.Should().Be(0.7);
        config.Commands!.Build.Should().Be("dotnet build");
        config.Ignore!.Patterns.Should().HaveCount(3);
    }
}

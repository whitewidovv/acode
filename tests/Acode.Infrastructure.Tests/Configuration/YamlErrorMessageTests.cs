using Acode.Infrastructure.Configuration;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.Configuration;

/// <summary>
/// Tests for enhanced YAML error messages (FR-002b-40, FR-002b-41).
/// </summary>
public class YamlErrorMessageTests
{
    [Fact]
    public async Task ReadAsync_WithInvalidYaml_ShouldIncludeLineNumber()
    {
        // Arrange
        var reader = new YamlConfigReader();
        var tempFile = Path.GetTempFileName();

        // Invalid YAML: mismatched indentation
        var yaml = @"schema_version: ""1.0.0""
project:
  name: test
   description: invalid  # Extra space - bad indentation
";

        await File.WriteAllTextAsync(tempFile, yaml).ConfigureAwait(true);

        try
        {
            // Act & Assert
            var act = async () => await reader.ReadAsync(tempFile).ConfigureAwait(true);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*line*").ConfigureAwait(true);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ReadAsync_WithInvalidYaml_ShouldSuggestFix()
    {
        // Arrange
        var reader = new YamlConfigReader();
        var tempFile = Path.GetTempFileName();

        // Invalid YAML: unclosed quote
        var yaml = @"schema_version: ""1.0.0
project:
  name: test
";

        await File.WriteAllTextAsync(tempFile, yaml).ConfigureAwait(true);

        try
        {
            // Act & Assert
            var act = async () => await reader.ReadAsync(tempFile).ConfigureAwait(true);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*suggestion*").ConfigureAwait(true);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ReadAsync_WithInvalidYaml_ShouldShowContext()
    {
        // Arrange
        var reader = new YamlConfigReader();
        var tempFile = Path.GetTempFileName();

        // Invalid YAML: unbalanced braces in mapping
        var yaml = @"schema_version: ""1.0.0""
project: {
  name: test
";

        await File.WriteAllTextAsync(tempFile, yaml).ConfigureAwait(true);

        try
        {
            // Act & Assert
            var act = async () => await reader.ReadAsync(tempFile).ConfigureAwait(true);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*line*").ConfigureAwait(true);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ReadAsync_WithSyntaxError_ShouldProvideDetailedMessage()
    {
        // Arrange
        var reader = new YamlConfigReader();
        var tempFile = Path.GetTempFileName();

        // Invalid YAML: tabs instead of spaces
        var yaml = "schema_version: \"1.0.0\"\nproject:\n\tname: test";

        await File.WriteAllTextAsync(tempFile, yaml).ConfigureAwait(true);

        try
        {
            // Act & Assert
            var act = async () => await reader.ReadAsync(tempFile).ConfigureAwait(true);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*line*").ConfigureAwait(true);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Read_WithInvalidYaml_ShouldIncludeLineNumber()
    {
        // Arrange
        var reader = new YamlConfigReader();

        // Invalid YAML: unclosed bracket
        var yaml = @"schema_version: ""1.0.0""
project:
  languages: [""csharp""
";

        // Act & Assert
        var act = () => reader.Read(yaml);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*line*");
    }
}

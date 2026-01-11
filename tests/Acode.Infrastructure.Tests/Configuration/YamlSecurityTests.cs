using Acode.Infrastructure.Configuration;
using FluentAssertions;

namespace Acode.Infrastructure.Tests.Configuration;

/// <summary>
/// Tests for YAML security features (FR-002b-04, 10-18, 21).
/// </summary>
public class YamlSecurityTests
{
    [Fact]
    public async Task ReadAsync_WithFileLargerThan1MB_ShouldThrowException()
    {
        // Arrange
        var reader = new YamlConfigReader();
        var tempFile = Path.GetTempFileName();

        // Create a file larger than 1MB
        await File.WriteAllTextAsync(tempFile, new string('a', (1024 * 1024) + 1)).ConfigureAwait(true);

        try
        {
            // Act & Assert
            var act = async () => await reader.ReadAsync(tempFile).ConfigureAwait(true);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*exceeds maximum size*").ConfigureAwait(true);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ReadAsync_WithExcessiveNesting_ShouldThrowException()
    {
        // Arrange
        var reader = new YamlConfigReader();
        var tempFile = Path.GetTempFileName();

        // Create YAML with 25 levels of nesting (exceeds limit of 20)
        var yaml = "schema_version: \"1.0.0\"\n";
        for (int i = 0; i < 25; i++)
        {
            yaml += new string(' ', i * 2) + $"level{i}:\n";
        }

        yaml += new string(' ', 50) + "value: test";

        await File.WriteAllTextAsync(tempFile, yaml).ConfigureAwait(true);

        try
        {
            // Act & Assert
            var act = async () => await reader.ReadAsync(tempFile).ConfigureAwait(true);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*nesting depth*").ConfigureAwait(true);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ReadAsync_WithTooManyKeys_ShouldThrowException()
    {
        // Arrange
        var reader = new YamlConfigReader();
        var tempFile = Path.GetTempFileName();

        // Create YAML with 1001 keys (exceeds limit of 1000)
        var yaml = "schema_version: \"1.0.0\"\nkeys:\n";
        for (int i = 0; i < 1001; i++)
        {
            yaml += $"  key{i}: value{i}\n";
        }

        await File.WriteAllTextAsync(tempFile, yaml).ConfigureAwait(true);

        try
        {
            // Act & Assert
            var act = async () => await reader.ReadAsync(tempFile).ConfigureAwait(true);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*too many keys*").ConfigureAwait(true);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ReadAsync_WithMultipleDocuments_ShouldThrowException()
    {
        // Arrange
        var reader = new YamlConfigReader();
        var tempFile = Path.GetTempFileName();

        var yaml = @"schema_version: ""1.0.0""
---
schema_version: ""1.0.0""
";

        await File.WriteAllTextAsync(tempFile, yaml).ConfigureAwait(true);

        try
        {
            // Act & Assert
            var act = async () => await reader.ReadAsync(tempFile).ConfigureAwait(true);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*multiple*YAML*documents*").ConfigureAwait(true);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ReadAsync_WithValidFile_ShouldSucceed()
    {
        // Arrange
        var reader = new YamlConfigReader();
        var tempFile = Path.GetTempFileName();

        var yaml = @"schema_version: ""1.0.0""
project:
  name: test
";

        await File.WriteAllTextAsync(tempFile, yaml).ConfigureAwait(true);

        try
        {
            // Act
            var config = await reader.ReadAsync(tempFile).ConfigureAwait(true);

            // Assert
            config.Should().NotBeNull();
            config.SchemaVersion.Should().Be("1.0.0");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}

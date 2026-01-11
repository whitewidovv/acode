using System.Text.Json;
using Acode.Domain.Validation;
using Acode.Infrastructure.Network;
using FluentAssertions;

namespace Acode.Infrastructure.Tests.Network;

/// <summary>
/// Tests for DenylistProvider implementation.
/// Verifies loadable denylist logic per Task 001.b FR-001b-36, FR-001b-37.
/// </summary>
public class DenylistProviderTests
{
    [Fact]
    public void LoadFromFile_WithValidJsonFile_ShouldLoadPatterns()
    {
        // Arrange
        var provider = new DenylistProvider();
        var testFilePath = CreateTestDenylistFile(new[]
        {
            new { pattern = "api.openai.com", type = "exact", description = "OpenAI API" },
            new { pattern = "*.anthropic.com", type = "wildcard", description = "Anthropic" }
        });

        // Act
        var patterns = provider.LoadFromFile(testFilePath);

        // Assert
        patterns.Should().HaveCount(2);
        patterns[0].Pattern.Should().Be("api.openai.com");
        patterns[0].Type.Should().Be(PatternType.Exact);
        patterns[1].Pattern.Should().Be("*.anthropic.com");
        patterns[1].Type.Should().Be(PatternType.Wildcard);

        // Cleanup
        File.Delete(testFilePath);
    }

    [Fact]
    public void LoadFromFile_WithRegexPattern_ShouldCompileRegex()
    {
        // Arrange
        var provider = new DenylistProvider();
        var testFilePath = CreateTestDenylistFile(new[]
        {
            new { pattern = @".*\.openai\.azure\.com", type = "regex", description = "Azure OpenAI" }
        });

        // Act
        var patterns = provider.LoadFromFile(testFilePath);

        // Assert
        patterns.Should().HaveCount(1);
        patterns[0].Type.Should().Be(PatternType.Regex);
        var uri = new Uri("https://mycompany.openai.azure.com/openai/deployments/gpt-4");
        patterns[0].Matches(uri).Should().BeTrue();

        // Cleanup
        File.Delete(testFilePath);
    }

    [Fact]
    public void LoadFromFile_WithInvalidPatternType_ShouldSkipPattern()
    {
        // Arrange
        var provider = new DenylistProvider();
        var testFilePath = CreateTestDenylistFile(new[]
        {
            new { pattern = "api.openai.com", type = "exact", description = "OpenAI API" },
            new { pattern = "bad.example.com", type = "invalid-type", description = "Should be skipped" }
        });

        // Act
        var patterns = provider.LoadFromFile(testFilePath);

        // Assert
        patterns.Should().HaveCount(1); // Only valid pattern loaded
        patterns[0].Pattern.Should().Be("api.openai.com");

        // Cleanup
        File.Delete(testFilePath);
    }

    [Fact]
    public void LoadFromFile_WithFileNotFound_ShouldReturnBuiltInDenylist()
    {
        // Arrange
        var provider = new DenylistProvider();
        var nonExistentPath = "nonexistent-denylist.json";

        // Act
        var patterns = provider.LoadFromFile(nonExistentPath);

        // Assert
        patterns.Should().NotBeEmpty();
        patterns.Should().Contain(p => p.Pattern == "api.openai.com");
        patterns.Should().Contain(p => p.Pattern == "api.anthropic.com");
    }

    [Fact]
    public void LoadFromFile_WithInvalidJson_ShouldReturnBuiltInDenylist()
    {
        // Arrange
        var provider = new DenylistProvider();
        var testFilePath = Path.GetTempFileName();
        File.WriteAllText(testFilePath, "{invalid json content}");

        // Act
        var patterns = provider.LoadFromFile(testFilePath);

        // Assert
        patterns.Should().NotBeEmpty();
        patterns.Should().Contain(p => p.Pattern == "api.openai.com");

        // Cleanup
        File.Delete(testFilePath);
    }

    [Fact]
    public void GetBuiltInDenylist_ShouldContainAllMajorProviders()
    {
        // Arrange
        var provider = new DenylistProvider();

        // Act
        var patterns = provider.GetBuiltInDenylist();

        // Assert
        patterns.Should().NotBeEmpty();
        patterns.Should().Contain(p => p.Pattern == "api.openai.com", "OpenAI API should be denied");
        patterns.Should().Contain(p => p.Pattern == "*.openai.com", "OpenAI subdomains should be denied");
        patterns.Should().Contain(p => p.Pattern == "api.anthropic.com", "Anthropic API should be denied");
        patterns.Should().Contain(p => p.Pattern == "*.anthropic.com", "Anthropic subdomains should be denied");
        patterns.Should().Contain(p => p.Type == PatternType.Regex && p.Pattern.Contains("azure", StringComparison.Ordinal), "Azure OpenAI should be denied");
        patterns.Should().Contain(p => p.Pattern == "generativelanguage.googleapis.com", "Google AI should be denied");
        patterns.Should().Contain(p => p.Type == PatternType.Regex && p.Pattern.Contains("bedrock", StringComparison.Ordinal), "AWS Bedrock should be denied");
        patterns.Should().Contain(p => p.Pattern == "api.cohere.ai", "Cohere should be denied");
        patterns.Should().Contain(p => p.Pattern == "api-inference.huggingface.co", "Hugging Face should be denied");
        patterns.Should().Contain(p => p.Pattern == "api.together.xyz", "Together.ai should be denied");
        patterns.Should().Contain(p => p.Pattern == "api.replicate.com", "Replicate should be denied");
    }

    [Fact]
    public void LoadFromFile_WithRealDenylistFile_ShouldLoad()
    {
        // Arrange
        var provider = new DenylistProvider();
        var denylistPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..",
            "..",
            "..",
            "..",
            "..",
            "data",
            "denylist.json");

        // Skip test if file doesn't exist (e.g., in CI without data folder)
        if (!File.Exists(denylistPath))
        {
            return;
        }

        // Act
        var patterns = provider.LoadFromFile(denylistPath);

        // Assert
        patterns.Should().NotBeEmpty();
        patterns.Should().HaveCountGreaterOrEqualTo(11); // At least 11 patterns from spec
    }

    private static string CreateTestDenylistFile(object[] patterns)
    {
        var tempFile = Path.GetTempFileName();
        var denylistData = new
        {
            version = "1.0.0",
            updated = "2026-01-11",
            patterns
        };
        var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(denylistData, jsonOptions);
        File.WriteAllText(tempFile, json);
        return tempFile;
    }
}

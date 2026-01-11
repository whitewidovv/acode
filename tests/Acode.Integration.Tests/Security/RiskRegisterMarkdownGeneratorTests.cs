namespace Acode.Integration.Tests.Security;

using Acode.Application.Security;
using Acode.Infrastructure.Security;
using FluentAssertions;

/// <summary>
/// Integration tests for RiskRegisterMarkdownGenerator.
/// </summary>
public class RiskRegisterMarkdownGeneratorTests
{
    [Fact]
    public async Task Should_Generate_Complete_Markdown_Documentation()
    {
        // Arrange
        var yamlPath = GetRiskRegisterPath();
        var repository = new YamlRiskRegisterRepository(yamlPath);
        var generator = new RiskRegisterMarkdownGenerator(repository);

        // Act
        var markdown = await generator.GenerateAsync();

        // Assert
        markdown.Should().NotBeNullOrWhiteSpace();
        markdown.Should().Contain("# Risk Register");
        markdown.Should().Contain("## Risks by STRIDE Category");
        markdown.Should().Contain("### Spoofing");
        markdown.Should().Contain("### Tampering");
        markdown.Should().Contain("### Repudiation");
        markdown.Should().Contain("### Information Disclosure");
        markdown.Should().Contain("### Denial of Service");
        markdown.Should().Contain("### Elevation of Privilege");
        markdown.Should().Contain("## Mitigations");
        markdown.Should().Contain("RISK-");  // Should have risk IDs
        markdown.Should().Contain("MIT-");   // Should have mitigation IDs

        // Write to file as side effect (for Gap #19 requirement)
        var outputPath = Path.Combine(
            GetRepositoryRoot(),
            "docs",
            "security",
            "risk-register.md");

        await File.WriteAllTextAsync(outputPath, markdown);
    }

    [Fact]
    public async Task Should_Include_All_STRIDE_Categories()
    {
        // Arrange
        var yamlPath = GetRiskRegisterPath();
        var repository = new YamlRiskRegisterRepository(yamlPath);
        var generator = new RiskRegisterMarkdownGenerator(repository);

        // Act
        var markdown = await generator.GenerateAsync();

        // Assert - Each STRIDE category should have a section
        markdown.Should().MatchRegex(@"###\s+Spoofing");
        markdown.Should().MatchRegex(@"###\s+Tampering");
        markdown.Should().MatchRegex(@"###\s+Repudiation");
        markdown.Should().MatchRegex(@"###\s+Information Disclosure");
        markdown.Should().MatchRegex(@"###\s+Denial of Service");
        markdown.Should().MatchRegex(@"###\s+Elevation of Privilege");
    }

    [Fact]
    public async Task Should_Include_Risk_Details_With_DREAD_Scores()
    {
        // Arrange
        var yamlPath = GetRiskRegisterPath();
        var repository = new YamlRiskRegisterRepository(yamlPath);
        var generator = new RiskRegisterMarkdownGenerator(repository);

        // Act
        var markdown = await generator.GenerateAsync();

        // Assert
        markdown.Should().Contain("**DREAD Score**:");
        markdown.Should().Contain("- Damage:");
        markdown.Should().Contain("- Reproducibility:");
        markdown.Should().Contain("- Exploitability:");
        markdown.Should().Contain("- Affected Users:");
        markdown.Should().Contain("- Discoverability:");
        markdown.Should().Contain("**Average**:");
    }

    [Fact]
    public async Task Should_Include_Mitigation_Details()
    {
        // Arrange
        var yamlPath = GetRiskRegisterPath();
        var repository = new YamlRiskRegisterRepository(yamlPath);
        var generator = new RiskRegisterMarkdownGenerator(repository);

        // Act
        var markdown = await generator.GenerateAsync();

        // Assert
        markdown.Should().Contain("### Detailed Mitigation Information");
        markdown.Should().Contain("**Status**:");
        markdown.Should().Contain("**Implementation**:");
    }

    private static string GetRiskRegisterPath()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var repoRoot = FindRepositoryRoot(currentDir);
        if (repoRoot == null)
        {
            throw new InvalidOperationException($"Could not find repository root from {currentDir}");
        }

        var yamlPath = Path.Combine(repoRoot, "docs", "security", "risk-register.yaml");
        if (!File.Exists(yamlPath))
        {
            throw new FileNotFoundException($"Risk register file not found: {yamlPath}");
        }

        return yamlPath;
    }

    private static string GetRepositoryRoot()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var repoRoot = FindRepositoryRoot(currentDir);
        if (repoRoot == null)
        {
            throw new InvalidOperationException($"Could not find repository root from {currentDir}");
        }

        return repoRoot;
    }

    private static string? FindRepositoryRoot(string startPath)
    {
        var dir = new DirectoryInfo(startPath);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, ".git")) ||
                File.Exists(Path.Combine(dir.FullName, "Acode.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return null;
    }
}

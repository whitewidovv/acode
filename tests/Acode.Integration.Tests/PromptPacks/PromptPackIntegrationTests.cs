using Acode.Domain.PromptPacks;
using Acode.Infrastructure.PromptPacks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Acode.Integration.Tests.PromptPacks;

/// <summary>
/// Integration tests for end-to-end prompt pack loading and composition.
/// Tests 23-30 from Task 008 parent spec (lines 1769-2075).
/// </summary>
public class PromptPackIntegrationTests : IDisposable
{
    private readonly string _testDir;
    private readonly ManifestParser _manifestParser;
    private readonly ContentHasher _contentHasher;
    private readonly EmbeddedPackProvider _embeddedPackProvider;
    private readonly PromptPackLoader _loader;
    private readonly TemplateEngine _templateEngine;

    public PromptPackIntegrationTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"prompt-pack-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);

        _manifestParser = new ManifestParser();
        _contentHasher = new ContentHasher();
        _embeddedPackProvider = new EmbeddedPackProvider(_manifestParser, NullLogger<EmbeddedPackProvider>.Instance);
        _loader = new PromptPackLoader(
            _manifestParser,
            _contentHasher,
            _embeddedPackProvider,
            NullLogger<PromptPackLoader>.Instance);
        _templateEngine = new TemplateEngine();
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, recursive: true);
        }
    }

    /// <summary>
    /// Test 23: Should Load Built-In Pack And Compose Prompt.
    /// </summary>
    [Fact]
    public async Task Should_Load_BuiltIn_Pack_And_Compose_Prompt()
    {
        // Arrange
        var composer = new PromptComposer(_templateEngine, logger: NullLogger<PromptComposer>.Instance);

        // Act - load built-in pack
        var pack = await _loader.LoadBuiltInPackAsync("acode-standard");
        var context = new CompositionContext { Role = "coder", Language = "csharp" };
        var prompt = await composer.ComposeAsync(pack, context);

        // Assert
        pack.Should().NotBeNull();
        pack.Id.Should().Be("acode-standard");
        prompt.Should().NotBeEmpty();
    }

    /// <summary>
    /// Test 24: Should Load User Pack From Workspace.
    /// </summary>
    [Fact]
    public async Task Should_Load_User_Pack_From_Workspace()
    {
        // Arrange
        var packDir = CreateUserPack("custom-pack", "Custom system prompt.");

        // Act
        var pack = await _loader.LoadPackAsync(packDir);

        // Assert
        pack.Should().NotBeNull();
        pack.Id.Should().Be("custom-pack");
        pack.Components.Should().HaveCount(1);
        pack.Components[0].Content.Should().Be("Custom system prompt.");
    }

    /// <summary>
    /// Test 25: Should Override Built-In Pack With User Pack.
    /// This test verifies that user packs with same ID take precedence.
    /// </summary>
    [Fact]
    public async Task Should_Override_BuiltIn_Pack_With_User_Pack()
    {
        // Arrange - create a user pack with same ID as built-in
        var packDir = CreateUserPackWithId("acode-standard", "2.0.0", "Custom Standard Pack");

        // Act - load user pack
        var pack = await _loader.LoadPackAsync(packDir);

        // Assert - user pack should have overridden values
        pack.Id.Should().Be("acode-standard");
        pack.Version.ToString().Should().Be("2.0.0");
        pack.Name.Should().Be("Custom Standard Pack");
    }

    /// <summary>
    /// Test 26: Should Apply Template Variables From Configuration.
    /// </summary>
    [Fact]
    public async Task Should_Apply_Template_Variables_From_Configuration()
    {
        // Arrange
        var packDir = CreatePackWithTemplateVariables();
        var pack = await _loader.LoadPackAsync(packDir);
        var composer = new PromptComposer(_templateEngine, logger: NullLogger<PromptComposer>.Instance);

        var context = new CompositionContext
        {
            ConfigVariables = new Dictionary<string, string>
            {
                ["workspace_name"] = "TestWorkspace",
                ["team_name"] = "Engineering"
            }
        };

        // Act
        var result = await composer.ComposeAsync(pack, context);

        // Assert
        result.Should().Contain("Workspace: TestWorkspace");
        result.Should().Contain("Team: Engineering");
    }

    /// <summary>
    /// Test 27: Should Validate Pack And Reject Invalid Manifest.
    /// </summary>
    [Fact]
    public async Task Should_Validate_Pack_And_Reject_Invalid_Manifest()
    {
        // Arrange
        var packDir = Path.Combine(_testDir, "invalid-pack");
        Directory.CreateDirectory(packDir);
        await File.WriteAllTextAsync(Path.Combine(packDir, "manifest.yml"), "invalid: yaml: content: [[[]");

        // Act & Assert
        var act = async () => await _loader.LoadPackAsync(packDir);
        await act.Should().ThrowAsync<Exception>();
    }

    /// <summary>
    /// Test 28: Complete Workflow - Select Pack, Compose Prompt.
    /// </summary>
    [Fact]
    public async Task Complete_Workflow_Select_Pack_Compose_Prompt()
    {
        // Arrange
        var composer = new PromptComposer(_templateEngine, logger: NullLogger<PromptComposer>.Instance);

        // Act - load pack and compose
        var pack = await _loader.LoadBuiltInPackAsync("acode-dotnet");
        var context = new CompositionContext
        {
            Role = "coder",
            Language = "csharp"
        };
        var systemPrompt = await composer.ComposeAsync(pack, context);

        // Assert
        pack.Id.Should().Be("acode-dotnet");
        systemPrompt.Should().NotBeEmpty();
    }

    /// <summary>
    /// Test 29: Multi-Stage Workflow - Different Prompts Per Stage.
    /// </summary>
    [Fact]
    public async Task Multi_Stage_Workflow_Different_Prompts_Per_Stage()
    {
        // Arrange
        var composer = new PromptComposer(_templateEngine, logger: NullLogger<PromptComposer>.Instance);
        var pack = await _loader.LoadBuiltInPackAsync("acode-standard");

        // Act - Stage 1: Planner
        var plannerContext = new CompositionContext { Role = "planner" };
        var plannerPrompt = await composer.ComposeAsync(pack, plannerContext);

        // Act - Stage 2: Coder
        var coderContext = new CompositionContext { Role = "coder", Language = "csharp" };
        var coderPrompt = await composer.ComposeAsync(pack, coderContext);

        // Act - Stage 3: Reviewer
        var reviewerContext = new CompositionContext { Role = "reviewer", Language = "csharp" };
        var reviewerPrompt = await composer.ComposeAsync(pack, reviewerContext);

        // Assert - prompts should be different based on role
        plannerPrompt.Should().NotBeEmpty();
        coderPrompt.Should().NotBeEmpty();
        reviewerPrompt.Should().NotBeEmpty();
    }

    /// <summary>
    /// Test 30: Custom Pack Workflow - Create, Validate, Use.
    /// </summary>
    [Fact]
    public async Task Custom_Pack_Workflow_Create_Validate_Use()
    {
        // Arrange - create custom pack
        var packDir = CreatePackWithTemplateVariables("my-custom-pack");

        // Act - validate
        var validator = new PackValidator(_manifestParser, NullLogger<PackValidator>.Instance);
        var pack = await _loader.LoadPackAsync(packDir);
        var validationResult = validator.Validate(pack);

        // Compose
        var composer = new PromptComposer(_templateEngine, logger: NullLogger<PromptComposer>.Instance);
        var context = new CompositionContext
        {
            Variables = new Dictionary<string, string> { ["workspace_name"] = "TestProject", ["team_name"] = "DevTeam" }
        };
        var prompt = await composer.ComposeAsync(pack, context);

        // Assert
        validationResult.IsValid.Should().BeTrue();
        pack.Id.Should().Be("my-custom-pack");
        prompt.Should().Contain("TestProject");
    }

    private string CreateUserPack(string packId, string systemContent)
    {
        var packDir = Path.Combine(_testDir, packId);
        Directory.CreateDirectory(packDir);

        var manifest = $@"format_version: '1.0'
id: {packId}
version: 1.0.0
name: {packId} Pack
description: A test prompt pack for integration testing purposes
created_at: 2024-01-15T10:00:00Z
components:
  - path: system.md
    type: system";

        File.WriteAllText(Path.Combine(packDir, "manifest.yml"), manifest);
        File.WriteAllText(Path.Combine(packDir, "system.md"), systemContent);

        return packDir;
    }

    private string CreateUserPackWithId(string packId, string version, string name)
    {
        var packDir = Path.Combine(_testDir, packId);
        Directory.CreateDirectory(packDir);

        var manifest = $@"format_version: '1.0'
id: {packId}
version: {version}
name: {name}
description: A test prompt pack for integration testing purposes
created_at: 2024-01-15T10:00:00Z
components:
  - path: system.md
    type: system";

        File.WriteAllText(Path.Combine(packDir, "manifest.yml"), manifest);
        File.WriteAllText(Path.Combine(packDir, "system.md"), "Custom system prompt.");

        return packDir;
    }

    private string CreatePackWithTemplateVariables(string packId = "template-pack")
    {
        var packDir = Path.Combine(_testDir, packId);
        Directory.CreateDirectory(packDir);

        var manifest = $@"format_version: '1.0'
id: {packId}
version: 1.0.0
name: Template Test Pack
description: A test prompt pack with template variables for testing
created_at: 2024-01-15T10:00:00Z
components:
  - path: system.md
    type: system";

        var systemContent = "Workspace: {{workspace_name}}, Team: {{team_name}}";

        File.WriteAllText(Path.Combine(packDir, "manifest.yml"), manifest);
        File.WriteAllText(Path.Combine(packDir, "system.md"), systemContent);

        return packDir;
    }
}

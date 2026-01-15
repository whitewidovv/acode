using Acode.Application.PromptPacks;
using Acode.Cli.Commands;
using Acode.Domain.PromptPacks;
using Acode.Domain.PromptPacks.Exceptions;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Acode.Cli.Tests.Commands;

/// <summary>
/// Tests for prompts CLI commands (list, show, validate, reload).
/// These tests verify AC-065 through AC-073 from Task 008b.
/// </summary>
public class PromptsCommandTests
{
    private readonly IPromptPackRegistry _mockRegistry;
    private readonly IPromptPackLoader _mockLoader;
    private readonly IPackValidator _mockValidator;

    public PromptsCommandTests()
    {
        _mockRegistry = Substitute.For<IPromptPackRegistry>();
        _mockLoader = Substitute.For<IPromptPackLoader>();
        _mockValidator = Substitute.For<IPackValidator>();
    }

    // AC-065: list command works
    [Fact]
    public async Task ExecuteAsync_List_ReturnsSuccess()
    {
        // Arrange
        var command = new PromptsCommand(_mockRegistry, _mockLoader, _mockValidator);
        var packs = new List<PromptPackInfo>
        {
            new("acode-standard", new PackVersion(1, 0, 0), "Acode Standard", PackSource.BuiltIn, true, "/packs/acode-standard"),
            new("acode-dotnet", new PackVersion(1, 0, 0), "Acode .NET", PackSource.BuiltIn, false, "/packs/acode-dotnet"),
        };

        _mockRegistry.ListPacks().Returns(packs);
        _mockRegistry.GetActivePackId().Returns("acode-standard");

        var output = new StringWriter();
        var context = CreateContext(new[] { "list" }, output);

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.Success);
    }

    // AC-066: list shows id, version, source
    [Fact]
    public async Task ExecuteAsync_List_ShowsIdVersionSource()
    {
        // Arrange
        var command = new PromptsCommand(_mockRegistry, _mockLoader, _mockValidator);
        var packs = new List<PromptPackInfo>
        {
            new("acode-standard", new PackVersion(1, 0, 0), "Acode Standard", PackSource.BuiltIn, true, "/packs/acode-standard"),
            new("custom-pack", new PackVersion(2, 1, 0), "Custom Pack", PackSource.User, false, ".acode/prompts/custom-pack"),
        };

        _mockRegistry.ListPacks().Returns(packs);
        _mockRegistry.GetActivePackId().Returns("acode-standard");

        var output = new StringWriter();
        var context = CreateContext(new[] { "list" }, output);

        // Act
        await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        var outputText = output.ToString();
        outputText.Should().Contain("acode-standard");
        outputText.Should().Contain("1.0.0");
        outputText.Should().Contain("built-in");
        outputText.Should().Contain("custom-pack");
        outputText.Should().Contain("2.1.0");
        outputText.Should().Contain("user");
    }

    // AC-067: list shows active flag
    [Fact]
    public async Task ExecuteAsync_List_ShowsActiveFlag()
    {
        // Arrange
        var command = new PromptsCommand(_mockRegistry, _mockLoader, _mockValidator);
        var packs = new List<PromptPackInfo>
        {
            new("acode-standard", new PackVersion(1, 0, 0), "Acode Standard", PackSource.BuiltIn, false, "/packs/acode-standard"),
            new("acode-dotnet", new PackVersion(1, 0, 0), "Acode .NET", PackSource.BuiltIn, true, "/packs/acode-dotnet"),
        };

        _mockRegistry.ListPacks().Returns(packs);
        _mockRegistry.GetActivePackId().Returns("acode-dotnet");

        var output = new StringWriter();
        var context = CreateContext(new[] { "list" }, output);

        // Act
        await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        var outputText = output.ToString();

        // Active pack should have indicator
        outputText.Should().Contain("*");
    }

    // AC-068: show command works
    [Fact]
    public async Task ExecuteAsync_Show_ReturnsSuccess()
    {
        // Arrange
        var command = new PromptsCommand(_mockRegistry, _mockLoader, _mockValidator);
        var pack = CreateTestPack("acode-standard");

        _mockRegistry.TryGetPackAsync("acode-standard", Arg.Any<CancellationToken>()).Returns(pack);

        var output = new StringWriter();
        var context = CreateContext(new[] { "show", "acode-standard" }, output);

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.Success);
    }

    // AC-069: show includes components
    [Fact]
    public async Task ExecuteAsync_Show_IncludesComponents()
    {
        // Arrange
        var command = new PromptsCommand(_mockRegistry, _mockLoader, _mockValidator);
        var pack = CreateTestPack("acode-standard");

        _mockRegistry.TryGetPackAsync("acode-standard", Arg.Any<CancellationToken>()).Returns(pack);

        var output = new StringWriter();
        var context = CreateContext(new[] { "show", "acode-standard" }, output);

        // Act
        await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        var outputText = output.ToString();
        outputText.Should().Contain("system.md");
        outputText.Should().Contain("roles/coder.md");
    }

    [Fact]
    public async Task ExecuteAsync_Show_WithoutPackId_ReturnsError()
    {
        // Arrange
        var command = new PromptsCommand(_mockRegistry, _mockLoader, _mockValidator);

        var output = new StringWriter();
        var context = CreateContext(new[] { "show" }, output);

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.InvalidArguments);
        output.ToString().Should().Contain("pack ID");
    }

    [Fact]
    public async Task ExecuteAsync_Show_WithUnknownPack_ReturnsError()
    {
        // Arrange
        var command = new PromptsCommand(_mockRegistry, _mockLoader, _mockValidator);

        _mockRegistry.TryGetPackAsync("unknown-pack", Arg.Any<CancellationToken>()).Returns((PromptPack?)null);

        var output = new StringWriter();
        var context = CreateContext(new[] { "show", "unknown-pack" }, output);

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.GeneralError);
        output.ToString().Should().Contain("not found");
    }

    // AC-070: validate command works
    [Fact]
    public async Task ExecuteAsync_Validate_WithValidPack_ReturnsSuccess()
    {
        // Arrange
        var command = new PromptsCommand(_mockRegistry, _mockLoader, _mockValidator);
        var pack = CreateTestPack("test-pack");
        var validationResult = ValidationResult.Success();

        _mockLoader.LoadPackAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(pack);
        _mockValidator.Validate(pack).Returns(validationResult);

        var output = new StringWriter();
        var context = CreateContext(new[] { "validate", "/path/to/pack" }, output);

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.Success);
    }

    // AC-071: validate outputs errors
    [Fact]
    public async Task ExecuteAsync_Validate_WithInvalidPack_OutputsErrors()
    {
        // Arrange
        var command = new PromptsCommand(_mockRegistry, _mockLoader, _mockValidator);
        var pack = CreateTestPack("test-pack");
        var errors = new List<ValidationError>
        {
            new() { Code = "MISSING_FIELD", Message = "Missing required field 'name'", FilePath = "manifest.yml" },
            new() { Code = "INVALID_VERSION", Message = "Version must follow SemVer", FilePath = "manifest.yml" },
        };
        var validationResult = ValidationResult.Failure(errors);

        _mockLoader.LoadPackAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(pack);
        _mockValidator.Validate(pack).Returns(validationResult);

        var output = new StringWriter();
        var context = CreateContext(new[] { "validate", "/path/to/pack" }, output);

        // Act
        await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        var outputText = output.ToString();
        outputText.Should().Contain("MISSING_FIELD");
        outputText.Should().Contain("INVALID_VERSION");
    }

    // AC-072: validate exit 0/1 correct
    [Fact]
    public async Task ExecuteAsync_Validate_WithInvalidPack_ReturnsError()
    {
        // Arrange
        var command = new PromptsCommand(_mockRegistry, _mockLoader, _mockValidator);
        var pack = CreateTestPack("test-pack");
        var errors = new List<ValidationError>
        {
            new() { Code = "MISSING_FIELD", Message = "Missing required field", FilePath = "manifest.yml" },
        };
        var validationResult = ValidationResult.Failure(errors);

        _mockLoader.LoadPackAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(pack);
        _mockValidator.Validate(pack).Returns(validationResult);

        var output = new StringWriter();
        var context = CreateContext(new[] { "validate", "/path/to/pack" }, output);

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.GeneralError);
    }

    [Fact]
    public async Task ExecuteAsync_Validate_WithLoadError_ReturnsError()
    {
        // Arrange
        var command = new PromptsCommand(_mockRegistry, _mockLoader, _mockValidator);

        _mockLoader.LoadPackAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new PackLoadException("Failed to load pack", "test-pack"));

        var output = new StringWriter();
        var context = CreateContext(new[] { "validate", "/path/to/pack" }, output);

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.GeneralError);
        output.ToString().Should().Contain("Failed to load");
    }

    // AC-073: reload command works
    [Fact]
    public async Task ExecuteAsync_Reload_RefreshesRegistry()
    {
        // Arrange
        var command = new PromptsCommand(_mockRegistry, _mockLoader, _mockValidator);

        var output = new StringWriter();
        var context = CreateContext(new[] { "reload" }, output);

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.Success);
        await _mockRegistry.Received(1).RefreshAsync(Arg.Any<CancellationToken>()).ConfigureAwait(true);
        output.ToString().Should().Contain("reload");
    }

    [Fact]
    public async Task ExecuteAsync_WithNoSubcommand_ReturnsError()
    {
        // Arrange
        var command = new PromptsCommand(_mockRegistry, _mockLoader, _mockValidator);

        var output = new StringWriter();
        var context = CreateContext(Array.Empty<string>(), output);

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.InvalidArguments);
    }

    [Fact]
    public async Task ExecuteAsync_WithUnknownSubcommand_ReturnsError()
    {
        // Arrange
        var command = new PromptsCommand(_mockRegistry, _mockLoader, _mockValidator);

        var output = new StringWriter();
        var context = CreateContext(new[] { "unknown" }, output);

        // Act
        var exitCode = await command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        exitCode.Should().Be(ExitCode.InvalidArguments);
        output.ToString().Should().Contain("Unknown subcommand");
    }

    [Fact]
    public void GetHelp_ReturnsUsageInformation()
    {
        // Arrange
        var command = new PromptsCommand(_mockRegistry, _mockLoader, _mockValidator);

        // Act
        var help = command.GetHelp();

        // Assert
        help.Should().Contain("list");
        help.Should().Contain("show");
        help.Should().Contain("validate");
        help.Should().Contain("reload");
    }

    [Fact]
    public void Name_ReturnsPrompts()
    {
        // Arrange
        var command = new PromptsCommand(_mockRegistry, _mockLoader, _mockValidator);

        // Act & Assert
        command.Name.Should().Be("prompts");
    }

    [Fact]
    public void Description_ReturnsDescription()
    {
        // Arrange
        var command = new PromptsCommand(_mockRegistry, _mockLoader, _mockValidator);

        // Act & Assert
        command.Description.Should().NotBeNullOrEmpty();
    }

    private static CommandContext CreateContext(string[] args, StringWriter output)
    {
        return new CommandContext
        {
            Configuration = new Dictionary<string, object>(),
            Args = args,
            Formatter = new ConsoleFormatter(output, enableColors: false),
            Output = output,
            CancellationToken = CancellationToken.None,
        };
    }

    private static PromptPack CreateTestPack(string id)
    {
        var components = new List<LoadedComponent>
        {
            new("system.md", ComponentType.System, "System prompt content.", null),
            new("roles/coder.md", ComponentType.Role, "Coder role content.", new Dictionary<string, string> { ["role"] = "coder" }),
        };

        return new PromptPack(
            id,
            new PackVersion(1, 0, 0),
            $"{id} Pack",
            "A test pack",
            PackSource.BuiltIn,
            $"/packs/{id}",
            null,
            components);
    }
}

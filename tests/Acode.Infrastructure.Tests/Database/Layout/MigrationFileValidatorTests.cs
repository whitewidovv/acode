#pragma warning disable CA2007 // xUnit tests should use ConfigureAwait(true)

using Acode.Infrastructure.Database.Layout;
using FluentAssertions;

namespace Acode.Infrastructure.Tests.Database.Layout;

/// <summary>
/// Tests for migration file structure and content validation.
/// </summary>
public sealed class MigrationFileValidatorTests
{
    private readonly MigrationFileValidator _sut = new();

    [Theory]
    [InlineData("001_initial_schema.sql", true)]
    [InlineData("002_add_conversations.sql", true)]
    [InlineData("099_final_cleanup.sql", true)]
    [InlineData("1_initial.sql", false)] // Not zero-padded
    [InlineData("initial_schema.sql", false)] // No version number
    [InlineData("001-initial-schema.sql", false)] // Hyphens instead of underscore
    [InlineData("001_InitialSchema.sql", false)] // PascalCase
    public void ValidateFileName_ShouldReturnExpectedResult(string fileName, bool expected)
    {
        // Act
        var result = _sut.ValidateFileName(fileName);

        // Assert
        result.IsValid.Should().Be(expected);
    }

    [Fact]
    public void ValidateDownScriptExists_ShouldRequireMatchingDownFile()
    {
        // Arrange
        var upFile = "migrations/005_add_sync.sql";

        // Act
        var withDown = _sut.ValidateDownScriptExists(upFile, downFileExists: true);
        var withoutDown = _sut.ValidateDownScriptExists(upFile, downFileExists: false);

        // Assert
        withDown.IsValid.Should().BeTrue();
        withoutDown.IsValid.Should().BeFalse();
        withoutDown.Errors.Should().Contain(e => e.Contains("_down.sql"));
    }

    [Fact]
    public void ValidateMigrationContent_ShouldRequireHeaderComment()
    {
        // Arrange
        var withHeader = @"-- migrations/001_initial.sql
-- Purpose: Create initial schema
-- Dependencies: None
CREATE TABLE sys_config (id TEXT PRIMARY KEY);";

        var withoutHeader = @"CREATE TABLE sys_config (id TEXT PRIMARY KEY);";

        // Act
        var withResult = _sut.ValidateContent(withHeader);
        var withoutResult = _sut.ValidateContent(withoutHeader);

        // Assert
        withResult.IsValid.Should().BeTrue();
        withoutResult.Warnings.Should().Contain(w => w.Contains("header comment"));
    }

    [Fact]
    public void ValidateMigrationContent_ShouldDetectForbiddenPatterns()
    {
        // Arrange
        var dangerous = @"-- Purpose: Backdoor
DROP DATABASE workspace;
GRANT ALL TO public;";

        // Act
        var result = _sut.ValidateContent(dangerous);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("DROP DATABASE"));
        result.Errors.Should().Contain(e => e.Contains("GRANT"));
    }

    [Fact]
    public void ValidateMigrationContent_ShouldRequireIfNotExists()
    {
        // Arrange
        var idempotent = "CREATE TABLE IF NOT EXISTS sys_config (id TEXT);";
        var notIdempotent = "CREATE TABLE sys_config (id TEXT);";

        // Act
        var idemResult = _sut.ValidateContent(idempotent);
        var notIdemResult = _sut.ValidateContent(notIdempotent);

        // Assert
        idemResult.IsValid.Should().BeTrue();
        notIdemResult.Warnings.Should().Contain(w => w.Contains("IF NOT EXISTS"));
    }

    [Fact]
    public void ExtractVersion_ShouldParseVersionFromFileName()
    {
        // Act
        var v1 = _sut.ExtractVersion("001_initial.sql");
        var v2 = _sut.ExtractVersion("042_add_feature.sql");
        var v3 = _sut.ExtractVersion("invalid.sql");

        // Assert
        v1.Should().Be(1);
        v2.Should().Be(42);
        v3.Should().BeNull();
    }

    [Fact]
    public void ValidateMigrationSequence_ShouldDetectGaps()
    {
        // Arrange
        var files = new[] { "001_a.sql", "002_b.sql", "004_d.sql" }; // Missing 003

        // Act
        var result = _sut.ValidateMigrationSequence(files);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("003"));
    }
}

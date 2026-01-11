// tests/Acode.Application.Tests/Database/MigrationOptionsTests.cs
namespace Acode.Application.Tests.Database;

using Acode.Application.Database;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for migration option records.
/// Verifies immutability and default values for MigrateOptions, RollbackOptions, and CreateOptions.
/// </summary>
public sealed class MigrationOptionsTests
{
    [Fact]
    public void MigrateOptions_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var options = new MigrateOptions();

        // Assert
        options.DryRun.Should().BeFalse();
        options.TargetVersion.Should().BeNull();
        options.SkipVersion.Should().BeNull();
        options.Force.Should().BeFalse();
        options.SkipChecksum.Should().BeFalse();
        options.CreateBackup.Should().BeTrue();
    }

    [Fact]
    public void MigrateOptions_ShouldAllowPropertyOverrides()
    {
        // Arrange & Act
        var options = new MigrateOptions
        {
            DryRun = true,
            TargetVersion = "005",
            SkipVersion = "003",
            Force = true,
            SkipChecksum = true,
            CreateBackup = false
        };

        // Assert
        options.DryRun.Should().BeTrue();
        options.TargetVersion.Should().Be("005");
        options.SkipVersion.Should().Be("003");
        options.Force.Should().BeTrue();
        options.SkipChecksum.Should().BeTrue();
        options.CreateBackup.Should().BeFalse();
    }

    [Fact]
    public void MigrateOptions_ShouldSupportWithExpression()
    {
        // Arrange
        var original = new MigrateOptions { DryRun = false };

        // Act
        var modified = original with { DryRun = true };

        // Assert
        modified.DryRun.Should().BeTrue();
        original.DryRun.Should().BeFalse("original should be unchanged");
    }

    [Fact]
    public void RollbackOptions_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var options = new RollbackOptions();

        // Assert
        options.Steps.Should().Be(1);
        options.TargetVersion.Should().BeNull();
        options.DryRun.Should().BeFalse();
        options.Force.Should().BeFalse();
        options.Confirm.Should().BeFalse();
    }

    [Fact]
    public void RollbackOptions_ShouldAllowPropertyOverrides()
    {
        // Arrange & Act
        var options = new RollbackOptions
        {
            Steps = 3,
            TargetVersion = "002",
            DryRun = true,
            Force = true,
            Confirm = true
        };

        // Assert
        options.Steps.Should().Be(3);
        options.TargetVersion.Should().Be("002");
        options.DryRun.Should().BeTrue();
        options.Force.Should().BeTrue();
        options.Confirm.Should().BeTrue();
    }

    [Fact]
    public void RollbackOptions_ShouldSupportWithExpression()
    {
        // Arrange
        var original = new RollbackOptions { Steps = 1 };

        // Act
        var modified = original with { Steps = 5 };

        // Assert
        modified.Steps.Should().Be(5);
        original.Steps.Should().Be(1, "original should be unchanged");
    }

    [Fact]
    public void CreateOptions_ShouldRequireName()
    {
        // Arrange & Act
        var options = new CreateOptions { Name = "add_user_table" };

        // Assert
        options.Name.Should().Be("add_user_table");
    }

    [Fact]
    public void CreateOptions_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var options = new CreateOptions { Name = "test_migration" };

        // Assert
        options.Template.Should().BeNull();
        options.NoDown.Should().BeFalse();
    }

    [Fact]
    public void CreateOptions_ShouldAllowPropertyOverrides()
    {
        // Arrange & Act
        var options = new CreateOptions
        {
            Name = "add_index",
            Template = "INDEX",
            NoDown = true
        };

        // Assert
        options.Name.Should().Be("add_index");
        options.Template.Should().Be("INDEX");
        options.NoDown.Should().BeTrue();
    }

    [Fact]
    public void CreateOptions_ShouldSupportWithExpression()
    {
        // Arrange
        var original = new CreateOptions { Name = "migration1", NoDown = false };

        // Act
        var modified = original with { NoDown = true };

        // Assert
        modified.NoDown.Should().BeTrue();
        original.NoDown.Should().BeFalse("original should be unchanged");
    }
}

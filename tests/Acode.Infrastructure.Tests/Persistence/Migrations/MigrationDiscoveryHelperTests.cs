// tests/Acode.Infrastructure.Tests/Persistence/Migrations/MigrationDiscoveryHelperTests.cs
namespace Acode.Infrastructure.Tests.Persistence.Migrations;

using Acode.Application.Database;
using Acode.Infrastructure.Persistence.Migrations;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for migration discovery helper types (EmbeddedResource, MigrationOptions, exceptions).
/// </summary>
public sealed class MigrationDiscoveryHelperTests
{
    [Fact]
    public void EmbeddedResource_ShouldInitializeAllProperties()
    {
        // Arrange & Act
        var resource = new EmbeddedResource("001_initial.sql", "CREATE TABLE test (id TEXT);");

        // Assert
        resource.Name.Should().Be("001_initial.sql");
        resource.Content.Should().Be("CREATE TABLE test (id TEXT);");
    }

    [Fact]
    public void MigrationOptions_ShouldHaveDefaultDirectory()
    {
        // Arrange & Act
        var options = new MigrationOptions();

        // Assert
        options.Directory.Should().Be(".agent/migrations");
    }

    [Fact]
    public void MigrationOptions_ShouldAllowCustomDirectory()
    {
        // Arrange & Act
        var options = new MigrationOptions { Directory = "/custom/path" };

        // Assert
        options.Directory.Should().Be("/custom/path");
    }

    [Fact]
    public void DuplicateMigrationVersionException_ShouldHaveCorrectErrorCode()
    {
        // Arrange & Act
        var exception = new DuplicateMigrationVersionException("001");

        // Assert
        exception.ErrorCode.Should().Be("ACODE-MIG-009");
        exception.Version.Should().Be("001");
        exception.Message.Should().Contain("001");
        exception.Message.Should().Contain("Duplicate migration version");
    }

    [Fact]
    public void DuplicateMigrationVersionException_ShouldIncludeVersionInMessage()
    {
        // Arrange & Act
        var exception = new DuplicateMigrationVersionException("005_add_feature");

        // Assert
        exception.Version.Should().Be("005_add_feature");
        exception.Message.Should().Contain("005_add_feature");
    }
}

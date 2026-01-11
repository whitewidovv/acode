// tests/Acode.Infrastructure.Tests/Persistence/Migrations/MigrationDiscoveryTests.cs
#pragma warning disable CA2007 // ConfigureAwait not required in test code

namespace Acode.Infrastructure.Tests.Persistence.Migrations;

using Acode.Application.Database;
using Acode.Infrastructure.Persistence.Migrations;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

/// <summary>
/// Tests for MigrationDiscovery class.
/// Verifies embedded and file-based migration discovery, ordering, pairing, and duplicate detection.
/// </summary>
public sealed class MigrationDiscoveryTests
{
    private readonly MigrationDiscovery _sut;
    private readonly IFileSystem _fileSystemMock;
    private readonly IEmbeddedResourceProvider _embeddedMock;
    private readonly ILogger<MigrationDiscovery> _loggerMock;
    private readonly string _migrationsDir = "/app/.agent/migrations";

    public MigrationDiscoveryTests()
    {
        _fileSystemMock = Substitute.For<IFileSystem>();
        _embeddedMock = Substitute.For<IEmbeddedResourceProvider>();
        _loggerMock = Substitute.For<ILogger<MigrationDiscovery>>();

        _sut = new MigrationDiscovery(
            _fileSystemMock,
            _embeddedMock,
            _loggerMock,
            Options.Create(new MigrationOptions { Directory = _migrationsDir }));
    }

    [Fact]
    public async Task DiscoverAsync_FindsEmbeddedMigrations()
    {
        // Arrange
        _embeddedMock.GetMigrationResourcesAsync(Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new EmbeddedResource("001_initial_schema.sql", "CREATE TABLE test (id TEXT);"),
                new EmbeddedResource("002_add_column.sql", "ALTER TABLE test ADD col TEXT;")
            });
        _fileSystemMock.GetFilesAsync(_migrationsDir, "*.sql", Arg.Any<CancellationToken>())
            .Returns(Array.Empty<string>());

        // Act
        var result = await _sut.DiscoverAsync(CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].Version.Should().Be("001");
        result[1].Version.Should().Be("002");
        result.All(m => m.Source == MigrationSource.Embedded).Should().BeTrue();
    }

    [Fact]
    public async Task DiscoverAsync_FindsFileBasedMigrations()
    {
        // Arrange
        _embeddedMock.GetMigrationResourcesAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<EmbeddedResource>());
        _fileSystemMock.GetFilesAsync(_migrationsDir, "*.sql", Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                "/app/.agent/migrations/003_add_feature.sql",
                "/app/.agent/migrations/003_add_feature_down.sql"
            });
        _fileSystemMock.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("CREATE TABLE feature (id TEXT);");

        // Act
        var result = await _sut.DiscoverAsync(CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Version.Should().Be("003");
        result[0].Source.Should().Be(MigrationSource.File);
        result[0].HasDownScript.Should().BeTrue();
    }

    [Fact]
    public async Task DiscoverAsync_OrdersByVersionNumber()
    {
        // Arrange
        _embeddedMock.GetMigrationResourcesAsync(Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new EmbeddedResource("010_tenth.sql", "SQL"),
                new EmbeddedResource("002_second.sql", "SQL"),
                new EmbeddedResource("001_first.sql", "SQL")
            });
        _fileSystemMock.GetFilesAsync(_migrationsDir, "*.sql", Arg.Any<CancellationToken>())
            .Returns(Array.Empty<string>());

        // Act
        var result = await _sut.DiscoverAsync(CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
        result[0].Version.Should().Be("001");
        result[1].Version.Should().Be("002");
        result[2].Version.Should().Be("010");
    }

    [Fact]
    public async Task DiscoverAsync_PairsUpAndDownScripts()
    {
        // Arrange
        _embeddedMock.GetMigrationResourcesAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<EmbeddedResource>());
        _fileSystemMock.GetFilesAsync(_migrationsDir, "*.sql", Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                "/app/.agent/migrations/001_create_table.sql",
                "/app/.agent/migrations/001_create_table_down.sql",
                "/app/.agent/migrations/002_add_column.sql"
            });
        _fileSystemMock.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("SQL content");

        // Act
        var result = await _sut.DiscoverAsync(CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].HasDownScript.Should().BeTrue();
        result[1].HasDownScript.Should().BeFalse();
    }

    [Fact]
    public async Task DiscoverAsync_ThrowsOnDuplicateVersion()
    {
        // Arrange
        _embeddedMock.GetMigrationResourcesAsync(Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new EmbeddedResource("001_first.sql", "SQL")
            });
        _fileSystemMock.GetFilesAsync(_migrationsDir, "*.sql", Arg.Any<CancellationToken>())
            .Returns(new[] { "/app/.agent/migrations/001_duplicate.sql" });
        _fileSystemMock.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("SQL");

        // Act
        var act = () => _sut.DiscoverAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DuplicateMigrationVersionException>()
            .WithMessage("*001*");
    }

    [Fact]
    public async Task DiscoverAsync_LogsWarningForMissingDownScript()
    {
        // Arrange
        _embeddedMock.GetMigrationResourcesAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { new EmbeddedResource("001_initial_schema.sql", "SQL") });
        _fileSystemMock.GetFilesAsync(_migrationsDir, "*.sql", Arg.Any<CancellationToken>())
            .Returns(Array.Empty<string>());

        // Act
        await _sut.DiscoverAsync(CancellationToken.None);

        // Assert
        _loggerMock.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(v => v.ToString()!.Contains("001")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }
}

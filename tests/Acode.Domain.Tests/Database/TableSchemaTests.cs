#pragma warning disable CA2007 // xUnit tests should use ConfigureAwait(true)

using Acode.Domain.Database;
using FluentAssertions;

namespace Acode.Domain.Tests.Database;

/// <summary>
/// Tests for <see cref="TableSchema"/> domain model.
/// </summary>
public sealed class TableSchemaTests
{
    [Fact]
    public void Constructor_WithNameAndColumns_ShouldSetProperties()
    {
        // Arrange
        var columns = new[]
        {
            new ColumnSchema("id", "TEXT", isPrimaryKey: true),
            new ColumnSchema("name", "TEXT")
        };

        // Act
        var table = new TableSchema("conv_chats", columns);

        // Assert
        table.Name.Should().Be("conv_chats");
        table.Columns.Should().HaveCount(2);
        table.Indexes.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithIndexes_ShouldSetIndexes()
    {
        // Arrange
        var columns = new[]
        {
            new ColumnSchema("id", "TEXT", isPrimaryKey: true),
            new ColumnSchema("created_at", "TEXT")
        };
        var indexes = new[] { "idx_conv_chats_created" };

        // Act
        var table = new TableSchema("conv_chats", columns, indexes);

        // Assert
        table.Indexes.Should().ContainSingle();
        table.Indexes.Should().Contain("idx_conv_chats_created");
    }

    [Fact]
    public void Constructor_WithNullName_ShouldThrowArgumentNullException()
    {
        // Arrange
        var columns = new[] { new ColumnSchema("id", "TEXT") };

        // Act
        var act = () => new TableSchema(null!, columns);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("name");
    }

    [Fact]
    public void Constructor_WithNullColumns_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new TableSchema("conv_chats", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("columns");
    }

    [Fact]
    public void PrimaryKey_WithPrimaryKeyColumn_ShouldReturnColumn()
    {
        // Arrange
        var columns = new[]
        {
            new ColumnSchema("id", "TEXT", isPrimaryKey: true),
            new ColumnSchema("name", "TEXT")
        };
        var table = new TableSchema("conv_chats", columns);

        // Act
        var pk = table.PrimaryKey;

        // Assert
        pk.Should().NotBeNull();
        pk!.Name.Should().Be("id");
        pk.IsPrimaryKey.Should().BeTrue();
    }

    [Fact]
    public void PrimaryKey_WithNoPrimaryKey_ShouldReturnNull()
    {
        // Arrange
        var columns = new[]
        {
            new ColumnSchema("name", "TEXT"),
            new ColumnSchema("value", "TEXT")
        };
        var table = new TableSchema("sys_config", columns);

        // Act
        var pk = table.PrimaryKey;

        // Assert
        pk.Should().BeNull();
    }

    [Fact]
    public void ForeignKeys_WithForeignKeyColumns_ShouldReturnThem()
    {
        // Arrange
        var columns = new[]
        {
            new ColumnSchema("id", "TEXT", isPrimaryKey: true),
            new ColumnSchema("chat_id", "TEXT", isForeignKey: true, foreignKeyTable: "conv_chats"),
            new ColumnSchema("user_id", "TEXT", isForeignKey: true, foreignKeyTable: "users"),
            new ColumnSchema("name", "TEXT")
        };
        var table = new TableSchema("conv_runs", columns);

        // Act
        var foreignKeys = table.ForeignKeys.ToList();

        // Assert
        foreignKeys.Should().HaveCount(2);
        foreignKeys.Should().Contain(fk => fk.Name == "chat_id");
        foreignKeys.Should().Contain(fk => fk.Name == "user_id");
        foreignKeys.All(fk => fk.IsForeignKey).Should().BeTrue();
    }

    [Fact]
    public void ForeignKeys_WithNoForeignKeys_ShouldReturnEmpty()
    {
        // Arrange
        var columns = new[]
        {
            new ColumnSchema("id", "TEXT", isPrimaryKey: true),
            new ColumnSchema("name", "TEXT")
        };
        var table = new TableSchema("conv_chats", columns);

        // Act
        var foreignKeys = table.ForeignKeys;

        // Assert
        foreignKeys.Should().BeEmpty();
    }

    [Theory]
    [InlineData("conv_chats", "conv_")]
    [InlineData("sess_sessions", "sess_")]
    [InlineData("appr_records", "appr_")]
    [InlineData("sync_outbox", "sync_")]
    [InlineData("sys_config", "sys_")]
    [InlineData("__migrations", "__")]
    public void DomainPrefix_ShouldExtractPrefixCorrectly(string tableName, string expectedPrefix)
    {
        // Arrange
        var columns = new[] { new ColumnSchema("id", "TEXT") };
        var table = new TableSchema(tableName, columns);

        // Act & Assert
        table.DomainPrefix.Should().Be(expectedPrefix);
    }

    [Fact]
    public void Columns_ShouldBeImmutable()
    {
        // Arrange
        var columns = new[] { new ColumnSchema("id", "TEXT") };
        var table = new TableSchema("conv_chats", columns);

        // Act & Assert
        table.Columns.Should().BeAssignableTo<IReadOnlyList<ColumnSchema>>();
    }

    [Fact]
    public void Indexes_ShouldBeImmutable()
    {
        // Arrange
        var columns = new[] { new ColumnSchema("id", "TEXT") };
        var indexes = new[] { "idx_conv_chats_id" };
        var table = new TableSchema("conv_chats", columns, indexes);

        // Act & Assert
        table.Indexes.Should().BeAssignableTo<IReadOnlyList<string>>();
    }

    [Fact]
    public void Name_ShouldBeAccessible()
    {
        // Arrange
        var columns = new[] { new ColumnSchema("id", "TEXT") };
        var table = new TableSchema("conv_chats", columns);

        // Assert
        table.Name.Should().Be("conv_chats");
    }

    [Fact]
    public void ComplexTable_WithAllFeatures_ShouldWorkCorrectly()
    {
        // Arrange
        var columns = new[]
        {
            new ColumnSchema("id", "TEXT", isNullable: false, isPrimaryKey: true),
            new ColumnSchema("chat_id", "TEXT", isNullable: false, isForeignKey: true, foreignKeyTable: "conv_chats"),
            new ColumnSchema("status", "TEXT", defaultValue: "'running'"),
            new ColumnSchema("created_at", "TEXT", defaultValue: "strftime('%Y-%m-%dT%H:%M:%SZ', 'now')"),
            new ColumnSchema("is_deleted", "INTEGER", defaultValue: "0")
        };
        var indexes = new[]
        {
            "idx_conv_runs_chat",
            "idx_conv_runs_status",
            "idx_conv_runs_created"
        };
        var table = new TableSchema("conv_runs", columns, indexes);

        // Assert
        table.Name.Should().Be("conv_runs");
        table.Columns.Should().HaveCount(5);
        table.Indexes.Should().HaveCount(3);
        table.PrimaryKey.Should().NotBeNull();
        table.PrimaryKey!.Name.Should().Be("id");
        table.ForeignKeys.Should().ContainSingle();
        table.ForeignKeys.First().Name.Should().Be("chat_id");
        table.DomainPrefix.Should().Be("conv_");
    }
}

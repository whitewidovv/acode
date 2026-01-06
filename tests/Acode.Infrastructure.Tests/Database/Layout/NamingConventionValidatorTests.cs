#pragma warning disable CA2007 // xUnit tests should use ConfigureAwait(true)

using Acode.Domain.Database;
using Acode.Infrastructure.Database.Layout;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.Database.Layout;

/// <summary>
/// Tests for database naming convention validation.
/// Verifies that table names, column names, indexes, and foreign keys
/// follow the established conventions.
/// </summary>
public sealed class NamingConventionValidatorTests
{
    private readonly NamingConventionValidator _sut = new();

    [Theory]
    [InlineData("conv_chats", true)]
    [InlineData("sess_sessions", true)]
    [InlineData("appr_records", true)]
    [InlineData("sync_outbox", true)]
    [InlineData("sys_config", true)]
    [InlineData("__migrations", true)]
    [InlineData("UserMessages", false)] // PascalCase not allowed
    [InlineData("user_messages", false)] // Missing domain prefix
    [InlineData("convChats", false)] // camelCase not allowed
    [InlineData("CONV_CHATS", false)] // UPPERCASE not allowed
    public void ValidateTableName_ShouldReturnExpectedResult(string tableName, bool expected)
    {
        // Act
        var result = _sut.ValidateTableName(tableName);

        // Assert
        result.IsValid.Should().Be(expected);
    }

    [Theory]
    [InlineData("id", true)]
    [InlineData("created_at", true)]
    [InlineData("updated_at", true)]
    [InlineData("chat_id", true)]
    [InlineData("is_deleted", true)]
    [InlineData("sync_status", true)]
    [InlineData("CreatedAt", false)] // PascalCase
    [InlineData("chatId", false)] // camelCase
    [InlineData("ID", false)] // UPPERCASE
    [InlineData("isDeleted", false)] // camelCase
    public void ValidateColumnName_ShouldReturnExpectedResult(string columnName, bool expected)
    {
        // Act
        var result = _sut.ValidateColumnName(columnName);

        // Assert
        result.IsValid.Should().Be(expected);
    }

    [Theory]
    [InlineData("idx_conv_chats_worktree", true)]
    [InlineData("idx_conv_messages_chat_created", true)]
    [InlineData("ux_sys_config_key", true)]
    [InlineData("fk_conv_runs_chat", true)]
    [InlineData("PK_ConvChats", false)] // PascalCase
    [InlineData("ix_chats", false)] // Wrong prefix format
    [InlineData("conv_chats_idx", false)] // Wrong order
    public void ValidateIndexName_ShouldReturnExpectedResult(string indexName, bool expected)
    {
        // Act
        var result = _sut.ValidateIndexName(indexName);

        // Assert
        result.IsValid.Should().Be(expected);
    }

    [Fact]
    public void ValidateTableName_WithInvalidPrefix_ShouldReturnError()
    {
        // Act
        var result = _sut.ValidateTableName("msg_content");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("prefix"));
    }

    [Fact]
    public void ValidatePrimaryKeyColumn_ShouldRequireIdColumn()
    {
        // Arrange
        var tableSchema = new TableSchema("conv_chats", new[]
        {
            new ColumnSchema("chat_id", "TEXT", isPrimaryKey: true),
            new ColumnSchema("title", "TEXT", isPrimaryKey: false)
        });

        // Act
        var result = _sut.ValidatePrimaryKey(tableSchema);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("'id'"));
    }

    [Fact]
    public void ValidateForeignKeyColumn_ShouldFollowNamingPattern()
    {
        // Act
        var valid = _sut.ValidateForeignKeyColumn("chat_id", "conv_chats");
        var invalid = _sut.ValidateForeignKeyColumn("parentChat", "conv_chats");

        // Assert
        valid.IsValid.Should().BeTrue();
        invalid.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("created_at", true)]
    [InlineData("updated_at", true)]
    [InlineData("deleted_at", true)]
    [InlineData("sync_at", true)]
    [InlineData("applied_at", true)]
    [InlineData("createdDate", false)] // Wrong pattern
    [InlineData("last_modified", false)] // Should be modified_at
    public void ValidateTimestampColumn_ShouldFollowAtPattern(string columnName, bool expected)
    {
        // Act
        var result = _sut.ValidateTimestampColumn(columnName);

        // Assert
        result.IsValid.Should().Be(expected);
    }

    [Theory]
    [InlineData("is_deleted", true)]
    [InlineData("is_active", true)]
    [InlineData("is_enabled", true)]
    [InlineData("is_internal", true)]
    [InlineData("deleted", false)] // Missing is_ prefix
    [InlineData("active", false)] // Missing is_ prefix
    [InlineData("isActive", false)] // camelCase
    public void ValidateBooleanColumn_ShouldFollowIsPattern(string columnName, bool expected)
    {
        // Act
        var result = _sut.ValidateBooleanColumn(columnName);

        // Assert
        result.IsValid.Should().Be(expected);
    }
}

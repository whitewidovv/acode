#pragma warning disable CA2007 // xUnit tests should use ConfigureAwait(true)

using Acode.Domain.Database;
using FluentAssertions;

namespace Acode.Domain.Tests.Database;

/// <summary>
/// Tests for <see cref="ColumnSchema"/> domain model.
/// </summary>
public sealed class ColumnSchemaTests
{
    [Fact]
    public void Constructor_WithNameAndType_ShouldSetProperties()
    {
        // Act
        var column = new ColumnSchema("user_id", "TEXT");

        // Assert
        column.Name.Should().Be("user_id");
        column.DataType.Should().Be("TEXT");
        column.IsNullable.Should().BeTrue(); // Default
        column.IsPrimaryKey.Should().BeFalse(); // Default
        column.IsForeignKey.Should().BeFalse(); // Default
        column.ForeignKeyTable.Should().BeNull(); // Default
        column.DefaultValue.Should().BeNull(); // Default
    }

    [Fact]
    public void Constructor_WithAllParameters_ShouldSetAllProperties()
    {
        // Act
        var column = new ColumnSchema(
            name: "chat_id",
            dataType: "TEXT",
            isNullable: false,
            isPrimaryKey: false,
            isForeignKey: true,
            foreignKeyTable: "conv_chats",
            defaultValue: null);

        // Assert
        column.Name.Should().Be("chat_id");
        column.DataType.Should().Be("TEXT");
        column.IsNullable.Should().BeFalse();
        column.IsPrimaryKey.Should().BeFalse();
        column.IsForeignKey.Should().BeTrue();
        column.ForeignKeyTable.Should().Be("conv_chats");
        column.DefaultValue.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithNullName_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new ColumnSchema(null!, "TEXT");

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("name");
    }

    [Fact]
    public void Constructor_WithNullDataType_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new ColumnSchema("id", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("dataType");
    }

    [Theory]
    [InlineData("created_at", true)]
    [InlineData("updated_at", true)]
    [InlineData("deleted_at", true)]
    [InlineData("sync_at", true)]
    [InlineData("applied_at", true)]
    [InlineData("id", false)]
    [InlineData("name", false)]
    [InlineData("status", false)]
    public void IsTimestamp_ShouldDetectTimestampColumns(string columnName, bool expectedIsTimestamp)
    {
        // Arrange
        var column = new ColumnSchema(columnName, "TEXT");

        // Act & Assert
        column.IsTimestamp.Should().Be(expectedIsTimestamp);
    }

    [Theory]
    [InlineData("is_deleted", true)]
    [InlineData("is_active", true)]
    [InlineData("is_archived", true)]
    [InlineData("is_internal", true)]
    [InlineData("deleted", false)]
    [InlineData("active", false)]
    [InlineData("id", false)]
    public void IsBoolean_ShouldDetectBooleanColumns(string columnName, bool expectedIsBoolean)
    {
        // Arrange
        var column = new ColumnSchema(columnName, "INTEGER");

        // Act & Assert
        column.IsBoolean.Should().Be(expectedIsBoolean);
    }

    [Theory]
    [InlineData("id", true)]
    [InlineData("user_id", true)]
    [InlineData("chat_id", true)]
    [InlineData("worktree_id", true)]
    [InlineData("name", false)]
    [InlineData("status", false)]
    [InlineData("identity", false)] // Contains "id" but doesn't match pattern
    public void IsId_ShouldDetectIdColumns(string columnName, bool expectedIsId)
    {
        // Arrange
        var column = new ColumnSchema(columnName, "TEXT");

        // Act & Assert
        column.IsId.Should().Be(expectedIsId);
    }

    [Fact]
    public void PrimaryKeyColumn_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var column = new ColumnSchema(
            name: "id",
            dataType: "TEXT",
            isNullable: false,
            isPrimaryKey: true);

        // Assert
        column.IsPrimaryKey.Should().BeTrue();
        column.IsNullable.Should().BeFalse();
        column.IsId.Should().BeTrue();
    }

    [Fact]
    public void ForeignKeyColumn_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var column = new ColumnSchema(
            name: "chat_id",
            dataType: "TEXT",
            isNullable: false,
            isForeignKey: true,
            foreignKeyTable: "conv_chats");

        // Assert
        column.IsForeignKey.Should().BeTrue();
        column.ForeignKeyTable.Should().Be("conv_chats");
        column.IsId.Should().BeTrue(); // Ends with _id
    }

    [Fact]
    public void ColumnWithDefaultValue_ShouldReturnDefaultValue()
    {
        // Arrange & Act
        var column = new ColumnSchema(
            name: "is_deleted",
            dataType: "INTEGER",
            defaultValue: "0");

        // Assert
        column.DefaultValue.Should().Be("0");
    }

    [Fact]
    public void Record_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var column1 = new ColumnSchema("id", "TEXT", isPrimaryKey: true);
        var column2 = new ColumnSchema("id", "TEXT", isPrimaryKey: true);

        // Assert
        column1.Should().Be(column2);
        (column1 == column2).Should().BeTrue();
    }

    [Fact]
    public void Record_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var column1 = new ColumnSchema("id", "TEXT");
        var column2 = new ColumnSchema("name", "TEXT");

        // Assert
        column1.Should().NotBe(column2);
        (column1 != column2).Should().BeTrue();
    }
}

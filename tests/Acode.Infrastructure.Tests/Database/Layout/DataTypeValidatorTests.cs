#pragma warning disable CA2007 // xUnit tests should use ConfigureAwait(true)

using Acode.Domain.Database;
using Acode.Infrastructure.Database.Layout;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.Database.Layout;

/// <summary>
/// Tests for database data type validation.
/// Verifies that columns use correct types for IDs, timestamps, booleans, etc.
/// </summary>
public sealed class DataTypeValidatorTests
{
    private readonly DataTypeValidator _sut = new();

    [Fact]
    public void ValidateIdColumn_ShouldRequireTextType()
    {
        // Arrange
        var textColumn = new ColumnSchema("id", "TEXT", isPrimaryKey: true);
        var intColumn = new ColumnSchema("id", "INTEGER", isPrimaryKey: true);

        // Act
        var textResult = _sut.ValidateIdColumn(textColumn);
        var intResult = _sut.ValidateIdColumn(intColumn);

        // Assert
        textResult.IsValid.Should().BeTrue();
        intResult.IsValid.Should().BeFalse();
        intResult.Errors.Should().Contain(e => e.Contains("TEXT"));
    }

    [Fact]
    public void ValidateTimestampColumn_ShouldRequireTextType()
    {
        // Arrange
        var textColumn = new ColumnSchema("created_at", "TEXT");
        var datetimeColumn = new ColumnSchema("created_at", "DATETIME");

        // Act
        var textResult = _sut.ValidateTimestampColumn(textColumn);
        var datetimeResult = _sut.ValidateTimestampColumn(datetimeColumn);

        // Assert
        textResult.IsValid.Should().BeTrue();
        datetimeResult.IsValid.Should().BeTrue(); // Warnings still mean valid
        datetimeResult.Warnings.Should().Contain(w => w.Contains("ISO 8601"));
    }

    [Fact]
    public void ValidateBooleanColumn_ShouldRequireIntegerType()
    {
        // Arrange
        var intColumn = new ColumnSchema("is_deleted", "INTEGER");
        var boolColumn = new ColumnSchema("is_deleted", "BOOLEAN");

        // Act
        var intResult = _sut.ValidateBooleanColumn(intColumn);
        var boolResult = _sut.ValidateBooleanColumn(boolColumn);

        // Assert
        intResult.IsValid.Should().BeTrue();
        boolResult.IsValid.Should().BeFalse();
        boolResult.Errors.Should().Contain(e => e.Contains("INTEGER"));
    }

    [Fact]
    public void ValidateJsonColumn_ShouldRequireTextType()
    {
        // Arrange
        var textColumn = new ColumnSchema("metadata", "TEXT");
        var jsonColumn = new ColumnSchema("metadata", "JSON");

        // Act
        var textResult = _sut.ValidateJsonColumn(textColumn);
        var jsonResult = _sut.ValidateJsonColumn(jsonColumn);

        // Assert
        textResult.IsValid.Should().BeTrue();
        jsonResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateForeignKeyColumn_ShouldRequireTextType()
    {
        // Arrange
        var textFk = new ColumnSchema("chat_id", "TEXT");
        var intFk = new ColumnSchema("chat_id", "INTEGER");

        // Act
        var textResult = _sut.ValidateForeignKeyColumn(textFk);
        var intResult = _sut.ValidateForeignKeyColumn(intFk);

        // Assert
        textResult.IsValid.Should().BeTrue();
        intResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateEnumColumn_ShouldRequireTextType()
    {
        // Arrange
        var textColumn = new ColumnSchema("sync_status", "TEXT");
        var intColumn = new ColumnSchema("sync_status", "INTEGER");

        // Act
        var textResult = _sut.ValidateEnumColumn(textColumn);
        var intResult = _sut.ValidateEnumColumn(intColumn);

        // Assert
        textResult.IsValid.Should().BeTrue();
        intResult.Warnings.Should().Contain(w => w.Contains("TEXT for enum"));
    }
}

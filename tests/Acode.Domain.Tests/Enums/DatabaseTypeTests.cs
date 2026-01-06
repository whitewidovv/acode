// tests/Acode.Domain.Tests/Enums/DatabaseTypeTests.cs
namespace Acode.Domain.Tests.Enums;

using Acode.Domain.Enums;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for DatabaseType enum.
/// Verifies that enum values are correctly defined for supported database types.
/// </summary>
public sealed class DatabaseTypeTests
{
    [Fact]
    public void DatabaseType_ShouldHaveSqliteValue()
    {
        // Arrange & Act
        var sqliteValue = DatabaseType.Sqlite;

        // Assert
        sqliteValue.Should().Be(DatabaseType.Sqlite);
        Enum.IsDefined(typeof(DatabaseType), sqliteValue).Should().BeTrue();
    }

    [Fact]
    public void DatabaseType_ShouldHavePostgresValue()
    {
        // Arrange & Act
        var postgresValue = DatabaseType.Postgres;

        // Assert
        postgresValue.Should().Be(DatabaseType.Postgres);
        Enum.IsDefined(typeof(DatabaseType), postgresValue).Should().BeTrue();
    }

    [Fact]
    public void DatabaseType_ShouldHaveExactlyTwoValues()
    {
        // Arrange & Act
        var values = Enum.GetValues<DatabaseType>();

        // Assert
        values.Should().HaveCount(2);
        values.Should().Contain(DatabaseType.Sqlite);
        values.Should().Contain(DatabaseType.Postgres);
    }

    [Theory]
    [InlineData(DatabaseType.Sqlite, "Sqlite")]
    [InlineData(DatabaseType.Postgres, "Postgres")]
    public void DatabaseType_ToString_ShouldReturnCorrectName(DatabaseType type, string expected)
    {
        // Arrange & Act
        var name = type.ToString();

        // Assert
        name.Should().Be(expected);
    }

    [Theory]
    [InlineData("Sqlite", DatabaseType.Sqlite)]
    [InlineData("Postgres", DatabaseType.Postgres)]
    public void DatabaseType_Parse_ShouldReturnCorrectValue(string name, DatabaseType expected)
    {
        // Arrange & Act
        var parsed = Enum.Parse<DatabaseType>(name);

        // Assert
        parsed.Should().Be(expected);
    }

    [Fact]
    public void DatabaseType_Parse_ShouldBeCaseInsensitive()
    {
        // Arrange & Act
        var sqlite = Enum.Parse<DatabaseType>("sqlite", ignoreCase: true);
        var postgres = Enum.Parse<DatabaseType>("POSTGRES", ignoreCase: true);

        // Assert
        sqlite.Should().Be(DatabaseType.Sqlite);
        postgres.Should().Be(DatabaseType.Postgres);
    }

    [Fact]
    public void DatabaseType_TryParse_ShouldReturnTrueForValidValues()
    {
        // Arrange & Act
        var sqliteSuccess = Enum.TryParse<DatabaseType>("Sqlite", out var sqliteValue);
        var postgresSuccess = Enum.TryParse<DatabaseType>("Postgres", out var postgresValue);

        // Assert
        sqliteSuccess.Should().BeTrue();
        sqliteValue.Should().Be(DatabaseType.Sqlite);

        postgresSuccess.Should().BeTrue();
        postgresValue.Should().Be(DatabaseType.Postgres);
    }

    [Fact]
    public void DatabaseType_TryParse_ShouldReturnFalseForInvalidValues()
    {
        // Arrange & Act
        var success = Enum.TryParse<DatabaseType>("MySQL", out var value);

        // Assert
        success.Should().BeFalse();
        value.Should().Be(default(DatabaseType));
    }
}

using Acode.Application.Database;
using FluentAssertions;
using Xunit;

namespace Acode.Application.Tests.Database;

/// <summary>
/// Tests for <see cref="IConnectionFactory"/> contract requirements.
/// </summary>
/// <remarks>
/// These tests verify the behavioral contract that all IConnectionFactory
/// implementations must satisfy. Specific provider tests verify implementation details.
/// </remarks>
public sealed class IConnectionFactoryTests
{
    [Fact]
    public void IConnectionFactory_MustHaveCreateAsync_Method()
    {
        // Arrange
        var factoryType = typeof(IConnectionFactory);

        // Act
        var createMethod = factoryType.GetMethod(
            "CreateAsync",
            new[] { typeof(CancellationToken) });

        // Assert
        createMethod.Should().NotBeNull("IConnectionFactory must have CreateAsync(CancellationToken) method");
        createMethod!.ReturnType.Should().Be(typeof(Task<IDbConnection>), "CreateAsync must return Task<IDbConnection>");
    }

    [Fact]
    public void IConnectionFactory_MustHaveProviderType_Property()
    {
        // Arrange
        var factoryType = typeof(IConnectionFactory);

        // Act
        var providerProperty = factoryType.GetProperty("ProviderType");

        // Assert
        providerProperty.Should().NotBeNull("IConnectionFactory must have ProviderType property");
        providerProperty!.PropertyType.Should().Be(typeof(DbProviderType), "ProviderType must be DbProviderType enum");
        providerProperty.CanRead.Should().BeTrue("ProviderType must be readable");
        providerProperty.CanWrite.Should().BeFalse("ProviderType must be get-only");
    }

    [Fact]
    public void IConnectionFactory_MustHaveConnectionString_Property()
    {
        // Arrange
        var factoryType = typeof(IConnectionFactory);

        // Act
        var connStringProperty = factoryType.GetProperty("ConnectionString");

        // Assert
        connStringProperty.Should().NotBeNull("IConnectionFactory must have ConnectionString property");
        connStringProperty!.PropertyType.Should().Be(typeof(string), "ConnectionString must be string");
        connStringProperty.CanRead.Should().BeTrue("ConnectionString must be readable");
    }
}

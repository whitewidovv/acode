using Acode.Application.Database;
using FluentAssertions;

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
    public void IConnectionFactory_MustHaveCheckHealthAsync_Method()
    {
        // Arrange
        var factoryType = typeof(IConnectionFactory);

        // Act
        var healthCheckMethod = factoryType.GetMethod(
            "CheckHealthAsync",
            new[] { typeof(CancellationToken) });

        // Assert
        healthCheckMethod.Should().NotBeNull("IConnectionFactory must have CheckHealthAsync(CancellationToken) method");
        healthCheckMethod!.ReturnType.Should().Be(typeof(Task<HealthCheckResult>), "CheckHealthAsync must return Task<HealthCheckResult>");
    }

    [Fact]
    public void IConnectionFactory_MustHaveProvider_Property()
    {
        // Arrange
        var factoryType = typeof(IConnectionFactory);

        // Act
        var providerProperty = factoryType.GetProperty("Provider");

        // Assert
        providerProperty.Should().NotBeNull("IConnectionFactory must have Provider property");
        providerProperty!.PropertyType.Should().Be(typeof(DatabaseProvider), "Provider must be DatabaseProvider enum");
        providerProperty.CanRead.Should().BeTrue("Provider must be readable");
        providerProperty.CanWrite.Should().BeFalse("Provider must be get-only");
    }
}

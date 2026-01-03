using Acode.Infrastructure.Services;
using FluentAssertions;

namespace Acode.Infrastructure.Tests;

/// <summary>
/// Placeholder tests for Infrastructure layer.
/// </summary>
public class PlaceholderServiceTests
{
    [Fact]
    public void DoSomething_ShouldReturnMessage_WhenCalled()
    {
        // Arrange
#pragma warning disable CS0618
        var service = new PlaceholderService();
#pragma warning restore CS0618

        // Act
        var result = service.DoSomething();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Be("Placeholder service executed");
    }
}

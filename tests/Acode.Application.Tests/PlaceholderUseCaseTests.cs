using Acode.Application.UseCases;
using FluentAssertions;

namespace Acode.Application.Tests;

/// <summary>
/// Placeholder tests for Application layer.
/// </summary>
public class PlaceholderUseCaseTests
{
    [Fact]
    public void Execute_ShouldReturnMessage_WhenCalled()
    {
        // Arrange
#pragma warning disable CS0618
        var useCase = new PlaceholderUseCase();
#pragma warning restore CS0618

        // Act
        var result = useCase.Execute();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Be("Placeholder use case executed");
    }
}

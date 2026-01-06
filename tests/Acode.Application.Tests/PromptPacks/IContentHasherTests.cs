using Acode.Application.PromptPacks;
using Acode.Domain.PromptPacks;
using FluentAssertions;
using Xunit;

namespace Acode.Application.Tests.PromptPacks;

/// <summary>
/// Tests for <see cref="IContentHasher"/> contract.
/// </summary>
public class IContentHasherTests
{
    [Fact]
    public void Interface_ShouldHaveComputeMethod()
    {
        // Arrange
        var interfaceType = typeof(IContentHasher);

        // Act
        var computeMethod = interfaceType.GetMethod(nameof(IContentHasher.Compute));

        // Assert
        computeMethod.Should().NotBeNull();
        computeMethod!.ReturnType.Should().Be(typeof(ContentHash));
    }

    [Fact]
    public void Interface_ShouldHaveVerifyMethod()
    {
        // Arrange
        var interfaceType = typeof(IContentHasher);

        // Act
        var verifyMethod = interfaceType.GetMethod(nameof(IContentHasher.Verify));

        // Assert
        verifyMethod.Should().NotBeNull();
        verifyMethod!.ReturnType.Should().Be(typeof(bool));
    }
}

namespace Acode.Application.Tests.Fallback;

using Acode.Application.Fallback;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for <see cref="FallbackResult"/>.
/// </summary>
public sealed class FallbackResultTests
{
    /// <summary>
    /// Test creating a successful fallback result.
    /// </summary>
    [Fact]
    public void Succeeded_Should_Create_Successful_Result()
    {
        // Arrange
        var modelId = "llama3.2:7b";
        var reason = "Primary model unavailable";
        var triedModels = new List<string> { "llama3.2:70b", "llama3.2:7b" };

        // Act
        var result = FallbackResult.Succeeded(modelId, reason, triedModels);

        // Assert
        result.Success.Should().BeTrue();
        result.ModelId.Should().Be(modelId);
        result.Reason.Should().Be(reason);
        result.TriedModels.Should().BeEquivalentTo(triedModels);
    }

    /// <summary>
    /// Test creating a failed fallback result.
    /// </summary>
    [Fact]
    public void Failed_Should_Create_Failed_Result()
    {
        // Arrange
        var reason = "All fallbacks exhausted";
        var triedModels = new List<string> { "llama3.2:70b", "mistral:7b" };
        var failureReasons = new Dictionary<string, string>
        {
            ["llama3.2:70b"] = "circuit open",
            ["mistral:7b"] = "unavailable",
        };

        // Act
        var result = FallbackResult.Failed(reason, triedModels, failureReasons);

        // Assert
        result.Success.Should().BeFalse();
        result.ModelId.Should().BeNull();
        result.Reason.Should().Be(reason);
        result.TriedModels.Should().BeEquivalentTo(triedModels);
        result.FailureReasons.Should().BeEquivalentTo(failureReasons);
    }

    /// <summary>
    /// Test failed result without failure reasons.
    /// </summary>
    [Fact]
    public void Failed_Should_Allow_Null_FailureReasons()
    {
        // Arrange
        var reason = "No fallback configured";
        var triedModels = new List<string>();

        // Act
        var result = FallbackResult.Failed(reason, triedModels);

        // Assert
        result.Success.Should().BeFalse();
        result.FailureReasons.Should().BeNull();
    }
}

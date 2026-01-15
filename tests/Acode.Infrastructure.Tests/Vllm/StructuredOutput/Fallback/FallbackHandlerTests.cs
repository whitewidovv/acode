namespace Acode.Infrastructure.Tests.Vllm.StructuredOutput.Fallback;

using Acode.Infrastructure.Vllm.StructuredOutput.Fallback;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for FallbackHandler.
/// </summary>
public class FallbackHandlerTests
{
    private readonly OutputValidator _validator = new();

    [Fact]
    public void Handle_WithValidContext_ReturnsResult()
    {
        // Arrange
        var handler = new FallbackHandler(this._validator);
        var context = new FallbackContext
        {
            ModelId = "llama2",
            FallbackMode = "Managed",
            FallbackAttempts = 0,
            MaxFallbackAttempts = 3,
        };
        var schema = @"{""type"":""object""}";

        // Act
        var result = handler.Handle(context, schema);

        // Assert
        result.Should().NotBeNull();
        context.FallbackAttempts.Should().Be(1);
    }

    [Fact]
    public void Handle_WithMaxAttemptsExceeded_ReturnsMaxAttemptsExceeded()
    {
        // Arrange
        var handler = new FallbackHandler(this._validator);
        var context = new FallbackContext
        {
            ModelId = "llama2",
            FallbackAttempts = 3,
            MaxFallbackAttempts = 3,
        };
        var schema = @"{""type"":""object""}";

        // Act
        var result = handler.Handle(context, schema);

        // Assert
        result.Success.Should().BeFalse();
        result.Reason.Should().Be(FallbackReason.MaxAttemptsExceeded);
        result.Message.Should().Contain("Maximum fallback attempts");
    }

    [Fact]
    public void Handle_WithNullContext_Throws()
    {
        // Arrange
        var handler = new FallbackHandler(this._validator);
        var schema = @"{""type"":""object""}";

        // Act
        var action = () => handler.Handle(null!, schema);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Handle_WithEmptySchema_Throws()
    {
        // Arrange
        var handler = new FallbackHandler(this._validator);
        var context = new FallbackContext { ModelId = "llama2" };

        // Act
        var action = () => handler.Handle(context, string.Empty);

        // Assert
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Handle_WithValidOutput_IncrementsAttempts()
    {
        // Arrange
        var handler = new FallbackHandler(this._validator);
        var context = new FallbackContext
        {
            ModelId = "llama2",
            FallbackAttempts = 0,
            MaxFallbackAttempts = 3,
            InvalidOutput = @"{""valid"":""json""}",
        };
        var schema = @"{""type"":""object""}";

        // Act
        var result = handler.Handle(context, schema);

        // Assert
        context.FallbackAttempts.Should().Be(1);
    }

    [Fact]
    public void Handle_WithRegenerationRequired_ReturnsRetryNeeded()
    {
        // Arrange
        var handler = new FallbackHandler(this._validator);
        var context = new FallbackContext
        {
            ModelId = "llama2",
            FallbackAttempts = 0,
            MaxFallbackAttempts = 3,
            ShouldRegenerateOutput = true,
        };
        var schema = @"{""type"":""object""}";

        // Act
        var result = handler.Handle(context, schema);

        // Assert
        result.Reason.Should().Be(FallbackReason.RegenerationRequired);
        result.ShouldRetry.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithValidOutput_ReturnsTrue()
    {
        // Arrange
        var handler = new FallbackHandler(this._validator);
        var output = @"{""name"":""John""}";
        var schema = @"{""type"":""object""}";

        // Act
        var result = handler.Validate(output, schema);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithInvalidJson_ReturnsFalse()
    {
        // Arrange
        var handler = new FallbackHandler(this._validator);
        var output = @"{invalid}";
        var schema = @"{""type"":""object""}";

        // Act
        var result = handler.Validate(output, schema);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithEmptyOutput_ReturnsFalse()
    {
        // Arrange
        var handler = new FallbackHandler(this._validator);
        var schema = @"{""type"":""object""}";

        // Act
        var result = handler.Validate(string.Empty, schema);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithNullValidator_Throws()
    {
        // Act
        var action = () => new FallbackHandler(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }
}

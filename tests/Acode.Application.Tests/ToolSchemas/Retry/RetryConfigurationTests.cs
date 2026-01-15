namespace Acode.Application.Tests.ToolSchemas.Retry;

using Acode.Application.ToolSchemas.Retry;
using FluentAssertions;

/// <summary>
/// Unit tests for RetryConfiguration class.
/// </summary>
/// <remarks>
/// Spec Reference: Implementation Prompt lines 3438-3505.
/// Verifies default values match specification.
/// </remarks>
public sealed class RetryConfigurationTests
{
    [Fact]
    public void Should_Have_Default_MaxAttempts_Of_3()
    {
        var config = new RetryConfiguration();
        config.MaxAttempts.Should().Be(3);
    }

    [Fact]
    public void Should_Have_Default_MaxMessageLength_Of_2000()
    {
        var config = new RetryConfiguration();
        config.MaxMessageLength.Should().Be(2000);
    }

    [Fact]
    public void Should_Have_Default_MaxErrorsShown_Of_10()
    {
        var config = new RetryConfiguration();
        config.MaxErrorsShown.Should().Be(10);
    }

    [Fact]
    public void Should_Have_Default_MaxValuePreview_Of_100()
    {
        var config = new RetryConfiguration();
        config.MaxValuePreview.Should().Be(100);
    }

    [Fact]
    public void Should_Have_Default_IncludeHints_True()
    {
        var config = new RetryConfiguration();
        config.IncludeHints.Should().BeTrue();
    }

    [Fact]
    public void Should_Have_Default_IncludeActualValues_True()
    {
        var config = new RetryConfiguration();
        config.IncludeActualValues.Should().BeTrue();
    }

    [Fact]
    public void Should_Have_Default_TrackHistory_True()
    {
        var config = new RetryConfiguration();
        config.TrackHistory.Should().BeTrue();
    }

    [Fact]
    public void Should_Have_Default_RedactSecrets_True()
    {
        var config = new RetryConfiguration();
        config.RedactSecrets.Should().BeTrue();
    }

    [Fact]
    public void Should_Have_Default_RelativizePaths_True()
    {
        var config = new RetryConfiguration();
        config.RelativizePaths.Should().BeTrue();
    }

    [Fact]
    public void Should_Be_Sealed_Class()
    {
        typeof(RetryConfiguration).IsSealed.Should().BeTrue();
    }

    [Fact]
    public void Should_Allow_Setting_All_Properties()
    {
        var config = new RetryConfiguration
        {
            MaxAttempts = 5,
            MaxMessageLength = 3000,
            MaxErrorsShown = 15,
            MaxValuePreview = 200,
            IncludeHints = false,
            IncludeActualValues = false,
            TrackHistory = false,
            RedactSecrets = false,
            RelativizePaths = false,
        };

        config.MaxAttempts.Should().Be(5);
        config.MaxMessageLength.Should().Be(3000);
        config.MaxErrorsShown.Should().Be(15);
        config.MaxValuePreview.Should().Be(200);
        config.IncludeHints.Should().BeFalse();
        config.IncludeActualValues.Should().BeFalse();
        config.TrackHistory.Should().BeFalse();
        config.RedactSecrets.Should().BeFalse();
        config.RelativizePaths.Should().BeFalse();
    }
}

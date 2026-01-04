namespace Acode.Infrastructure.Tests.Ollama;

using Acode.Infrastructure.Ollama;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for <see cref="OllamaConfiguration"/>.
/// </summary>
/// <remarks>
/// FR-005-017 to FR-005-025: Configuration validation and defaults.
/// </remarks>
public sealed class OllamaConfigurationTests
{
    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange & Act
        var config = new OllamaConfiguration(
            baseUrl: "http://localhost:11434",
            defaultModel: "llama3.2:latest",
            requestTimeoutSeconds: 120,
            healthCheckTimeoutSeconds: 5,
            maxRetries: 3,
            enableRetry: true);

        // Assert
        config.BaseUrl.Should().Be("http://localhost:11434");
        config.DefaultModel.Should().Be("llama3.2:latest");
        config.RequestTimeoutSeconds.Should().Be(120);
        config.HealthCheckTimeoutSeconds.Should().Be(5);
        config.MaxRetries.Should().Be(3);
        config.EnableRetry.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithDefaults_UsesDefaultValues()
    {
        // Arrange & Act
        var config = new OllamaConfiguration();

        // Assert
        config.BaseUrl.Should().Be("http://localhost:11434");
        config.DefaultModel.Should().Be("llama3.2:latest");
        config.RequestTimeoutSeconds.Should().Be(120);
        config.HealthCheckTimeoutSeconds.Should().Be(5);
        config.MaxRetries.Should().Be(3);
        config.EnableRetry.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyBaseUrl_ThrowsArgumentException(string baseUrl)
    {
        // Arrange & Act
        var act = () => new OllamaConfiguration(baseUrl: baseUrl);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("baseUrl")
            .WithMessage("*must be non-empty*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyDefaultModel_ThrowsArgumentException(string defaultModel)
    {
        // Arrange & Act
        var act = () => new OllamaConfiguration(defaultModel: defaultModel);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("defaultModel")
            .WithMessage("*must be non-empty*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_WithInvalidRequestTimeout_ThrowsArgumentException(int timeout)
    {
        // Arrange & Act
        var act = () => new OllamaConfiguration(requestTimeoutSeconds: timeout);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("requestTimeoutSeconds")
            .WithMessage("*must be positive*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_WithInvalidHealthCheckTimeout_ThrowsArgumentException(int timeout)
    {
        // Arrange & Act
        var act = () => new OllamaConfiguration(healthCheckTimeoutSeconds: timeout);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("healthCheckTimeoutSeconds")
            .WithMessage("*must be positive*");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_WithNegativeMaxRetries_ThrowsArgumentException(int maxRetries)
    {
        // Arrange & Act
        var act = () => new OllamaConfiguration(maxRetries: maxRetries);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("maxRetries")
            .WithMessage("*must be non-negative*");
    }

    [Fact]
    public void Constructor_WithMaxRetriesZero_AllowsNoRetries()
    {
        // Arrange & Act
        var config = new OllamaConfiguration(maxRetries: 0);

        // Assert
        config.MaxRetries.Should().Be(0);
    }

    [Fact]
    public void RequestTimeout_ReturnsCorrectTimeSpan()
    {
        // Arrange
        var config = new OllamaConfiguration(requestTimeoutSeconds: 90);

        // Act
        var timeout = config.RequestTimeout;

        // Assert
        timeout.Should().Be(TimeSpan.FromSeconds(90));
    }

    [Fact]
    public void HealthCheckTimeout_ReturnsCorrectTimeSpan()
    {
        // Arrange
        var config = new OllamaConfiguration(healthCheckTimeoutSeconds: 10);

        // Act
        var timeout = config.HealthCheckTimeout;

        // Assert
        timeout.Should().Be(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void WithProperty_CreatesNewInstanceWithModifiedValue()
    {
        // Arrange
        var original = new OllamaConfiguration();

        // Act
        var modified = original with { DefaultModel = "qwen2.5:latest" };

        // Assert
        modified.DefaultModel.Should().Be("qwen2.5:latest");
        original.DefaultModel.Should().Be("llama3.2:latest"); // Original unchanged
    }
}

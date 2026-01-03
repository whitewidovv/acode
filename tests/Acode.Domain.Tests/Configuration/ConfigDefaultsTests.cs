using Acode.Domain.Configuration;
using FluentAssertions;
using Xunit;

namespace Acode.Domain.Tests.Configuration;

/// <summary>
/// Tests for ConfigDefaults.
/// Verifies default values match schema specification per Task 002.b.
/// </summary>
public class ConfigDefaultsTests
{
    [Fact]
    public void ConfigDefaults_SchemaVersion_ShouldBe_1_0_0()
    {
        // Arrange & Act
        var version = ConfigDefaults.SchemaVersion;

        // Assert
        version.Should().Be("1.0.0", "per FR-002b-94");
    }

    [Fact]
    public void ConfigDefaults_DefaultMode_ShouldBeLocalOnly()
    {
        // Arrange & Act
        var mode = ConfigDefaults.DefaultMode;

        // Assert
        mode.Should().Be("local-only", "per FR-002b-95 and HC-07");
    }

    [Fact]
    public void ConfigDefaults_AllowBurst_ShouldBeTrue()
    {
        // Arrange & Act
        var allow = ConfigDefaults.AllowBurst;

        // Assert
        allow.Should().BeTrue("per FR-002b-96");
    }

    [Fact]
    public void ConfigDefaults_AirgappedLock_ShouldBeFalse()
    {
        // Arrange & Act
        var locked = ConfigDefaults.AirgappedLock;

        // Assert
        locked.Should().BeFalse("per FR-002b-97");
    }

    [Fact]
    public void ConfigDefaults_DefaultProvider_ShouldBeOllama()
    {
        // Arrange & Act
        var provider = ConfigDefaults.DefaultProvider;

        // Assert
        provider.Should().Be("ollama", "per FR-002b-98");
    }

    [Fact]
    public void ConfigDefaults_DefaultModel_ShouldBeCodeLlama7b()
    {
        // Arrange & Act
        var model = ConfigDefaults.DefaultModel;

        // Assert
        model.Should().Be("codellama:7b", "per FR-002b-99");
    }

    [Fact]
    public void ConfigDefaults_DefaultEndpoint_ShouldBeLocalhostOllama()
    {
        // Arrange & Act
        var endpoint = ConfigDefaults.DefaultEndpoint;

        // Assert
        endpoint.Should().Be("http://localhost:11434", "per FR-002b-100");
    }

    [Fact]
    public void ConfigDefaults_DefaultTemperature_ShouldBe_0_7()
    {
        // Arrange & Act
        var temp = ConfigDefaults.DefaultTemperature;

        // Assert
        temp.Should().Be(0.7, "per FR-002b-101");
    }

    [Fact]
    public void ConfigDefaults_DefaultMaxTokens_ShouldBe_4096()
    {
        // Arrange & Act
        var tokens = ConfigDefaults.DefaultMaxTokens;

        // Assert
        tokens.Should().Be(4096, "per FR-002b-102");
    }

    [Fact]
    public void ConfigDefaults_DefaultTimeoutSeconds_ShouldBe_120()
    {
        // Arrange & Act
        var timeout = ConfigDefaults.DefaultTimeoutSeconds;

        // Assert
        timeout.Should().Be(120, "per FR-002b-103");
    }

    [Fact]
    public void ConfigDefaults_DefaultRetryCount_ShouldBe_3()
    {
        // Arrange & Act
        var retry = ConfigDefaults.DefaultRetryCount;

        // Assert
        retry.Should().Be(3, "per FR-002b-104");
    }

    [Fact]
    public void ConfigDefaults_AllDefaults_ShouldMatchSchema()
    {
        // This test verifies all defaults are defined and accessible
        // Arrange & Act & Assert
        ConfigDefaults.SchemaVersion.Should().NotBeNullOrWhiteSpace();
        ConfigDefaults.DefaultMode.Should().NotBeNullOrWhiteSpace();
        ConfigDefaults.DefaultProvider.Should().NotBeNullOrWhiteSpace();
        ConfigDefaults.DefaultModel.Should().NotBeNullOrWhiteSpace();
        ConfigDefaults.DefaultEndpoint.Should().NotBeNullOrWhiteSpace();
        ConfigDefaults.DefaultTemperature.Should().BeInRange(0, 2);
        ConfigDefaults.DefaultMaxTokens.Should().BePositive();
        ConfigDefaults.DefaultTimeoutSeconds.Should().BePositive();
        ConfigDefaults.DefaultRetryCount.Should().BeGreaterOrEqualTo(0);
    }
}

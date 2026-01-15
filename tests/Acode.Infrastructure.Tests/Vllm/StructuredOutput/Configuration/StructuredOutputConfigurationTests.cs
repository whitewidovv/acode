namespace Acode.Infrastructure.Tests.Vllm.StructuredOutput.Configuration;

using Acode.Infrastructure.Vllm.StructuredOutput.Configuration;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for StructuredOutputConfiguration.
/// </summary>
public class StructuredOutputConfigurationTests
{
    [Fact]
    public void IsEnabled_GloballyEnabled_WithUnknownModel_ReturnsTrue()
    {
        // Arrange
        var config = new StructuredOutputConfiguration { Enabled = true };

        // Act
        var enabled = config.IsEnabled("unknown-model");

        // Assert
        enabled.Should().BeTrue("structured output is enabled globally");
    }

    [Fact]
    public void IsEnabled_GloballyDisabled_Returnsfalse()
    {
        // Arrange
        var config = new StructuredOutputConfiguration { Enabled = false };

        // Act
        var enabled = config.IsEnabled("any-model");

        // Assert
        enabled.Should().BeFalse("structured output is disabled globally");
    }

    [Fact]
    public void IsEnabled_PerModelOverride_DisabledModel_ReturnsFalse()
    {
        // Arrange
        var config = new StructuredOutputConfiguration { Enabled = true };
        config.Models["custom-model"] = new ModelStructuredOutputConfig { Enabled = false };

        // Act
        var enabled = config.IsEnabled("custom-model");

        // Assert
        enabled.Should().BeFalse("per-model override disables structured output");
    }

    [Fact]
    public void IsEnabled_PerModelOverride_EnabledModel_ReturnsTrue()
    {
        // Arrange
        var config = new StructuredOutputConfiguration { Enabled = false };
        config.Models["special-model"] = new ModelStructuredOutputConfig { Enabled = true };

        // Act
        var enabled = config.IsEnabled("special-model");

        // Assert
        enabled.Should().BeTrue("per-model override enables structured output even when globally disabled");
    }

    [Fact]
    public void IsEnabled_EmptyModelId_ReturnsFalse()
    {
        // Arrange
        var config = new StructuredOutputConfiguration { Enabled = true };

        // Act
        var enabled = config.IsEnabled(string.Empty);

        // Assert
        enabled.Should().BeFalse("empty model id should be treated as disabled");
    }

    [Fact]
    public void IsEnabled_NullModelId_ReturnsFalse()
    {
        // Arrange
        var config = new StructuredOutputConfiguration { Enabled = true };

        // Act
        var enabled = config.IsEnabled(null!);

        // Assert
        enabled.Should().BeFalse("null model id should be treated as disabled");
    }

    [Fact]
    public void GetFallbackConfig_GlobalFallbackReturned_WhenNoOverride()
    {
        // Arrange
        var globalFallback = new FallbackConfiguration { MaxRetries = 5 };
        var config = new StructuredOutputConfiguration { Fallback = globalFallback };

        // Act
        var fallbackConfig = config.GetFallbackConfig("any-model");

        // Assert
        fallbackConfig.Should().Be(globalFallback);
        fallbackConfig.MaxRetries.Should().Be(5);
    }

    [Fact]
    public void GetFallbackConfig_ModelOverride_ReturnsModelFallback()
    {
        // Arrange
        var globalFallback = new FallbackConfiguration { MaxRetries = 3 };
        var modelFallback = new FallbackConfiguration { MaxRetries = 10 };
        var config = new StructuredOutputConfiguration { Fallback = globalFallback };
        config.Models["custom-model"] = new ModelStructuredOutputConfig { Fallback = modelFallback };

        // Act
        var fallbackConfig = config.GetFallbackConfig("custom-model");

        // Assert
        fallbackConfig.Should().Be(modelFallback);
        fallbackConfig.MaxRetries.Should().Be(10);
    }

    [Fact]
    public void GetFallbackConfig_EmptyModelId_ReturnsGlobalFallback()
    {
        // Arrange
        var globalFallback = new FallbackConfiguration { MaxRetries = 3 };
        var config = new StructuredOutputConfiguration { Fallback = globalFallback };

        // Act
        var fallbackConfig = config.GetFallbackConfig(string.Empty);

        // Assert
        fallbackConfig.Should().Be(globalFallback);
    }

    [Fact]
    public void GetFallbackConfig_NullModelId_ReturnsGlobalFallback()
    {
        // Arrange
        var globalFallback = new FallbackConfiguration { MaxRetries = 3 };
        var config = new StructuredOutputConfiguration { Fallback = globalFallback };

        // Act
        var fallbackConfig = config.GetFallbackConfig(null!);

        // Assert
        fallbackConfig.Should().Be(globalFallback);
    }

    [Fact]
    public void Validate_ValidConfiguration_ReturnsValid()
    {
        // Arrange
        var config = new StructuredOutputConfiguration
        {
            Enabled = true,
            DefaultMode = "json_schema",
            Fallback = new FallbackConfiguration { MaxRetries = 3 },
            Schema = new SchemaConfiguration { MaxDepth = 10, MaxSizeBytes = 65536 }
        };

        // Act
        var result = config.Validate();

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_MaxRetriesOutOfRange_ReturnsInvalid()
    {
        // Arrange
        var config = new StructuredOutputConfiguration
        {
            Fallback = new FallbackConfiguration { MaxRetries = 15 }
        };

        // Act
        var result = config.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("MaxRetries"));
    }

    [Fact]
    public void Validate_MaxDepthOutOfRange_ReturnsInvalid()
    {
        // Arrange
        var config = new StructuredOutputConfiguration
        {
            Schema = new SchemaConfiguration { MaxDepth = 50 }
        };

        // Act
        var result = config.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("MaxDepth"));
    }

    [Fact]
    public void Validate_InvalidDefaultMode_ReturnsInvalid()
    {
        // Arrange
        var config = new StructuredOutputConfiguration { DefaultMode = "invalid_mode" };

        // Act
        var result = config.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("DefaultMode"));
    }

    [Fact]
    public void Validate_NullDefaultMode_ReturnsInvalid()
    {
        // Arrange
        var config = new StructuredOutputConfiguration { DefaultMode = null! };

        // Act
        var result = config.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("DefaultMode"));
    }
}

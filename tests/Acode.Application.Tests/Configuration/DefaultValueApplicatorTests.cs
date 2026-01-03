using Acode.Application.Configuration;
using Acode.Domain.Configuration;
using FluentAssertions;
using Xunit;

namespace Acode.Application.Tests.Configuration;

/// <summary>
/// Tests for DefaultValueApplicator.
/// Verifies default value application per FR-002b-91 to FR-002b-105.
/// </summary>
public class DefaultValueApplicatorTests
{
    [Fact]
    public void Apply_WithNullInput_ShouldReturnNull()
    {
        // Arrange
        var applicator = new DefaultValueApplicator();

        // Act
        var result = applicator.Apply(null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Apply_WithCompleteConfig_ShouldNotOverrideExplicitValues()
    {
        // Arrange
        var applicator = new DefaultValueApplicator();
        var config = new AcodeConfig
        {
            SchemaVersion = "2.0.0",
            Mode = new ModeConfig
            {
                Default = "burst",
                AllowBurst = false,
                AirgappedLock = true
            },
            Model = new ModelConfig
            {
                Provider = "vllm",
                Name = "deepseek-coder:33b",
                Endpoint = "http://localhost:8000",
                Parameters = new ModelParametersConfig
                {
                    Temperature = 0.2,
                    MaxTokens = 8192
                },
                TimeoutSeconds = 300,
                RetryCount = 5
            }
        };

        // Act
        var result = applicator.Apply(config);

        // Assert - FR-002b-92: Defaults MUST NOT override explicit values
        result.Should().NotBeNull();
        result!.SchemaVersion.Should().Be("2.0.0");
        result.Mode.Should().NotBeNull();
        result.Mode!.Default.Should().Be("burst");
        result.Mode.AllowBurst.Should().BeFalse();
        result.Mode.AirgappedLock.Should().BeTrue();
        result.Model.Should().NotBeNull();
        result.Model!.Provider.Should().Be("vllm");
        result.Model.Name.Should().Be("deepseek-coder:33b");
        result.Model.Endpoint.Should().Be("http://localhost:8000");
        result.Model.Parameters.Should().NotBeNull();
        result.Model.Parameters!.Temperature.Should().Be(0.2);
        result.Model.Parameters.MaxTokens.Should().Be(8192);
        result.Model.TimeoutSeconds.Should().Be(300);
        result.Model.RetryCount.Should().Be(5);
    }

    [Fact]
    public void Apply_WithMinimalConfig_ShouldApplyAllDefaults()
    {
        // Arrange
        var applicator = new DefaultValueApplicator();
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0"
        };

        // Act
        var result = applicator.Apply(config);

        // Assert - FR-002b-94 to FR-002b-104: Default values
        result.Should().NotBeNull();
        result!.SchemaVersion.Should().Be("1.0.0"); // Preserved
        result.Mode.Should().NotBeNull();
        result.Mode!.Default.Should().Be("local-only"); // FR-002b-95
        result.Mode.AllowBurst.Should().BeTrue(); // FR-002b-96
        result.Mode.AirgappedLock.Should().BeFalse(); // FR-002b-97
        result.Model.Should().NotBeNull();
        result.Model!.Provider.Should().Be("ollama"); // FR-002b-98
        result.Model.Name.Should().Be("codellama:7b"); // FR-002b-99
        result.Model.Endpoint.Should().Be("http://localhost:11434"); // FR-002b-100
        result.Model.Parameters.Should().NotBeNull();
        result.Model.Parameters!.Temperature.Should().Be(0.7); // FR-002b-101
        result.Model.Parameters.MaxTokens.Should().Be(4096); // FR-002b-102
        result.Model.TimeoutSeconds.Should().Be(120); // FR-002b-103
        result.Model.RetryCount.Should().Be(3); // FR-002b-104
    }

    [Fact]
    public void Apply_WithPartialModeConfig_ShouldApplyMissingDefaults()
    {
        // Arrange
        var applicator = new DefaultValueApplicator();
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Mode = new ModeConfig
            {
                Default = "local-only"

                // AllowBurst and AirgappedLock will use defaults
            }
        };

        // Act
        var result = applicator.Apply(config);

        // Assert
        result.Should().NotBeNull();
        result!.Mode.Should().NotBeNull();
        result.Mode!.Default.Should().Be("local-only"); // Preserved
        result.Mode.AllowBurst.Should().BeTrue(); // Default applied
        result.Mode.AirgappedLock.Should().BeFalse(); // Default applied
    }

    [Fact]
    public void Apply_WithPartialModelConfig_ShouldApplyMissingDefaults()
    {
        // Arrange
        var applicator = new DefaultValueApplicator();
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Model = new ModelConfig
            {
                Provider = "ollama",
                Name = "custom-model"

                // Endpoint, Parameters, TimeoutSeconds, RetryCount will use defaults
            }
        };

        // Act
        var result = applicator.Apply(config);

        // Assert
        result.Should().NotBeNull();
        result!.Model.Should().NotBeNull();
        result.Model!.Provider.Should().Be("ollama"); // Preserved
        result.Model.Name.Should().Be("custom-model"); // Preserved
        result.Model.Endpoint.Should().Be("http://localhost:11434"); // Default applied
        result.Model.Parameters.Should().NotBeNull();
        result.Model.Parameters!.Temperature.Should().Be(0.7); // Default applied
        result.Model.Parameters.MaxTokens.Should().Be(4096); // Default applied
        result.Model.TimeoutSeconds.Should().Be(120); // Default applied
        result.Model.RetryCount.Should().Be(3); // Default applied
    }

    [Fact]
    public void Apply_WithPartialModelParameters_ShouldApplyMissingDefaults()
    {
        // Arrange
        var applicator = new DefaultValueApplicator();
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0",
            Model = new ModelConfig
            {
                Provider = "ollama",
                Name = "codellama:7b",
                Endpoint = "http://localhost:11434",
                Parameters = new ModelParametersConfig
                {
                    Temperature = 0.5

                    // MaxTokens will use default
                }
            }
        };

        // Act
        var result = applicator.Apply(config);

        // Assert
        result.Should().NotBeNull();
        result!.Model.Should().NotBeNull();
        result.Model!.Parameters.Should().NotBeNull();
        result.Model.Parameters!.Temperature.Should().Be(0.5); // Preserved
        result.Model.Parameters.MaxTokens.Should().Be(4096); // Default applied
    }

    [Fact]
    public void Apply_WithNullSchemaVersion_ShouldApplyDefaultSchemaVersion()
    {
        // Arrange
        var applicator = new DefaultValueApplicator();
        var config = new AcodeConfig
        {
            SchemaVersion = string.Empty
        };

        // Act
        var result = applicator.Apply(config);

        // Assert - FR-002b-94
        result.Should().NotBeNull();
        result!.SchemaVersion.Should().Be("1.0.0");
    }

    [Fact]
    public void Apply_ShouldCreateNewInstance()
    {
        // Arrange
        var applicator = new DefaultValueApplicator();
        var config = new AcodeConfig
        {
            SchemaVersion = "1.0.0"
        };

        // Act
        var result = applicator.Apply(config);

        // Assert - Should not modify original
        result.Should().NotBeSameAs(config);
        config.Mode.Should().BeNull(); // Original unchanged
        result!.Mode.Should().NotBeNull(); // Result has defaults
    }
}

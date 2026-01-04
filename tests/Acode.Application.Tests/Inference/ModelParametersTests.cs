namespace Acode.Application.Tests.Inference;

using System;
using System.Text.Json;
using Acode.Application.Inference;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for ModelParameters record following TDD (RED phase).
/// FR-004-55 to FR-004-65.
/// </summary>
public class ModelParametersTests
{
    [Fact]
    public void ModelParameters_HasModelProperty()
    {
        // FR-004-56, FR-004-57: Model is required model identifier string
        var parameters = new ModelParameters("llama2:7b");

        parameters.Model.Should().Be("llama2:7b");
    }

    [Fact]
    public void ModelParameters_HasTemperatureWithDefault()
    {
        // FR-004-058, FR-004-59: Temperature defaults to 0.7
        var parameters = new ModelParameters("llama2");

        parameters.Temperature.Should().Be(0.7);
    }

    [Fact]
    public void ModelParameters_AllowsCustomTemperature()
    {
        // FR-004-58: Temperature property
        var parameters = new ModelParameters("llama2", temperature: 0.5);

        parameters.Temperature.Should().Be(0.5);
    }

    [Fact]
    public void ModelParameters_ValidatesTemperatureRange()
    {
        // FR-004-60: Temperature must be in range [0.0, 2.0]
        var actLow = () => new ModelParameters("llama2", temperature: -0.1);
        var actHigh = () => new ModelParameters("llama2", temperature: 2.1);

        actLow.Should().Throw<ArgumentOutOfRangeException>();
        actHigh.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ModelParameters_HasMaxTokensProperty()
    {
        // FR-004-61, FR-004-62: MaxTokens nullable (use model default)
        var defaultParams = new ModelParameters("llama2");
        var customParams = new ModelParameters("llama2", maxTokens: 1000);

        defaultParams.MaxTokens.Should().BeNull();
        customParams.MaxTokens.Should().Be(1000);
    }

    [Fact]
    public void ModelParameters_HasTopPWithDefault()
    {
        // FR-004-63, FR-004-64: TopP defaults to 1.0
        var parameters = new ModelParameters("llama2");

        parameters.TopP.Should().Be(1.0);
    }

    [Fact]
    public void ModelParameters_HasStopSequencesProperty()
    {
        // FR-004-65: StopSequences property
        var defaultParams = new ModelParameters("llama2");
        var customParams = new ModelParameters("llama2", stopSequences: new[] { "\n\n", "###" });

        defaultParams.StopSequences.Should().BeNull();
        customParams.StopSequences.Should().BeEquivalentTo(new[] { "\n\n", "###" });
    }

    [Fact]
    public void ModelParameters_HasSeedProperty()
    {
        // FR-004-065+: Seed for reproducible generation
        var defaultParams = new ModelParameters("llama2");
        var seeded = new ModelParameters("llama2", seed: 42);

        defaultParams.Seed.Should().BeNull();
        seeded.Seed.Should().Be(42);
    }

    [Fact]
    public void ModelParameters_SerializesToJson()
    {
        // ModelParameters should serialize to JSON
        var parameters = new ModelParameters("llama2", temperature: 0.8, maxTokens: 500);

        var json = JsonSerializer.Serialize(parameters);

        json.Should().Contain("\"model\":");
        json.Should().Contain("\"temperature\":");
        json.Should().Contain("\"maxTokens\":");
    }

    [Fact]
    public void ModelParameters_ValidatesNonEmptyModel()
    {
        // Model must be non-empty
        var act = () => new ModelParameters(string.Empty);

        act.Should().Throw<ArgumentException>().WithParameterName("Model");
    }

    [Fact]
    public void ModelParameters_ImplementsValueEquality()
    {
        // Records have value equality
        var params1 = new ModelParameters("llama2", temperature: 0.8);
        var params2 = new ModelParameters("llama2", temperature: 0.8);

        params1.Should().Be(params2);
    }
}

namespace Acode.Infrastructure.Tests.Vllm.StructuredOutput;

using System.Text.Json;
using Acode.Infrastructure.Vllm.StructuredOutput;
using Acode.Infrastructure.Vllm.StructuredOutput.Capability;
using Acode.Infrastructure.Vllm.StructuredOutput.Configuration;
using Acode.Infrastructure.Vllm.StructuredOutput.Fallback;
using Acode.Infrastructure.Vllm.StructuredOutput.ResponseFormat;
using Acode.Infrastructure.Vllm.StructuredOutput.Schema;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for StructuredOutputHandler orchestrator.
/// </summary>
public class StructuredOutputHandlerTests
{
    private readonly StructuredOutputConfiguration _config;
    private readonly SchemaValidator _schemaValidator;
    private readonly CapabilityDetector _capabilityDetector;
    private readonly CapabilityCache _capabilityCache;
    private readonly ResponseFormatBuilder _responseFormatBuilder;
    private readonly GuidedDecodingBuilder _guidedDecodingBuilder;
    private readonly OutputValidator _outputValidator;
    private readonly FallbackHandler _fallbackHandler;

    public StructuredOutputHandlerTests()
    {
        this._config = new StructuredOutputConfiguration { Enabled = true };
        this._schemaValidator = new SchemaValidator();
        this._capabilityDetector = new CapabilityDetector();
        this._capabilityCache = new CapabilityCache();
        this._responseFormatBuilder = new ResponseFormatBuilder();
        this._guidedDecodingBuilder = new GuidedDecodingBuilder();
        this._outputValidator = new OutputValidator();
        this._fallbackHandler = new FallbackHandler(this._outputValidator);
    }

    [Fact]
    public async Task EnrichRequestAsync_WithValidSchema_ReturnsSuccess()
    {
        // Arrange
        var handler = this.CreateHandler();
        var modelId = "llama2";
        var schema = JsonDocument.Parse(@"{""type"":""object"",""properties"":{""name"":{""type"":""string""}}}").RootElement;

        // Act
        var result = await handler.EnrichRequestAsync(modelId, schema);

        // Assert
        result.Success.Should().BeTrue();
        result.ResponseFormat.Should().NotBeNull();
        result.ResponseFormat!.Type.Should().Be("json_schema");
        result.Capabilities.Should().NotBeNull();
    }

    [Fact]
    public async Task EnrichRequestAsync_WithEmptyModelId_ReturnsFailed()
    {
        // Arrange
        var handler = this.CreateHandler();
        var schema = JsonDocument.Parse(@"{""type"":""object""}").RootElement;

        // Act
        var result = await handler.EnrichRequestAsync(string.Empty, schema);

        // Assert
        result.Success.Should().BeFalse();
        result.FailureReasonCode.Should().Be(ValidationFailureReason.Disabled);
    }

    [Fact]
    public async Task EnrichRequestAsync_WithDisabledStructuredOutput_ReturnsFailed()
    {
        // Arrange
        this._config.Enabled = false;
        var handler = this.CreateHandler();
        var modelId = "llama2";
        var schema = JsonDocument.Parse(@"{""type"":""object""}").RootElement;

        // Act
        var result = await handler.EnrichRequestAsync(modelId, schema);

        // Assert
        result.Success.Should().BeFalse();
        result.FailureReasonCode.Should().Be(ValidationFailureReason.Disabled);
    }

    [Fact]
    public async Task EnrichRequestAsync_WithOversizedSchema_ReturnsFailed()
    {
        // Arrange
        var handler = this.CreateHandler();
        var modelId = "llama2";

        // Build a schema that exceeds the 64KB size limit by using many properties
        var propertiesList = new System.Text.StringBuilder();
        for (int i = 0; i < 2000; i++)
        {
            if (i > 0)
            {
                propertiesList.Append(",");
            }

            propertiesList.Append("\"prop").Append(i).Append("\":{\"type\":\"string\",\"description\":\"This is a very long description to help fill space in the schema\"}");
        }

        var schemaJson = "{\"type\":\"object\",\"properties\":{" + propertiesList.ToString() + "}}";
        var schema = JsonDocument.Parse(schemaJson).RootElement;

        // Act
        var result = await handler.EnrichRequestAsync(modelId, schema);

        // Assert
        result.Success.Should().BeFalse();
        result.FailureReasonCode.Should().Be(ValidationFailureReason.InvalidSchema);
    }

    [Fact]
    public async Task EnrichRequestAsync_WithCapabilityCache_UsesCachedResult()
    {
        // Arrange
        var handler = this.CreateHandler();
        var modelId = "llama2";
        var schema = JsonDocument.Parse(@"{""type"":""object""}").RootElement;

        // Act - First call (cache miss)
        var result1 = await handler.EnrichRequestAsync(modelId, schema);
        var cacheSize1 = this._capabilityCache.GetCacheSize();

        // Act - Second call (cache hit)
        var result2 = await handler.EnrichRequestAsync(modelId, schema);
        var cacheSize2 = this._capabilityCache.GetCacheSize();

        // Assert
        result1.Success.Should().BeTrue();
        result2.Success.Should().BeTrue();
        cacheSize1.Should().Be(1);
        cacheSize2.Should().Be(1); // Cache size unchanged
    }

    [Fact]
    public void HandleValidationFailure_WithInvalidOutput_ReturnsExtractionResult()
    {
        // Arrange
        var handler = this.CreateHandler();
        var modelId = "llama2";
        var invalidOutput = @"The result is: {""name"":""John""} but there was text before it";
        var schema = @"{""type"":""object""}";

        // Act
        var result = handler.HandleValidationFailure(modelId, invalidOutput, schema);

        // Assert
        result.Should().NotBeNull();
        result.Message.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void HandleValidationFailure_WithEmptyOutput_ReturnsFailed()
    {
        // Arrange
        var handler = this.CreateHandler();
        var modelId = "llama2";
        var schema = @"{""type"":""object""}";

        // Act
        var result = handler.HandleValidationFailure(modelId, string.Empty, schema);

        // Assert
        result.Success.Should().BeFalse();
        result.Reason.Should().Be(FallbackReason.Unrecoverable);
    }

    [Fact]
    public void ValidateOutput_WithValidJson_ReturnsTrue()
    {
        // Arrange
        var handler = this.CreateHandler();
        var output = @"{""name"":""John""}";
        var schema = @"{""type"":""object""}";

        // Act
        var result = handler.ValidateOutput(output, schema);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateOutput_WithInvalidJson_ReturnsFalse()
    {
        // Arrange
        var handler = this.CreateHandler();
        var output = @"{invalid}";
        var schema = @"{""type"":""object""}";

        // Act
        var result = handler.ValidateOutput(output, schema);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateOutput_WithEmptyOutput_ReturnsFalse()
    {
        // Arrange
        var handler = this.CreateHandler();
        var schema = @"{""type"":""object""}";

        // Act
        var result = handler.ValidateOutput(string.Empty, schema);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithNullConfig_Throws()
    {
        // Act
        var action = () => new StructuredOutputHandler(
            null!,
            this._schemaValidator,
            this._capabilityDetector,
            this._capabilityCache,
            this._responseFormatBuilder,
            this._guidedDecodingBuilder,
            this._fallbackHandler);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullValidator_Throws()
    {
        // Act
        var action = () => new StructuredOutputHandler(
            this._config,
            null!,
            this._capabilityDetector,
            this._capabilityCache,
            this._responseFormatBuilder,
            this._guidedDecodingBuilder,
            this._fallbackHandler);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task EnrichRequestAsync_WithEnumSchema_SelectsChoiceGuidedParameter()
    {
        // Arrange
        var handler = this.CreateHandler();
        var modelId = "llama3";
        var schema = JsonDocument.Parse(@"{""type"":""string"",""enum"":[""active"",""inactive"",""pending""]}").RootElement;

        // Act
        var result = await handler.EnrichRequestAsync(modelId, schema);

        // Assert
        result.Success.Should().BeTrue();
        result.GuidedParameter.Should().BeOfType<GuidedChoiceParameter>();
    }

    [Fact]
    public async Task EnrichRequestAsync_WithPatternSchema_SelectsRegexGuidedParameter()
    {
        // Arrange
        var handler = this.CreateHandler();
        var modelId = "llama3";
        var schema = JsonDocument.Parse(@"{""type"":""string"",""pattern"":""^[A-Z0-9]+$""}").RootElement;

        // Act
        var result = await handler.EnrichRequestAsync(modelId, schema);

        // Assert
        result.Success.Should().BeTrue();
        result.GuidedParameter.Should().BeOfType<GuidedRegexParameter>();
    }

    [Fact]
    public void TestEnrichmentResultSuccess()
    {
        // Arrange
        var responseFormat = new VllmResponseFormat { Type = "json_schema" };
        var guidedParam = new GuidedJsonParameter { Type = "json_schema" };
        var capabilities = new ModelCapabilities { ModelId = "test" };

        // Act
        var result = EnrichmentResult.CreateSuccess(responseFormat, guidedParam, capabilities);

        // Assert
        result.Success.Should().BeTrue();
        result.ResponseFormat.Should().Be(responseFormat);
        result.GuidedParameter.Should().Be(guidedParam);
        result.Capabilities.Should().Be(capabilities);
    }

    [Fact]
    public void TestEnrichmentResultDisabled()
    {
        // Act
        var result = EnrichmentResult.CreateDisabled("Test reason");

        // Assert
        result.Success.Should().BeFalse();
        result.FailureReason.Should().Be("Test reason");
        result.FailureReasonCode.Should().Be(ValidationFailureReason.Disabled);
    }

    [Fact]
    public void TestEnrichmentResultFailed()
    {
        // Act
        var result = EnrichmentResult.CreateFailed("Test error", ValidationFailureReason.InvalidSchema);

        // Assert
        result.Success.Should().BeFalse();
        result.FailureReason.Should().Be("Test error");
        result.FailureReasonCode.Should().Be(ValidationFailureReason.InvalidSchema);
    }

    private StructuredOutputHandler CreateHandler()
    {
        return new StructuredOutputHandler(
            this._config,
            this._schemaValidator,
            this._capabilityDetector,
            this._capabilityCache,
            this._responseFormatBuilder,
            this._guidedDecodingBuilder,
            this._fallbackHandler);
    }
}

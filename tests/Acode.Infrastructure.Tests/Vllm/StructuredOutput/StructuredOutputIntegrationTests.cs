namespace Acode.Infrastructure.Tests.Vllm.StructuredOutput;

using System.Text.Json;
using Acode.Application.Inference;
using Acode.Application.Tools;
using Acode.Domain.Models.Inference;
using Acode.Infrastructure.Vllm.StructuredOutput;
using Acode.Infrastructure.Vllm.StructuredOutput.Capability;
using Acode.Infrastructure.Vllm.StructuredOutput.Configuration;
using Acode.Infrastructure.Vllm.StructuredOutput.Fallback;
using Acode.Infrastructure.Vllm.StructuredOutput.ResponseFormat;
using Acode.Infrastructure.Vllm.StructuredOutput.Schema;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

/// <summary>
/// Phase 9: End-to-end integration tests for structured output with ChatRequest and VllmProvider.
/// Tests the complete flow from ChatRequest through ApplyToRequestAsync enrichment.
/// </summary>
public class StructuredOutputIntegrationTests
{
    private readonly StructuredOutputConfiguration _config;
    private readonly StructuredOutputHandler _handler;

    public StructuredOutputIntegrationTests()
    {
        this._config = new StructuredOutputConfiguration { Enabled = true };
        var schemaValidator = new SchemaValidator();
        var capabilityDetector = new CapabilityDetector();
        var capabilityCache = new CapabilityCache();
        var responseFormatBuilder = new ResponseFormatBuilder();
        var guidedDecodingBuilder = new GuidedDecodingBuilder();
        var outputValidator = new OutputValidator();
        var fallbackHandler = new FallbackHandler(outputValidator);

        this._handler = new StructuredOutputHandler(
            this._config,
            schemaValidator,
            capabilityDetector,
            capabilityCache,
            responseFormatBuilder,
            guidedDecodingBuilder,
            fallbackHandler,
            Substitute.For<ILogger<StructuredOutputHandler>>(),
            Substitute.For<IToolSchemaRegistry>());
    }

    [Fact]
    public async Task ApplyToRequestAsync_WithResponseFormatJsonObject_ReturnsSuccess()
    {
        // Arrange
        var chatRequest = new ChatRequest(
            new[]
            {
                ChatMessage.CreateUser("Extract person data"),
            },
            new ModelParameters("llama2"),
            responseFormat: new Acode.Application.Inference.ResponseFormat { Type = "json_object" });

        // Act
        var result = await this._handler.ApplyToRequestAsync(chatRequest, "llama2");

        // Assert
        result.Success.Should().BeTrue();
        result.ResponseFormat.Should().NotBeNull();
        result.ResponseFormat!.Type.Should().Be("json_object");
        result.FailureReason.Should().BeNull();
    }

    [Fact]
    public async Task ApplyToRequestAsync_WithResponseFormatJsonSchema_ReturnsSuccess()
    {
        // Arrange
        var schema = JsonDocument.Parse(
            @"{
                ""type"": ""object"",
                ""properties"": {
                    ""name"": { ""type"": ""string"" },
                    ""age"": { ""type"": ""integer"" }
                },
                ""required"": [""name""]
            }").RootElement;

        var chatRequest = new ChatRequest(
            new[]
            {
                ChatMessage.CreateUser("Extract person data"),
            },
            new ModelParameters("llama2"),
            responseFormat: new Acode.Application.Inference.ResponseFormat
            {
                Type = "json_schema",
                JsonSchema = new JsonSchemaFormat { Name = "Person", Schema = schema },
            });

        // Act
        var result = await this._handler.ApplyToRequestAsync(chatRequest, "llama2");

        // Assert
        result.Success.Should().BeTrue();
        result.ResponseFormat.Should().NotBeNull();
        result.FailureReason.Should().BeNull();
    }

    [Fact]
    public async Task ApplyToRequestAsync_WithToolDefinitions_ReturnsSuccess()
    {
        // Arrange
        var toolParameters = JsonDocument.Parse(
            @"{
                ""type"": ""object"",
                ""properties"": {
                    ""query"": { ""type"": ""string"" }
                }
            }").RootElement;

        var tools = new[]
        {
            new ToolDefinition("search", "Search knowledge base", toolParameters),
        };

        var chatRequest = new ChatRequest(
            new[]
            {
                ChatMessage.CreateUser("Find information"),
            },
            new ModelParameters("llama2"),
            tools: tools);

        // Act
        var result = await this._handler.ApplyToRequestAsync(chatRequest, "llama2");

        // Assert
        result.Success.Should().BeTrue();
        result.GuidedParameter.Should().NotBeNull();
        result.FailureReason.Should().BeNull();
    }

    [Fact]
    public async Task ApplyToRequestAsync_WithDisabledStructuredOutput_ReturnsDisabled()
    {
        // Arrange
        var config = new StructuredOutputConfiguration { Enabled = false };
        var handler = new StructuredOutputHandler(
            config,
            new SchemaValidator(),
            new CapabilityDetector(),
            new CapabilityCache(),
            new ResponseFormatBuilder(),
            new GuidedDecodingBuilder(),
            new FallbackHandler(new OutputValidator()),
            Substitute.For<ILogger<StructuredOutputHandler>>(),
            Substitute.For<IToolSchemaRegistry>());

        var chatRequest = new ChatRequest(
            new[]
            {
                ChatMessage.CreateUser("Extract data"),
            },
            new ModelParameters("llama2"),
            responseFormat: new Acode.Application.Inference.ResponseFormat { Type = "json_object" });

        // Act
        var result = await handler.ApplyToRequestAsync(chatRequest, "llama2");

        // Assert
        result.Success.Should().BeFalse();
        result.FailureReasonCode.Should().Be(ValidationFailureReason.Disabled);
    }

    [Fact]
    public async Task ApplyToRequestAsync_WithNeitherResponseFormatNorTools_ReturnsDisabled()
    {
        // Arrange
        var chatRequest = new ChatRequest(
            new[]
            {
                ChatMessage.CreateUser("Just chat"),
            },
            new ModelParameters("llama2"));

        // Act
        var result = await this._handler.ApplyToRequestAsync(chatRequest, "llama2");

        // Assert
        result.Success.Should().BeFalse();
        result.FailureReasonCode.Should().Be(ValidationFailureReason.Disabled);
    }

    [Fact]
    public async Task ApplyToRequestAsync_ResponseFormatTakesPriority_IgnoresTools()
    {
        // Arrange
        var toolParameters = JsonDocument.Parse(
            @"{
                ""type"": ""object"",
                ""properties"": { ""query"": { ""type"": ""string"" } }
            }").RootElement;

        var tools = new[]
        {
            new ToolDefinition("search", "Search", toolParameters),
        };

        var chatRequest = new ChatRequest(
            new[]
            {
                ChatMessage.CreateUser("Extract data"),
            },
            new ModelParameters("llama2"),
            tools: tools,
            responseFormat: new Acode.Application.Inference.ResponseFormat { Type = "json_object" });

        // Act
        var result = await this._handler.ApplyToRequestAsync(chatRequest, "llama2");

        // Assert
        result.Success.Should().BeTrue();

        // ResponseFormat should be processed, not Tools
        result.ResponseFormat.Should().NotBeNull();
        result.ResponseFormat!.Type.Should().Be("json_object");
    }

    [Fact]
    public async Task ApplyToRequestAsync_WithMultipleTools_CollectsAllSchemas()
    {
        // Arrange
        var searchParams = JsonDocument.Parse(
            @"{
                ""type"": ""object"",
                ""properties"": { ""query"": { ""type"": ""string"" } }
            }").RootElement;

        var calculateParams = JsonDocument.Parse(
            @"{
                ""type"": ""object"",
                ""properties"": { ""expression"": { ""type"": ""string"" } }
            }").RootElement;

        var tools = new[]
        {
            new ToolDefinition("search", "Search knowledge base", searchParams),
            new ToolDefinition("calculate", "Calculate expression", calculateParams),
        };

        var chatRequest = new ChatRequest(
            new[]
            {
                ChatMessage.CreateUser("Help with tasks"),
            },
            new ModelParameters("llama2"),
            tools: tools);

        // Act
        var result = await this._handler.ApplyToRequestAsync(chatRequest, "llama2");

        // Assert
        result.Success.Should().BeTrue();
        result.GuidedParameter.Should().BeOfType<JsonElement[]>();
        var guidedParams = (JsonElement[])result.GuidedParameter!;
        guidedParams.Should().HaveCount(2);
    }

    [Fact]
    public async Task ApplyToRequestAsync_WithChatRequestNullArgument_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => this._handler.ApplyToRequestAsync(null!, "llama2"));
    }

    [Fact]
    public async Task ApplyToRequestAsync_IntegrationFlow_FullChatRequestProcessing()
    {
        // Arrange
        // Simulate complete ChatRequest with ResponseFormat + Tools
        var responseSchema = JsonDocument.Parse(
            @"{
                ""type"": ""object"",
                ""properties"": {
                    ""result"": { ""type"": ""string"" }
                }
            }").RootElement;

        var toolParams = JsonDocument.Parse(
            @"{
                ""type"": ""object"",
                ""properties"": { ""url"": { ""type"": ""string"", ""format"": ""uri"" } }
            }").RootElement;

        var tools = new[]
        {
            new ToolDefinition("fetch", "Fetch from URL", toolParams),
        };

        // ResponseFormat takes priority
        var chatRequest = new ChatRequest(
            new[]
            {
                ChatMessage.CreateSystem("You are a helpful assistant"),
                ChatMessage.CreateUser("Extract and process data"),
            },
            new ModelParameters("llama2", 0.7, 1000),
            tools: tools,
            stream: false,
            responseFormat: new Acode.Application.Inference.ResponseFormat
            {
                Type = "json_schema",
                JsonSchema = new JsonSchemaFormat { Name = "Result", Schema = responseSchema },
            });

        // Act
        var result = await this._handler.ApplyToRequestAsync(chatRequest, "llama2");

        // Assert
        result.Success.Should().BeTrue();
        result.ResponseFormat.Should().NotBeNull();
        result.Capabilities.Should().NotBeNull();
        result.FailureReason.Should().BeNull();
    }

    [Fact]
    public async Task ApplyToRequestAsync_WithUnknownResponseFormatType_ReturnsFailed()
    {
        // Arrange
        var schema = JsonDocument.Parse(@"{""type"":""object""}").RootElement;

        var chatRequest = new ChatRequest(
            new[]
            {
                ChatMessage.CreateUser("Extract data"),
            },
            new ModelParameters("llama2"),
            responseFormat: new Acode.Application.Inference.ResponseFormat
            {
                Type = "unknown_format",
                JsonSchema = new JsonSchemaFormat { Name = "Test", Schema = schema },
            });

        // Act
        var result = await this._handler.ApplyToRequestAsync(chatRequest, "llama2");

        // Assert
        result.Success.Should().BeFalse();
        result.FailureReasonCode.Should().Be(ValidationFailureReason.InvalidSchema);
    }
}

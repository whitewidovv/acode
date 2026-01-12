namespace Acode.Domain.Tests.Models.Inference;

using System;
using System.Collections.Generic;
using Acode.Domain.Models.Inference;
using FluentAssertions;

/// <summary>
/// Tests for ResponseBuilder class.
/// FR-004b-078 to FR-004b-088: Fluent API for building responses.
/// </summary>
public sealed class ResponseBuilderTests
{
    [Fact]
    public void Should_Build_Valid_Response()
    {
        // Arrange
        var builder = new ResponseBuilder()
            .WithMessage(ChatMessage.CreateAssistant("Hello"))
            .WithFinishReason(FinishReason.Stop)
            .WithUsage(new UsageInfo(10, 5))
            .WithMetadata(this.CreateMetadata())
            .WithModel("llama3.2:8b");

        // Act
        var response = builder.Build();

        // Assert
        response.Should().NotBeNull();
        response.Message.Content.Should().Be("Hello");
        response.FinishReason.Should().Be(FinishReason.Stop);
        response.Model.Should().Be("llama3.2:8b");
    }

    [Fact]
    public void Should_AutoGenerate_Id()
    {
        // Arrange
        var builder = new ResponseBuilder()
            .WithMessage(ChatMessage.CreateAssistant("test"))
            .WithFinishReason(FinishReason.Stop)
            .WithUsage(UsageInfo.Empty)
            .WithMetadata(this.CreateMetadata())
            .WithModel("test-model");

        // Act
        var response = builder.Build();

        // Assert
        response.Id.Should().NotBeNullOrEmpty();
        Guid.TryParse(response.Id, out _).Should().BeTrue();
    }

    [Fact]
    public void Should_Use_Provided_Id()
    {
        // Arrange
        var customId = "custom-id-12345";
        var builder = new ResponseBuilder()
            .WithId(customId)
            .WithMessage(ChatMessage.CreateAssistant("test"))
            .WithFinishReason(FinishReason.Stop)
            .WithUsage(UsageInfo.Empty)
            .WithMetadata(this.CreateMetadata())
            .WithModel("test-model");

        // Act
        var response = builder.Build();

        // Assert
        response.Id.Should().Be(customId);
    }

    [Fact]
    public void Should_AutoSet_Created()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        var builder = new ResponseBuilder()
            .WithMessage(ChatMessage.CreateAssistant("test"))
            .WithFinishReason(FinishReason.Stop)
            .WithUsage(UsageInfo.Empty)
            .WithMetadata(this.CreateMetadata())
            .WithModel("test-model");

        // Act
        var response = builder.Build();
        var after = DateTimeOffset.UtcNow;

        // Assert
        response.Created.Should().BeOnOrAfter(before);
        response.Created.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void Should_Validate_Required_Message()
    {
        // Arrange
        var builder = new ResponseBuilder()
            .WithFinishReason(FinishReason.Stop)
            .WithUsage(UsageInfo.Empty)
            .WithMetadata(this.CreateMetadata())
            .WithModel("test-model");

        // Act
        var action = () => builder.Build();

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*message*required*");
    }

    [Fact]
    public void Should_Validate_Required_Usage()
    {
        // Arrange
        var builder = new ResponseBuilder()
            .WithMessage(ChatMessage.CreateAssistant("test"))
            .WithFinishReason(FinishReason.Stop)
            .WithMetadata(this.CreateMetadata())
            .WithModel("test-model");

        // Act
        var action = () => builder.Build();

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*usage*required*");
    }

    [Fact]
    public void Should_Validate_Required_Metadata()
    {
        // Arrange
        var builder = new ResponseBuilder()
            .WithMessage(ChatMessage.CreateAssistant("test"))
            .WithFinishReason(FinishReason.Stop)
            .WithUsage(UsageInfo.Empty)
            .WithModel("test-model");

        // Act
        var action = () => builder.Build();

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*metadata*required*");
    }

    [Fact]
    public void Should_Validate_Required_Model()
    {
        // Arrange
        var builder = new ResponseBuilder()
            .WithMessage(ChatMessage.CreateAssistant("test"))
            .WithFinishReason(FinishReason.Stop)
            .WithUsage(UsageInfo.Empty)
            .WithMetadata(this.CreateMetadata());

        // Act
        var action = () => builder.Build();

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*model*required*");
    }

    [Fact]
    public void Should_Support_Fluent_API()
    {
        // Arrange & Act
        var response = new ResponseBuilder()
            .WithId("test-id")
            .WithMessage(ChatMessage.CreateAssistant("Hello"))
            .WithFinishReason(FinishReason.Stop)
            .WithUsage(new UsageInfo(100, 50))
            .WithMetadata(this.CreateMetadata())
            .WithModel("llama3.2:8b")
            .WithRefusal(null)
            .Build();

        // Assert
        response.Id.Should().Be("test-id");
        response.Model.Should().Be("llama3.2:8b");
    }

    [Fact]
    public void Should_Support_Refusal()
    {
        // Arrange & Act
        var response = new ResponseBuilder()
            .WithMessage(ChatMessage.CreateAssistant("I cannot help with that."))
            .WithFinishReason(FinishReason.Stop)
            .WithUsage(UsageInfo.Empty)
            .WithMetadata(this.CreateMetadata())
            .WithModel("test-model")
            .WithRefusal("Request violates usage policy")
            .Build();

        // Assert
        response.Refusal.Should().Be("Request violates usage policy");
    }

    [Fact]
    public void Should_Support_ContentFilterResults()
    {
        // Arrange
        var filterResults = new List<ContentFilterResult>
        {
            new ContentFilterResult
            {
                Category = FilterCategory.Violence,
                Severity = FilterSeverity.Low,
                Filtered = false,
            },
        };

        // Act
        var response = new ResponseBuilder()
            .WithMessage(ChatMessage.CreateAssistant("test"))
            .WithFinishReason(FinishReason.Stop)
            .WithUsage(UsageInfo.Empty)
            .WithMetadata(this.CreateMetadata())
            .WithModel("test-model")
            .WithContentFilterResults(filterResults)
            .Build();

        // Assert
        response.ContentFilterResults.Should().HaveCount(1);
    }

    [Fact]
    public void Should_Allow_Reuse_After_Build()
    {
        // Arrange
        var builder = new ResponseBuilder()
            .WithMessage(ChatMessage.CreateAssistant("Response 1"))
            .WithFinishReason(FinishReason.Stop)
            .WithUsage(UsageInfo.Empty)
            .WithMetadata(this.CreateMetadata())
            .WithModel("test-model");

        // Act
        var response1 = builder.Build();

        builder.WithMessage(ChatMessage.CreateAssistant("Response 2"));
        var response2 = builder.Build();

        // Assert
        response1.Message.Content.Should().Be("Response 1");
        response2.Message.Content.Should().Be("Response 2");
        response1.Id.Should().NotBe(response2.Id);
    }

    private ResponseMetadata CreateMetadata()
    {
        return new ResponseMetadata(
            "ollama",
            "llama3.2:8b",
            TimeSpan.FromSeconds(2));
    }
}

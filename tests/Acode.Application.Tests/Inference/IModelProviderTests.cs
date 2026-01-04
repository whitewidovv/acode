namespace Acode.Application.Tests.Inference;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Inference;
using Acode.Domain.Models.Inference;
using FluentAssertions;
using NSubstitute;
using Xunit;

/// <summary>
/// Tests for IModelProvider interface contract following TDD (RED phase).
/// FR-004-81 to FR-004-90.
/// </summary>
#pragma warning disable CA2007 // ConfigureAwait not needed in test methods
public class IModelProviderTests
{
    [Fact]
    public void IModelProvider_HasProviderNameProperty()
    {
        // FR-004-81: IModelProvider MUST have ProviderName property (string)
        var provider = Substitute.For<IModelProvider>();
        provider.ProviderName.Returns("TestProvider");

        provider.ProviderName.Should().Be("TestProvider");
    }

    [Fact]
    public void IModelProvider_HasCapabilitiesProperty()
    {
        // FR-004-82: IModelProvider MUST have Capabilities property
        var provider = Substitute.For<IModelProvider>();
        var capabilities = new ProviderCapabilities(supportsStreaming: true);
        provider.Capabilities.Returns(capabilities);

        provider.Capabilities.Should().Be(capabilities);
    }

    [Fact]
    public async Task IModelProvider_HasChatAsyncMethod()
    {
        // FR-004-83: IModelProvider MUST have ChatAsync method
        var provider = Substitute.For<IModelProvider>();
        var request = new ChatRequest(new[] { ChatMessage.CreateUser("Hello") });
        var response = new ChatResponse(
            Id: "test-1",
            Message: ChatMessage.CreateAssistant("Hi"),
            FinishReason: FinishReason.Stop,
            Usage: UsageInfo.Empty,
            Metadata: new ResponseMetadata("test", "test", TimeSpan.Zero),
            Created: DateTimeOffset.UtcNow,
            Model: "test");

        provider.ChatAsync(request, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        var result = await provider.ChatAsync(request, CancellationToken.None);

        result.Should().Be(response);
    }

    [Fact]
    public async Task IModelProvider_ChatAsyncAcceptsCancellationToken()
    {
        // FR-004-84: ChatAsync MUST accept CancellationToken
        var provider = Substitute.For<IModelProvider>();
        var request = new ChatRequest(new[] { ChatMessage.CreateUser("Hello") });
        var cts = new CancellationTokenSource();

        await provider.Received(0).ChatAsync(request, Arg.Any<CancellationToken>());

        var task = provider.ChatAsync(request, cts.Token);

        await provider.Received(1).ChatAsync(request, cts.Token);
    }

    [Fact]
    public async Task IModelProvider_HasStreamChatAsyncMethod()
    {
        // FR-004-85, FR-004-86: IModelProvider MUST have StreamChatAsync method returning IAsyncEnumerable
        var provider = Substitute.For<IModelProvider>();
        var request = new ChatRequest(new[] { ChatMessage.CreateUser("Hello") }, stream: true);

        async IAsyncEnumerable<ResponseDelta> StreamDeltas()
        {
            await Task.CompletedTask;
            yield return new ResponseDelta(0, "Hello");
            yield return new ResponseDelta(0, " world", finishReason: FinishReason.Stop);
        }

        provider.StreamChatAsync(request, Arg.Any<CancellationToken>())
            .Returns(StreamDeltas());

        var deltas = new List<ResponseDelta>();
        await foreach (var delta in provider.StreamChatAsync(request, CancellationToken.None))
        {
            deltas.Add(delta);
        }

        deltas.Should().HaveCount(2);
        deltas[0].ContentDelta.Should().Be("Hello");
        deltas[1].ContentDelta.Should().Be(" world");
    }

    [Fact]
    public async Task IModelProvider_StreamChatAsyncAcceptsCancellationToken()
    {
        // FR-004-87: StreamChatAsync MUST accept CancellationToken
        var provider = Substitute.For<IModelProvider>();
        var request = new ChatRequest(new[] { ChatMessage.CreateUser("Hello") }, stream: true);
        var cts = new CancellationTokenSource();

        async IAsyncEnumerable<ResponseDelta> EmptyStream()
        {
            await Task.CompletedTask;
            yield break;
        }

        provider.StreamChatAsync(request, Arg.Any<CancellationToken>())
            .Returns(EmptyStream());

        await foreach (var delta in provider.StreamChatAsync(request, cts.Token))
        {
            // No-op
        }

        provider.Received(1).StreamChatAsync(request, cts.Token);
    }

    [Fact]
    public async Task IModelProvider_HasIsHealthyAsyncMethod()
    {
        // FR-004-88: IModelProvider MUST have IsHealthyAsync method
        var provider = Substitute.For<IModelProvider>();
        provider.IsHealthyAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

        var result = await provider.IsHealthyAsync(CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IModelProvider_IsHealthyAsyncAcceptsCancellationToken()
    {
        // FR-004-89: IsHealthyAsync MUST accept CancellationToken
        var provider = Substitute.For<IModelProvider>();
        var cts = new CancellationTokenSource();
        provider.IsHealthyAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

        await provider.IsHealthyAsync(cts.Token);

        await provider.Received(1).IsHealthyAsync(cts.Token);
    }

    [Fact]
    public void IModelProvider_HasGetSupportedModelsMethod()
    {
        // FR-004-90: IModelProvider MUST have GetSupportedModels method
        var provider = Substitute.For<IModelProvider>();
        var models = new[] { "model1", "model2", "model3" };
        provider.GetSupportedModels().Returns(models);

        var result = provider.GetSupportedModels();

        result.Should().BeEquivalentTo(models);
    }
}

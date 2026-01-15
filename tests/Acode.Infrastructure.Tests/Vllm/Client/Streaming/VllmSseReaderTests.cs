using Acode.Infrastructure.Vllm.Client.Streaming;
using FluentAssertions;

namespace Acode.Infrastructure.Tests.Vllm.Client.Streaming;

public class VllmSseReaderTests
{
    [Fact]
    public async Task Should_Parse_Data_Lines()
    {
        // Arrange (FR-041, AC-041): MUST parse lines with "data: " prefix
        var sseData = "data: {\"test\":\"value\"}\n\n";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(sseData));

        var reader = new VllmSseReader();

        // Act
        var events = new List<string>();
#pragma warning disable CA2007
        await foreach (var evt in reader.ReadEventsAsync(stream, CancellationToken.None))
        {
            events.Add(evt);
        }
#pragma warning restore CA2007

        // Assert
        events.Should().HaveCount(1);
        events[0].Should().Be("{\"test\":\"value\"}");
    }

    [Fact]
    public async Task Should_Ignore_Comment_Lines()
    {
        // Arrange (FR-042, AC-042): Comments start with ":", should be ignored
        var sseData = ": this is a comment\ndata: {\"test\":\"value\"}\n\n";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(sseData));

        var reader = new VllmSseReader();

        // Act
        var events = new List<string>();
#pragma warning disable CA2007
        await foreach (var evt in reader.ReadEventsAsync(stream, CancellationToken.None))
        {
            events.Add(evt);
        }
#pragma warning restore CA2007

        // Assert
        events.Should().HaveCount(1);
        events[0].Should().Be("{\"test\":\"value\"}");
    }

    [Fact]
    public async Task Should_Handle_Done_Marker()
    {
        // Arrange (FR-052, AC-047): Handle [DONE] marker to end stream
        var sseData = "data: {\"chunk\":1}\n\ndata: [DONE]\n\n";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(sseData));

        var reader = new VllmSseReader();

        // Act
        var events = new List<string>();
#pragma warning disable CA2007
        await foreach (var evt in reader.ReadEventsAsync(stream, CancellationToken.None))
        {
            events.Add(evt);
        }
#pragma warning restore CA2007

        // Assert - [DONE] should be included as last event
        events.Should().HaveCount(2);
        events[0].Should().Be("{\"chunk\":1}");
        events[1].Should().Be("[DONE]");
    }

    [Fact]
    public async Task Should_Handle_Multiple_Events()
    {
        // Arrange (FR-048, AC-044): Handle multiple data events separated by blank lines
        var sseData = "data: {\"choice\":0,\"delta\":{\"content\":\"hello\"}}\n\ndata: {\"choice\":0,\"delta\":{\"content\":\" world\"}}\n\n";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(sseData));

        var reader = new VllmSseReader();

        // Act
        var events = new List<string>();
#pragma warning disable CA2007
        await foreach (var evt in reader.ReadEventsAsync(stream, CancellationToken.None))
        {
            events.Add(evt);
        }
#pragma warning restore CA2007

        // Assert
        events.Should().HaveCount(2);
        events[0].Should().Be("{\"choice\":0,\"delta\":{\"content\":\"hello\"}}");
        events[1].Should().Be("{\"choice\":0,\"delta\":{\"content\":\" world\"}}");
    }

    [Fact]
    public async Task Should_Handle_Empty_Stream()
    {
        // Arrange (FR-043, AC-043): Handle empty or no-data streams gracefully
        var sseData = string.Empty;
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(sseData));

        var reader = new VllmSseReader();

        // Act
        var events = new List<string>();
#pragma warning disable CA2007
        await foreach (var evt in reader.ReadEventsAsync(stream, CancellationToken.None))
        {
            events.Add(evt);
        }
#pragma warning restore CA2007

        // Assert
        events.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_Handle_Comments_Between_Events()
    {
        // Arrange: Comments can appear between events
        var sseData = "data: {\"first\":1}\n\n: comment line\ndata: {\"second\":2}\n\n";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(sseData));

        var reader = new VllmSseReader();

        // Act
        var events = new List<string>();
#pragma warning disable CA2007
        await foreach (var evt in reader.ReadEventsAsync(stream, CancellationToken.None))
        {
            events.Add(evt);
        }
#pragma warning restore CA2007

        // Assert
        events.Should().HaveCount(2);
        events[0].Should().Be("{\"first\":1}");
        events[1].Should().Be("{\"second\":2}");
    }

    [Fact]
    public async Task Should_Support_Cancellation()
    {
        // Arrange: Test cancellation token is respected
        var sseData = "data: {\"test\":\"value\"}\n\n";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(sseData));
        using var cts = new CancellationTokenSource();

        var reader = new VllmSseReader();

        // Act & Assert
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
#pragma warning disable CA2007
            await foreach (var unused in reader.ReadEventsAsync(stream, cts.Token))
            {
                // Would iterate events
            }
#pragma warning restore CA2007
        });
    }
}

using System.Text;
using Acode.Infrastructure.Ollama.Models;
using Acode.Infrastructure.Ollama.Streaming;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.Ollama.Streaming;

/// <summary>
/// Tests for OllamaStreamReader.
/// FR-068 to FR-078 from Task 005.a.
/// </summary>
#pragma warning disable CA2007 // ConfigureAwait not needed in test methods
public sealed class OllamaStreamReaderTests
{
    [Fact]
    public async Task ReadAsync_Should_Parse_NDJSON_Lines()
    {
        // FR-068: Read NDJSON format (one JSON object per line)
        var ndjson = @"{""model"":""llama3.2:8b"",""created_at"":""2024-01-01T12:00:00Z"",""message"":{""role"":""assistant"",""content"":""Hello""},""done"":false}
{""model"":""llama3.2:8b"",""created_at"":""2024-01-01T12:00:01Z"",""message"":{""role"":""assistant"",""content"":"" world""},""done"":false}
{""model"":""llama3.2:8b"",""created_at"":""2024-01-01T12:00:02Z"",""message"":{""role"":""assistant"",""content"":""!""},""done"":true}";

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(ndjson));
        var chunks = new List<OllamaStreamChunk>();

        await foreach (var chunk in OllamaStreamReader.ReadAsync(stream, CancellationToken.None))
        {
            chunks.Add(chunk);
        }

        chunks.Should().HaveCount(3);
        chunks[0].Message?.Content.Should().Be("Hello");
        chunks[1].Message?.Content.Should().Be(" world");
        chunks[2].Message?.Content.Should().Be("!");
        chunks[2].Done.Should().BeTrue();
    }

    [Fact]
    public async Task ReadAsync_Should_Handle_Empty_Lines()
    {
        // FR-073: Handle empty lines gracefully
        var ndjson = @"{""model"":""llama3.2:8b"",""created_at"":""2024-01-01T12:00:00Z"",""message"":{""role"":""assistant"",""content"":""Hello""},""done"":false}

{""model"":""llama3.2:8b"",""created_at"":""2024-01-01T12:00:01Z"",""message"":{""role"":""assistant"",""content"":"" world""},""done"":true}";

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(ndjson));
        var chunks = new List<OllamaStreamChunk>();

        await foreach (var chunk in OllamaStreamReader.ReadAsync(stream, CancellationToken.None))
        {
            chunks.Add(chunk);
        }

        chunks.Should().HaveCount(2);
    }

    [Fact]
    public async Task ReadAsync_Should_Detect_Final_Chunk()
    {
        // FR-072: Detect final chunk (done: true)
        var ndjson = @"{""model"":""llama3.2:8b"",""created_at"":""2024-01-01T12:00:00Z"",""message"":{""role"":""assistant"",""content"":""Test""},""done"":true}";

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(ndjson));
        var chunks = new List<OllamaStreamChunk>();

        await foreach (var chunk in OllamaStreamReader.ReadAsync(stream, CancellationToken.None))
        {
            chunks.Add(chunk);
        }

        chunks.Should().HaveCount(1);
        chunks[0].Done.Should().BeTrue();
    }

    [Fact]
    public async Task ReadAsync_Should_Support_Cancellation()
    {
        // FR-075: Propagate cancellation
        var ndjson = @"{""model"":""llama3.2:8b"",""created_at"":""2024-01-01T12:00:00Z"",""message"":{""role"":""assistant"",""content"":""Test""},""done"":false}
{""model"":""llama3.2:8b"",""created_at"":""2024-01-01T12:00:01Z"",""message"":{""role"":""assistant"",""content"":"" more""},""done"":true}";

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(ndjson));
        var cts = new CancellationTokenSource();
        var chunks = new List<OllamaStreamChunk>();

        var act = async () =>
        {
            await foreach (var chunk in OllamaStreamReader.ReadAsync(stream, cts.Token))
            {
                chunks.Add(chunk);
                cts.Cancel(); // Cancel after first chunk
            }
        };

        await act.Should().ThrowAsync<OperationCanceledException>();
        chunks.Should().HaveCount(1);
    }

    [Fact]
    public async Task ReadAsync_Should_Handle_Chunk_With_Usage_Info()
    {
        // FR-079 to FR-088: Parse usage info from final chunk
        var ndjson = @"{""model"":""llama3.2:8b"",""created_at"":""2024-01-01T12:00:00Z"",""message"":{""role"":""assistant"",""content"":""Test""},""done"":true,""total_duration"":1500000000,""prompt_eval_count"":10,""eval_count"":20}";

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(ndjson));
        var chunks = new List<OllamaStreamChunk>();

        await foreach (var chunk in OllamaStreamReader.ReadAsync(stream, CancellationToken.None))
        {
            chunks.Add(chunk);
        }

        chunks.Should().HaveCount(1);
        chunks[0].TotalDuration.Should().Be(1500000000);
        chunks[0].PromptEvalCount.Should().Be(10);
        chunks[0].EvalCount.Should().Be(20);
    }
}

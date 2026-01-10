// <copyright file="EventSerializerTests.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

namespace Acode.Cli.Tests.JSONL;

using System.Text.Json;
using Acode.Cli.Events;
using Acode.Cli.JSONL;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for <see cref="EventSerializer"/>.
/// </summary>
public class EventSerializerTests
{
    private readonly EventSerializer _sut = new();

    /// <summary>
    /// Verifies that all required base fields are present.
    /// </summary>
    [Fact]
    public void Serialize_ShouldIncludeBaseFields()
    {
        var eventObj = new StatusEvent { Status = "running", Message = "test" };

        var json = _sut.Serialize(eventObj);
        var parsed = JsonDocument.Parse(json);
        var root = parsed.RootElement;

        root.TryGetProperty("type", out _).Should().BeTrue();
        root.TryGetProperty("timestamp", out _).Should().BeTrue();
        root.TryGetProperty("event_id", out _).Should().BeTrue();
        root.TryGetProperty("schema_version", out _).Should().BeTrue();
    }

    /// <summary>
    /// Verifies snake_case naming is used.
    /// </summary>
    [Fact]
    public void Serialize_ShouldUseSnakeCaseNaming()
    {
        var eventObj = new SessionStartEvent { RunId = "run-123", Command = "test" };

        var json = _sut.Serialize(eventObj);

        json.Should().Contain("run_id");
        json.Should().Contain("schema_version");
        json.Should().NotContain("runId");
        json.Should().NotContain("schemaVersion");
    }

    /// <summary>
    /// Verifies null fields are excluded.
    /// </summary>
    [Fact]
    public void Serialize_ShouldExcludeNullFields()
    {
        var eventObj = new ErrorEvent
        {
            Code = "ERR-001",
            Message = "Error",
            Component = "Test",
            StackTrace = null,
        };

        var json = _sut.Serialize(eventObj);

        json.Should().NotContain("stack_trace");
    }

    /// <summary>
    /// Verifies serialization is fast.
    /// </summary>
    [Fact]
    public void Serialize_Performance_ShouldBeUnder1Ms()
    {
        var eventObj = new ProgressEvent
        {
            Step = 1,
            Total = 10,
            Message = "Test",
        };

        _ = _sut.Serialize(eventObj);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < 100; i++)
        {
            _ = _sut.Serialize(eventObj);
        }

        sw.Stop();
        var avgMs = sw.ElapsedMilliseconds / 100.0;
        avgMs.Should().BeLessThan(1.0);
    }

    /// <summary>
    /// Verifies null event throws.
    /// </summary>
    [Fact]
    public void Serialize_NullEvent_ShouldThrow()
    {
        var act = () => _sut.Serialize(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Verifies pretty print adds indentation.
    /// </summary>
    [Fact]
    public void Serialize_PrettyPrint_ShouldIndent()
    {
        var serializer = new EventSerializer(prettyPrint: true);
        var eventObj = new StatusEvent { Status = "test", Message = "msg" };

        var json = serializer.Serialize(eventObj);

        json.Should().Contain("\n");
    }
}

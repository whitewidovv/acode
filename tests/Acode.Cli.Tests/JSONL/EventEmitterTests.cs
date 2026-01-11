// <copyright file="EventEmitterTests.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

namespace Acode.Cli.Tests.JSONL;

using System.IO;
using Acode.Cli.Events;
using Acode.Cli.JSONL;
using FluentAssertions;
using NSubstitute;
using Xunit;

/// <summary>
/// Unit tests for <see cref="EventEmitter"/>.
/// </summary>
public class EventEmitterTests : IDisposable
{
    private readonly StringWriter _output;
    private readonly IEventSerializer _serializer;
    private readonly EventEmitter _sut;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventEmitterTests"/> class.
    /// </summary>
    public EventEmitterTests()
    {
        _output = new StringWriter();
        _serializer = Substitute.For<IEventSerializer>();
        _sut = new EventEmitter(_output, _serializer);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _output.Dispose();
    }

    /// <summary>
    /// Verifies events are written to output.
    /// </summary>
    [Fact]
    public void Emit_ShouldWriteToOutput()
    {
        _serializer.Serialize(Arg.Any<BaseEvent>()).Returns("{\"type\":\"status\"}");

        _sut.Emit(new StatusEvent { Status = "running", Message = "test" });

        _output.ToString().Should().Contain("{\"type\":\"status\"}");
    }

    /// <summary>
    /// Verifies events end with newline.
    /// </summary>
    [Fact]
    public void Emit_ShouldAppendNewline()
    {
        _serializer.Serialize(Arg.Any<BaseEvent>()).Returns("{}");

        _sut.Emit(new StatusEvent { Status = "test", Message = "msg" });

        _output.ToString().Should().EndWith(Environment.NewLine);
    }

    /// <summary>
    /// Verifies multiple events are on separate lines.
    /// </summary>
    [Fact]
    public void Emit_Multiple_ShouldWriteSeparateLines()
    {
        _serializer.Serialize(Arg.Any<BaseEvent>()).Returns("{\"n\":1}", "{\"n\":2}");

        _sut.Emit(new StatusEvent { Status = "1", Message = "a" });
        _sut.Emit(new StatusEvent { Status = "2", Message = "b" });

        var lines = _output
            .ToString()
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        lines.Should().HaveCount(2);
    }

    /// <summary>
    /// Verifies null event throws.
    /// </summary>
    [Fact]
    public void Emit_NullEvent_ShouldThrow()
    {
        var act = () => _sut.Emit(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Verifies stats track total events.
    /// </summary>
    [Fact]
    public void GetStats_ShouldTrackTotalEvents()
    {
        _serializer.Serialize(Arg.Any<BaseEvent>()).Returns("{}");

        _sut.Emit(new StatusEvent { Status = "a", Message = "1" });
        _sut.Emit(new StatusEvent { Status = "b", Message = "2" });

        _sut.GetStats().TotalEvents.Should().Be(2);
    }

    /// <summary>
    /// Verifies error events are counted.
    /// </summary>
    [Fact]
    public void GetStats_ShouldTrackErrorCount()
    {
        _serializer.Serialize(Arg.Any<BaseEvent>()).Returns("{}");

        _sut.Emit(new StatusEvent { Status = "ok", Message = "ok" });
        _sut.Emit(
            new ErrorEvent
            {
                Code = "ERR",
                Message = "error",
                Component = "test",
            }
        );

        var stats = _sut.GetStats();
        stats.TotalEvents.Should().Be(2);
        stats.ErrorCount.Should().Be(1);
    }

    /// <summary>
    /// Verifies warning events are counted.
    /// </summary>
    [Fact]
    public void GetStats_ShouldTrackWarningCount()
    {
        _serializer.Serialize(Arg.Any<BaseEvent>()).Returns("{}");

        _sut.Emit(new StatusEvent { Status = "ok", Message = "ok" });
        _sut.Emit(
            new WarningEvent
            {
                Code = "WARN",
                Message = "warning",
                Component = "test",
            }
        );

        var stats = _sut.GetStats();
        stats.WarningCount.Should().Be(1);
    }

    /// <summary>
    /// Verifies null output throws in constructor.
    /// </summary>
    [Fact]
    public void Constructor_NullOutput_ShouldThrow()
    {
        var act = () => new EventEmitter(null!, _serializer);

        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Verifies null serializer throws in constructor.
    /// </summary>
    [Fact]
    public void Constructor_NullSerializer_ShouldThrow()
    {
        var act = () => new EventEmitter(_output, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Verifies null options throws in Configure.
    /// </summary>
    [Fact]
    public void Configure_NullOptions_ShouldThrow()
    {
        var act = () => _sut.Configure(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Verifies throughput of 1000 events per second.
    /// </summary>
    [Fact]
    public void Emit_ShouldSupport1000EventsPerSecond()
    {
        var realSerializer = new EventSerializer();
        using var perfOutput = new StringWriter();
        var perfEmitter = new EventEmitter(perfOutput, realSerializer);
        var eventObj = new StatusEvent { Status = "test", Message = "perf" };

        for (int i = 0; i < 10; i++)
        {
            perfEmitter.Emit(eventObj);
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++)
        {
            perfEmitter.Emit(eventObj);
        }

        sw.Stop();
        sw.ElapsedMilliseconds.Should().BeLessThan(1000);
    }
}

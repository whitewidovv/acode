// <copyright file="EventIdGeneratorTests.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

namespace Acode.Cli.Tests.JSONL;

using Acode.Cli.JSONL;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for <see cref="EventIdGenerator"/>.
/// </summary>
public class EventIdGeneratorTests
{
    private readonly EventIdGenerator _sut = new();

    /// <summary>
    /// Verifies generated ID has correct prefix.
    /// </summary>
    [Fact]
    public void Next_ShouldHaveEvtPrefix()
    {
        var result = _sut.Next();

        result.Should().StartWith("evt_");
    }

    /// <summary>
    /// Verifies generated IDs are unique.
    /// </summary>
    [Fact]
    public void Next_ShouldBeUnique()
    {
        var sut = new EventIdGenerator();
        var ids = new HashSet<string>();

        for (int i = 0; i < 1000; i++)
        {
            ids.Add(sut.Next());
        }

        ids.Should().HaveCount(1000);
    }

    /// <summary>
    /// Verifies IDs increment sequentially.
    /// </summary>
    [Fact]
    public void Next_ShouldIncrementSequentially()
    {
        var sut = new EventIdGenerator();
        var first = sut.Next();
        var second = sut.Next();
        var third = sut.Next();

        int ExtractNumber(string id) =>
            int.Parse(id.Split('_')[1], System.Globalization.CultureInfo.InvariantCulture);

        var firstNum = ExtractNumber(first);
        var secondNum = ExtractNumber(second);
        var thirdNum = ExtractNumber(third);

        secondNum.Should().BeGreaterThan(firstNum);
        thirdNum.Should().BeGreaterThan(secondNum);
    }

    /// <summary>
    /// Verifies ID length is reasonable.
    /// </summary>
    [Fact]
    public void Next_ShouldHaveReasonableLength()
    {
        var result = _sut.Next();

        result.Length.Should().BeGreaterThanOrEqualTo(5);
        result.Length.Should().BeLessThan(50);
    }

    /// <summary>
    /// Verifies high-throughput generation.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task Next_ShouldBeThreadSafe()
    {
        var sut = new EventIdGenerator();
        var ids = new System.Collections.Concurrent.ConcurrentBag<string>();
        var tasks = new List<Task>();

        for (int t = 0; t < 10; t++)
        {
            tasks.Add(
                Task.Run(() =>
                {
                    for (int i = 0; i < 100; i++)
                    {
                        ids.Add(sut.Next());
                    }
                })
            );
        }

        await Task.WhenAll(tasks).ConfigureAwait(true);

        ids.Distinct().Count().Should().Be(1000);
    }

    /// <summary>
    /// Verifies performance of ID generation.
    /// </summary>
    [Fact]
    public void Next_ShouldBePerformant()
    {
        var sut = new EventIdGenerator();
        for (int i = 0; i < 10; i++)
        {
            sut.Next();
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < 10000; i++)
        {
            sut.Next();
        }

        sw.Stop();
        sw.ElapsedMilliseconds.Should().BeLessThan(100);
    }

    /// <summary>
    /// Verifies IDs contain only valid characters.
    /// </summary>
    [Fact]
    public void Next_ShouldContainValidCharacters()
    {
        var sut = new EventIdGenerator();
        for (int i = 0; i < 100; i++)
        {
            var id = sut.Next();
            id.Should().MatchRegex(@"^evt_\d+$");
        }
    }

    /// <summary>
    /// Verifies custom prefix works.
    /// </summary>
    [Fact]
    public void Next_WithCustomPrefix_ShouldUsePrefix()
    {
        var sut = new EventIdGenerator("custom");

        var result = sut.Next();

        result.Should().StartWith("custom_");
    }

    /// <summary>
    /// Verifies current count property.
    /// </summary>
    [Fact]
    public void CurrentCount_ShouldTrackGenerations()
    {
        var sut = new EventIdGenerator();

        sut.CurrentCount.Should().Be(0);
        sut.Next();
        sut.CurrentCount.Should().Be(1);
        sut.Next();
        sut.CurrentCount.Should().Be(2);
    }

    /// <summary>
    /// Verifies reset resets counter.
    /// </summary>
    [Fact]
    public void Reset_ShouldResetCounter()
    {
        var sut = new EventIdGenerator();
        sut.Next();
        sut.Next();

        sut.Reset();

        sut.CurrentCount.Should().Be(0);
        sut.Next().Should().Be("evt_001");
    }
}

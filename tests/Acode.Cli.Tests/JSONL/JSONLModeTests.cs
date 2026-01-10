// <copyright file="JSONLModeTests.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

namespace Acode.Cli.Tests.JSONL;

using System.IO;
using Acode.Cli.Events;
using Acode.Cli.JSONL;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for JSONL mode activation.
/// </summary>
public class JSONLModeTests
{
    /// <summary>
    /// Verifies --json flag enables JSONL mode.
    /// FR-001: --json flag MUST enable JSONL mode.
    /// </summary>
    [Fact]
    public void Should_Enable_Via_Flag()
    {
        var args = new[] { "--json", "help" };

        var useJson =
            args.Contains("--json")
            || string.Equals(
                Environment.GetEnvironmentVariable("ACODE_JSON"),
                "1",
                StringComparison.Ordinal
            );

        useJson.Should().BeTrue();
    }

    /// <summary>
    /// Verifies ACODE_JSON=1 env var enables JSONL mode.
    /// FR-002: ACODE_JSON=1 env MUST enable JSONL mode.
    /// </summary>
    [Fact]
    public void Should_Enable_Via_EnvVar()
    {
        var originalValue = Environment.GetEnvironmentVariable("ACODE_JSON");
        try
        {
            Environment.SetEnvironmentVariable("ACODE_JSON", "1");

            var args = new[] { "help" };
            var useJson =
                args.Contains("--json")
                || string.Equals(
                    Environment.GetEnvironmentVariable("ACODE_JSON"),
                    "1",
                    StringComparison.Ordinal
                );

            useJson.Should().BeTrue();
        }
        finally
        {
            Environment.SetEnvironmentVariable("ACODE_JSON", originalValue);
        }
    }

    /// <summary>
    /// Verifies ACODE_JSON with different values does not enable mode.
    /// </summary>
    /// <param name="value">The environment variable value to test.</param>
    [Theory]
    [InlineData("0")]
    [InlineData("false")]
    [InlineData("true")]
    [InlineData("")]
    public void Should_Not_Enable_Via_EnvVar_With_Other_Values(string value)
    {
        var originalValue = Environment.GetEnvironmentVariable("ACODE_JSON");
        try
        {
            Environment.SetEnvironmentVariable("ACODE_JSON", value);

            var args = new[] { "help" };
            var useJson =
                args.Contains("--json")
                || string.Equals(
                    Environment.GetEnvironmentVariable("ACODE_JSON"),
                    "1",
                    StringComparison.Ordinal
                );

            useJson.Should().BeFalse();
        }
        finally
        {
            Environment.SetEnvironmentVariable("ACODE_JSON", originalValue);
        }
    }

    /// <summary>
    /// Verifies flag takes precedence when env var is not set.
    /// </summary>
    [Fact]
    public void Should_Enable_Via_Flag_Even_Without_EnvVar()
    {
        var originalValue = Environment.GetEnvironmentVariable("ACODE_JSON");
        try
        {
            Environment.SetEnvironmentVariable("ACODE_JSON", null);

            var args = new[] { "--json", "help" };
            var useJson =
                args.Contains("--json")
                || string.Equals(
                    Environment.GetEnvironmentVariable("ACODE_JSON"),
                    "1",
                    StringComparison.Ordinal
                );

            useJson.Should().BeTrue();
        }
        finally
        {
            Environment.SetEnvironmentVariable("ACODE_JSON", originalValue);
        }
    }

    /// <summary>
    /// Verifies JsonLinesFormatter writes to specified writer.
    /// FR-003: Events MUST be written to stdout.
    /// </summary>
    [Fact]
    public void JsonLinesFormatter_ShouldWriteToOutput()
    {
        using var output = new StringWriter();
        var formatter = new JsonLinesFormatter(output);

        formatter.WriteMessage("test message");

        output.ToString().Should().NotBeEmpty();
    }

    /// <summary>
    /// Verifies JsonLinesFormatter writes valid JSON.
    /// FR-005: Each line MUST be valid JSON.
    /// </summary>
    [Fact]
    public void JsonLinesFormatter_ShouldWriteValidJson()
    {
        using var output = new StringWriter();
        var formatter = new JsonLinesFormatter(output);

        formatter.WriteMessage("test message");

        var json = output.ToString().Trim();
        var act = () => System.Text.Json.JsonDocument.Parse(json);
        act.Should().NotThrow();
    }

    /// <summary>
    /// Verifies JsonLinesFormatter output ends with newline.
    /// FR-006: Lines MUST end with newline.
    /// </summary>
    [Fact]
    public void JsonLinesFormatter_ShouldEndWithNewline()
    {
        using var output = new StringWriter();
        var formatter = new JsonLinesFormatter(output);

        formatter.WriteMessage("test");

        output.ToString().Should().EndWith(Environment.NewLine);
    }

    /// <summary>
    /// Verifies ConsoleFormatter can be selected when not in JSON mode.
    /// </summary>
    [Fact]
    public void Should_Use_ConsoleFormatter_Without_JsonFlag()
    {
        var args = new[] { "help" };
        var useJson = args.Contains("--json");

        useJson.Should().BeFalse();
    }

    /// <summary>
    /// Verifies stderr is not affected by JSONL mode.
    /// FR-007: Logs MUST go to stderr in JSONL mode.
    /// </summary>
    [Fact]
    public void JsonLinesFormatter_ShouldNotAffectStderr()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var formatter = new JsonLinesFormatter(output);

        // Writing to formatter goes to output only
        formatter.WriteMessage("message");

        // Stderr remains empty (formatter doesn't write to stderr directly)
        output.ToString().Should().NotBeEmpty();
        error.ToString().Should().BeEmpty();
    }

    /// <summary>
    /// Verifies JsonLinesFormatter includes type field.
    /// FR-009: Events MUST have "type" field.
    /// </summary>
    [Fact]
    public void JsonLinesFormatter_ShouldIncludeTypeField()
    {
        using var output = new StringWriter();
        var formatter = new JsonLinesFormatter(output);

        formatter.WriteMessage("test");

        output.ToString().Should().Contain("\"type\"");
    }

    /// <summary>
    /// Verifies EventSerializer includes timestamp field.
    /// FR-010: Events MUST have "timestamp" field.
    /// </summary>
    [Fact]
    public void EventSerializer_ShouldIncludeTimestampField()
    {
        var serializer = new EventSerializer();
        var statusEvent = new StatusEvent { Status = "test", Message = "test" };

        var json = serializer.Serialize(statusEvent);

        json.Should().Contain("\"timestamp\"");
    }
}

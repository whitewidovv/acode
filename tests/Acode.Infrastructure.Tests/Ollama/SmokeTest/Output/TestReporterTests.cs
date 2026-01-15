namespace Acode.Infrastructure.Tests.Ollama.SmokeTest.Output;

using System.Text.Json;
using Acode.Infrastructure.Ollama.SmokeTest.Output;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for ITestReporter implementations.
/// </summary>
public sealed class TestReporterTests
{
    [Fact]
    public void TextReporter_FormatsPassedTest()
    {
        // Arrange
        var results = new SmokeTestResults
        {
            Results = new List<TestResult>
            {
                new() { TestName = "Health Check", Passed = true, ElapsedTime = TimeSpan.FromMilliseconds(45) }
            },
            CheckedAt = DateTime.UtcNow,
            TotalDuration = TimeSpan.FromMilliseconds(45)
        };

        var reporter = new TextTestReporter(verbose: false);
        using var output = new StringWriter();

        // Act
        reporter.Report(results, output);
        var result = output.ToString();

        // Assert
        result.Should().Contain("Health Check");
        result.Should().Contain("PASS");
        result.Should().Contain("45");
    }

    [Fact]
    public void TextReporter_FormatsFailedTest()
    {
        // Arrange
        var results = new SmokeTestResults
        {
            Results = new List<TestResult>
            {
                new()
                {
                    TestName = "Health Check",
                    Passed = false,
                    ElapsedTime = TimeSpan.FromMilliseconds(100),
                    ErrorMessage = "Connection refused",
                    DiagnosticHint = "Start Ollama with 'ollama serve'"
                }
            },
            CheckedAt = DateTime.UtcNow,
            TotalDuration = TimeSpan.FromMilliseconds(100)
        };

        var reporter = new TextTestReporter(verbose: false);
        using var output = new StringWriter();

        // Act
        reporter.Report(results, output);
        var result = output.ToString();

        // Assert
        result.Should().Contain("Health Check");
        result.Should().Contain("FAIL");
        result.Should().Contain("Connection refused");
        result.Should().Contain("Start Ollama with 'ollama serve'");
    }

    [Fact]
    public void TextReporter_FormatsSummary()
    {
        // Arrange
        var results = new SmokeTestResults
        {
            Results = new List<TestResult>
            {
                new() { TestName = "Test1", Passed = true, ElapsedTime = TimeSpan.FromMilliseconds(10) },
                new() { TestName = "Test2", Passed = false, ElapsedTime = TimeSpan.FromMilliseconds(20) },
                new() { TestName = "Test3", Passed = true, ElapsedTime = TimeSpan.FromMilliseconds(15) }
            },
            CheckedAt = DateTime.UtcNow,
            TotalDuration = TimeSpan.FromMilliseconds(45)
        };

        var reporter = new TextTestReporter(verbose: false);
        using var output = new StringWriter();

        // Act
        reporter.Report(results, output);
        var result = output.ToString();

        // Assert
        result.Should().Contain("2/3");
        result.Should().Contain("1 failed");
        result.Should().MatchRegex(@"Total time:.*45");
    }

    [Fact]
    public void TextReporter_VerboseMode_ShowsAdditionalDetails()
    {
        // Arrange
        var results = new SmokeTestResults
        {
            Results = new List<TestResult>
            {
                new() { TestName = "Health Check", Passed = true, ElapsedTime = TimeSpan.FromMilliseconds(45) }
            },
            CheckedAt = DateTime.UtcNow,
            TotalDuration = TimeSpan.FromMilliseconds(45)
        };

        var reporter = new TextTestReporter(verbose: true);
        using var output = new StringWriter();

        // Act
        reporter.Report(results, output);
        var result = output.ToString();

        // Assert - Verbose should show more details (timestamp, etc.)
        result.Should().NotBeNullOrEmpty();
        result.Length.Should().BeGreaterThan(50, "verbose output should have more content");
    }

    [Fact]
    public void JsonReporter_ProducesValidJson()
    {
        // Arrange
        var results = new SmokeTestResults
        {
            Results = new List<TestResult>
            {
                new() { TestName = "Health Check", Passed = true, ElapsedTime = TimeSpan.FromMilliseconds(45) }
            },
            CheckedAt = new DateTime(2026, 1, 13, 12, 0, 0, DateTimeKind.Utc),
            TotalDuration = TimeSpan.FromMilliseconds(45)
        };

        var reporter = new JsonTestReporter();
        using var output = new StringWriter();

        // Act
        reporter.Report(results, output);
        var result = output.ToString();

        // Assert - Should be valid JSON
        using var parsed = JsonDocument.Parse(result);
        parsed.Should().NotBeNull();

        var root = parsed.RootElement;
        root.GetProperty("allPassed").GetBoolean().Should().BeTrue();
        root.GetProperty("passedCount").GetInt32().Should().Be(1);
        root.GetProperty("failedCount").GetInt32().Should().Be(0);
        root.GetProperty("results").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public void JsonReporter_IncludesAllTestResults()
    {
        // Arrange
        var results = new SmokeTestResults
        {
            Results = new List<TestResult>
            {
                new() { TestName = "Test1", Passed = true, ElapsedTime = TimeSpan.FromMilliseconds(10) },
                new()
                {
                    TestName = "Test2",
                    Passed = false,
                    ElapsedTime = TimeSpan.FromMilliseconds(20),
                    ErrorMessage = "Test error",
                    DiagnosticHint = "Fix hint"
                }
            },
            CheckedAt = DateTime.UtcNow,
            TotalDuration = TimeSpan.FromMilliseconds(30)
        };

        var reporter = new JsonTestReporter();
        using var output = new StringWriter();

        // Act
        reporter.Report(results, output);
        var result = output.ToString();

        // Assert
        using var parsed = JsonDocument.Parse(result);
        var resultsArray = parsed.RootElement.GetProperty("results");
        resultsArray.GetArrayLength().Should().Be(2);

        var test2 = resultsArray[1];
        test2.GetProperty("testName").GetString().Should().Be("Test2");
        test2.GetProperty("passed").GetBoolean().Should().BeFalse();
        test2.GetProperty("errorMessage").GetString().Should().Be("Test error");
        test2.GetProperty("diagnosticHint").GetString().Should().Be("Fix hint");
    }
}

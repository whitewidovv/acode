namespace Acode.Infrastructure.Tests.Ollama.SmokeTest.Output;

using Acode.Infrastructure.Ollama.SmokeTest.Output;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for TestResult and SmokeTestResults models.
/// </summary>
public sealed class TestResultTests
{
    [Fact]
    public void TestResult_HasRequiredProperties()
    {
        // Arrange & Act
        var result = new TestResult
        {
            TestName = "Health Check",
            Passed = true,
            ElapsedTime = TimeSpan.FromMilliseconds(45)
        };

        // Assert
        result.TestName.Should().Be("Health Check");
        result.Passed.Should().BeTrue();
        result.ElapsedTime.Should().Be(TimeSpan.FromMilliseconds(45));
        result.ErrorMessage.Should().BeNull();
        result.DiagnosticHint.Should().BeNull();
    }

    [Fact]
    public void TestResult_SupportsFailureWithError()
    {
        // Arrange & Act
        var result = new TestResult
        {
            TestName = "Health Check",
            Passed = false,
            ElapsedTime = TimeSpan.FromMilliseconds(100),
            ErrorMessage = "Connection refused",
            DiagnosticHint = "Start Ollama with 'ollama serve'"
        };

        // Assert
        result.Passed.Should().BeFalse();
        result.ErrorMessage.Should().Be("Connection refused");
        result.DiagnosticHint.Should().Be("Start Ollama with 'ollama serve'");
    }

    [Fact]
    public void SmokeTestResults_CalculatesAllPassed()
    {
        // Arrange
        var results = new SmokeTestResults
        {
            Results = new List<TestResult>
            {
                new() { TestName = "Test1", Passed = true, ElapsedTime = TimeSpan.FromMilliseconds(10) },
                new() { TestName = "Test2", Passed = true, ElapsedTime = TimeSpan.FromMilliseconds(20) }
            },
            CheckedAt = DateTime.UtcNow,
            TotalDuration = TimeSpan.FromMilliseconds(30)
        };

        // Act & Assert
        results.AllPassed.Should().BeTrue();
    }

    [Fact]
    public void SmokeTestResults_AllPassed_IsFalse_WhenAnyTestFails()
    {
        // Arrange
        var results = new SmokeTestResults
        {
            Results = new List<TestResult>
            {
                new() { TestName = "Test1", Passed = true, ElapsedTime = TimeSpan.FromMilliseconds(10) },
                new() { TestName = "Test2", Passed = false, ElapsedTime = TimeSpan.FromMilliseconds(20) }
            },
            CheckedAt = DateTime.UtcNow,
            TotalDuration = TimeSpan.FromMilliseconds(30)
        };

        // Act & Assert
        results.AllPassed.Should().BeFalse();
    }

    [Fact]
    public void SmokeTestResults_CountsPassedAndFailed()
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

        // Act & Assert
        results.PassedCount.Should().Be(2);
        results.FailedCount.Should().Be(1);
    }

    [Fact]
    public void SmokeTestResults_EmptyResults_AllPassed()
    {
        // Arrange
        var results = new SmokeTestResults
        {
            Results = new List<TestResult>(),
            CheckedAt = DateTime.UtcNow,
            TotalDuration = TimeSpan.Zero
        };

        // Act & Assert
        results.AllPassed.Should().BeTrue("no tests means nothing failed");
        results.PassedCount.Should().Be(0);
        results.FailedCount.Should().Be(0);
    }
}

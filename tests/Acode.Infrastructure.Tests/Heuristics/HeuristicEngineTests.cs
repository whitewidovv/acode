namespace Acode.Infrastructure.Tests.Heuristics;

using Acode.Application.Heuristics;
using Acode.Infrastructure.Heuristics;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

/// <summary>
/// Unit tests for <see cref="HeuristicEngine"/>.
/// Tests AC-009 to AC-014: HeuristicEngine behavior.
/// </summary>
public sealed class HeuristicEngineTests
{
    private readonly ILogger<HeuristicEngine> _logger = NullLogger<HeuristicEngine>.Instance;

    /// <summary>
    /// Test that all registered heuristics are executed.
    /// AC-010: HeuristicEngine runs all heuristics.
    /// </summary>
    [Fact]
    public void Should_Run_All_Registered_Heuristics()
    {
        // Arrange
        var heuristic1 = Substitute.For<IRoutingHeuristic>();
        heuristic1.Name.Returns("Heuristic1");
        heuristic1.Priority.Returns(1);
        heuristic1
            .Evaluate(Arg.Any<HeuristicContext>())
            .Returns(
                new HeuristicResult
                {
                    Score = 30,
                    Confidence = 0.8,
                    Reasoning = "Test reason 1",
                }
            );

        var heuristic2 = Substitute.For<IRoutingHeuristic>();
        heuristic2.Name.Returns("Heuristic2");
        heuristic2.Priority.Returns(2);
        heuristic2
            .Evaluate(Arg.Any<HeuristicContext>())
            .Returns(
                new HeuristicResult
                {
                    Score = 50,
                    Confidence = 0.9,
                    Reasoning = "Test reason 2",
                }
            );

        var engine = new HeuristicEngine(
            new[] { heuristic1, heuristic2 },
            CreateDefaultConfig(),
            _logger
        );
        var context = new HeuristicContext
        {
            TaskDescription = "Test task",
            Files = new List<string> { "test.cs" },
        };

        // Act
        var result = engine.Evaluate(context);

        // Assert
        heuristic1.Received(1).Evaluate(Arg.Any<HeuristicContext>());
        heuristic2.Received(1).Evaluate(Arg.Any<HeuristicContext>());
        result.CombinedScore.Should().BeInRange(0, 100);
    }

    /// <summary>
    /// Test that scores are weighted by confidence.
    /// AC-012: Weights by confidence.
    /// </summary>
    [Fact]
    public void Should_Weight_Scores_By_Confidence()
    {
        // Arrange
        var lowConfidenceHeuristic = Substitute.For<IRoutingHeuristic>();
        lowConfidenceHeuristic.Name.Returns("LowConfidence");
        lowConfidenceHeuristic.Priority.Returns(1);
        lowConfidenceHeuristic
            .Evaluate(Arg.Any<HeuristicContext>())
            .Returns(
                new HeuristicResult
                {
                    Score = 100,
                    Confidence = 0.1, // Low confidence
                    Reasoning = "Uncertain",
                }
            );

        var highConfidenceHeuristic = Substitute.For<IRoutingHeuristic>();
        highConfidenceHeuristic.Name.Returns("HighConfidence");
        highConfidenceHeuristic.Priority.Returns(2);
        highConfidenceHeuristic
            .Evaluate(Arg.Any<HeuristicContext>())
            .Returns(
                new HeuristicResult
                {
                    Score = 20,
                    Confidence = 0.9, // High confidence
                    Reasoning = "Very certain",
                }
            );

        var engine = new HeuristicEngine(
            new[] { lowConfidenceHeuristic, highConfidenceHeuristic },
            CreateDefaultConfig(),
            _logger
        );
        var context = new HeuristicContext
        {
            TaskDescription = "Test",
            Files = new List<string> { "test.cs" },
        };

        // Act
        var result = engine.Evaluate(context);

        // Assert - high confidence score should dominate
        result.CombinedScore.Should().BeCloseTo(20, 15);
    }

    /// <summary>
    /// Test that failed heuristics are skipped gracefully.
    /// AC-058: Failed heuristic skipped.
    /// </summary>
    [Fact]
    public void Should_Handle_Failed_Heuristic_Gracefully()
    {
        // Arrange
        var failingHeuristic = Substitute.For<IRoutingHeuristic>();
        failingHeuristic.Name.Returns("Failing");
        failingHeuristic.Priority.Returns(1);
        failingHeuristic
            .Evaluate(Arg.Any<HeuristicContext>())
            .Throws(new InvalidOperationException("Heuristic failed"));

        var workingHeuristic = Substitute.For<IRoutingHeuristic>();
        workingHeuristic.Name.Returns("Working");
        workingHeuristic.Priority.Returns(2);
        workingHeuristic
            .Evaluate(Arg.Any<HeuristicContext>())
            .Returns(
                new HeuristicResult
                {
                    Score = 50,
                    Confidence = 0.8,
                    Reasoning = "Works fine",
                }
            );

        var mockLogger = Substitute.For<ILogger<HeuristicEngine>>();
        var engine = new HeuristicEngine(
            new[] { failingHeuristic, workingHeuristic },
            CreateDefaultConfig(),
            mockLogger
        );
        var context = new HeuristicContext
        {
            TaskDescription = "Test",
            Files = new List<string> { "test.cs" },
        };

        // Act
        var result = engine.Evaluate(context);

        // Assert - should still produce result using working heuristic
        result.CombinedScore.Should().BeInRange(0, 100);
    }

    /// <summary>
    /// Test that all heuristics failing returns default score.
    /// AC-059: All failures use default score.
    /// </summary>
    [Fact]
    public void Should_Return_Default_Score_When_All_Heuristics_Fail()
    {
        // Arrange
        var heuristic = Substitute.For<IRoutingHeuristic>();
        heuristic.Name.Returns("Failing");
        heuristic.Priority.Returns(1);
        heuristic
            .Evaluate(Arg.Any<HeuristicContext>())
            .Throws(new InvalidOperationException("Failed"));

        var mockLogger = Substitute.For<ILogger<HeuristicEngine>>();
        var engine = new HeuristicEngine(new[] { heuristic }, CreateDefaultConfig(), mockLogger);
        var context = new HeuristicContext
        {
            TaskDescription = "Test",
            Files = new List<string> { "test.cs" },
        };

        // Act
        var result = engine.Evaluate(context);

        // Assert
        result.CombinedScore.Should().Be(50); // Default medium score
    }

    /// <summary>
    /// Test that heuristics execute in priority order.
    /// AC-010: Runs in priority order.
    /// </summary>
    [Fact]
    public void Should_Execute_Heuristics_In_Priority_Order()
    {
        // Arrange
        var executionOrder = new List<string>();

        var heuristic1 = Substitute.For<IRoutingHeuristic>();
        heuristic1.Name.Returns("Priority3");
        heuristic1.Priority.Returns(3);
        heuristic1
            .When(h => h.Evaluate(Arg.Any<HeuristicContext>()))
            .Do(_ => executionOrder.Add("Priority3"));
        heuristic1
            .Evaluate(Arg.Any<HeuristicContext>())
            .Returns(
                new HeuristicResult
                {
                    Score = 50,
                    Confidence = 0.8,
                    Reasoning = "Test",
                }
            );

        var heuristic2 = Substitute.For<IRoutingHeuristic>();
        heuristic2.Name.Returns("Priority1");
        heuristic2.Priority.Returns(1);
        heuristic2
            .When(h => h.Evaluate(Arg.Any<HeuristicContext>()))
            .Do(_ => executionOrder.Add("Priority1"));
        heuristic2
            .Evaluate(Arg.Any<HeuristicContext>())
            .Returns(
                new HeuristicResult
                {
                    Score = 50,
                    Confidence = 0.8,
                    Reasoning = "Test",
                }
            );

        var heuristic3 = Substitute.For<IRoutingHeuristic>();
        heuristic3.Name.Returns("Priority2");
        heuristic3.Priority.Returns(2);
        heuristic3
            .When(h => h.Evaluate(Arg.Any<HeuristicContext>()))
            .Do(_ => executionOrder.Add("Priority2"));
        heuristic3
            .Evaluate(Arg.Any<HeuristicContext>())
            .Returns(
                new HeuristicResult
                {
                    Score = 50,
                    Confidence = 0.8,
                    Reasoning = "Test",
                }
            );

        var engine = new HeuristicEngine(
            new[] { heuristic1, heuristic2, heuristic3 },
            CreateDefaultConfig(),
            _logger
        );
        var context = new HeuristicContext
        {
            TaskDescription = "Test",
            Files = new List<string> { "test.cs" },
        };

        // Act
        engine.Evaluate(context);

        // Assert
        executionOrder.Should().Equal("Priority1", "Priority2", "Priority3");
    }

    /// <summary>
    /// Test that disabled heuristics are skipped.
    /// </summary>
    [Fact]
    public void Should_Skip_Disabled_Heuristics()
    {
        // Arrange
        var heuristic = Substitute.For<IRoutingHeuristic>();
        heuristic.Name.Returns("Disabled");
        heuristic.Priority.Returns(1);
        heuristic
            .Evaluate(Arg.Any<HeuristicContext>())
            .Returns(
                new HeuristicResult
                {
                    Score = 100,
                    Confidence = 1.0,
                    Reasoning = "Test",
                }
            );

        var config = CreateDefaultConfig();
        config.DisabledHeuristics.Add("Disabled");

        var engine = new HeuristicEngine(new[] { heuristic }, config, _logger);
        var context = new HeuristicContext
        {
            TaskDescription = "Test",
            Files = new List<string> { "test.cs" },
        };

        // Act
        var result = engine.Evaluate(context);

        // Assert
        heuristic.DidNotReceive().Evaluate(Arg.Any<HeuristicContext>());
        result.CombinedScore.Should().Be(50); // Default score since no heuristics ran
    }

    /// <summary>
    /// Test that null context throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Should_Throw_For_Null_Context()
    {
        // Arrange
        var engine = new HeuristicEngine(
            Array.Empty<IRoutingHeuristic>(),
            CreateDefaultConfig(),
            _logger
        );

        // Act
        var action = () => engine.Evaluate(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Test that when heuristics are disabled, default score is returned.
    /// </summary>
    [Fact]
    public void Should_Return_Default_Score_When_Heuristics_Disabled()
    {
        // Arrange
        var heuristic = Substitute.For<IRoutingHeuristic>();
        heuristic.Name.Returns("Test");
        heuristic.Priority.Returns(1);

        var config = CreateDefaultConfig();
        config.Enabled = false;

        var engine = new HeuristicEngine(new[] { heuristic }, config, _logger);
        var context = new HeuristicContext
        {
            TaskDescription = "Test",
            Files = new List<string> { "test.cs" },
        };

        // Act
        var result = engine.Evaluate(context);

        // Assert
        heuristic.DidNotReceive().Evaluate(Arg.Any<HeuristicContext>());
        result.CombinedScore.Should().Be(50);
    }

    /// <summary>
    /// Test that registered heuristics can be introspected.
    /// </summary>
    [Fact]
    public void Should_Return_Registered_Heuristics()
    {
        // Arrange
        var heuristic1 = Substitute.For<IRoutingHeuristic>();
        heuristic1.Name.Returns("FileCount");
        heuristic1.Priority.Returns(1);

        var heuristic2 = Substitute.For<IRoutingHeuristic>();
        heuristic2.Name.Returns("TaskType");
        heuristic2.Priority.Returns(2);

        var engine = new HeuristicEngine(
            new[] { heuristic1, heuristic2 },
            CreateDefaultConfig(),
            _logger
        );

        // Act
        var registered = engine.GetRegisteredHeuristics();

        // Assert
        registered.Should().HaveCount(2);
        registered.Select(h => h.Name).Should().Contain(new[] { "FileCount", "TaskType" });
    }

    /// <summary>
    /// Test that weights from config are applied.
    /// </summary>
    [Fact]
    public void Should_Apply_Configured_Weights()
    {
        // Arrange
        var heuristic = Substitute.For<IRoutingHeuristic>();
        heuristic.Name.Returns("Test");
        heuristic.Priority.Returns(1);
        heuristic
            .Evaluate(Arg.Any<HeuristicContext>())
            .Returns(
                new HeuristicResult
                {
                    Score = 100,
                    Confidence = 1.0,
                    Reasoning = "Test",
                }
            );

        var config = CreateDefaultConfig();
        config.Weights["test"] = 0.5; // Reduce weight by half

        var engine = new HeuristicEngine(new[] { heuristic }, config, _logger);
        var context = new HeuristicContext
        {
            TaskDescription = "Test",
            Files = new List<string> { "test.cs" },
        };

        // Act
        var result = engine.Evaluate(context);

        // Assert - Score of 100 with weight 0.5 = 50
        result.CombinedScore.Should().Be(50);
    }

    private static HeuristicConfiguration CreateDefaultConfig()
    {
        return new HeuristicConfiguration
        {
            Enabled = true,
            Thresholds = new ComplexityThresholds { Low = 30, High = 70 },
        };
    }
}

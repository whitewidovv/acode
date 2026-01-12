namespace Acode.Application.Tests.Audit.Services;

using System.Threading.Tasks;
using Acode.Application.Audit.Services;
using Acode.Domain.Audit;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for CorrelationService.
/// Verifies correlation ID scope management and async propagation.
/// </summary>
public sealed class CorrelationServiceTests
{
    [Fact]
    public void Should_CreateCorrelationScope()
    {
        // Arrange
        var service = new CorrelationService();

        // Act
        using var scope = service.BeginCorrelation();
        var correlationId = service.GetCurrentCorrelationId();

        // Assert
        correlationId.Should().NotBeNull();
    }

    [Fact]
    public void Should_ReturnNullCorrelation_WhenNoScope()
    {
        // Arrange
        var service = new CorrelationService();

        // Act
        var correlationId = service.GetCurrentCorrelationId();

        // Assert
        correlationId.Should().BeNull();
    }

    [Fact]
    public void Should_ClearCorrelation_WhenScopeDisposed()
    {
        // Arrange
        var service = new CorrelationService();

        // Act
        var scope = service.BeginCorrelation();
        var correlationIdDuringScope = service.GetCurrentCorrelationId();
        scope.Dispose();
        var correlationIdAfterDispose = service.GetCurrentCorrelationId();

        // Assert
        correlationIdDuringScope.Should().NotBeNull();
        correlationIdAfterDispose.Should().BeNull();
    }

    [Fact]
    public async Task Should_PropagateCorrelation_AcrossAsync()
    {
        // Arrange
        var service = new CorrelationService();
        CorrelationId? capturedId = null;

        // Act
        using (service.BeginCorrelation())
        {
            var originalId = service.GetCurrentCorrelationId();

            await Task.Run(() =>
            {
                capturedId = service.GetCurrentCorrelationId();
            });

            // Assert
            capturedId.Should().Be(originalId);
        }
    }

    [Fact]
    public void Should_SupportNestedScopes()
    {
        // Arrange
        var service = new CorrelationService();

        // Act
        using var outerScope = service.BeginCorrelation();
        var outerCorrelationId = service.GetCurrentCorrelationId();

        using (var innerScope = service.BeginCorrelation())
        {
            var innerCorrelationId = service.GetCurrentCorrelationId();

            // Assert
            innerCorrelationId.Should().NotBeNull();
            innerCorrelationId.Should().NotBe(outerCorrelationId);
        }

        var restoredCorrelationId = service.GetCurrentCorrelationId();
        restoredCorrelationId.Should().Be(outerCorrelationId);
    }

    [Fact]
    public void Should_AllowExplicitCorrelationId()
    {
        // Arrange
        var service = new CorrelationService();
        var explicitId = CorrelationId.New();

        // Act
        using var scope = service.BeginCorrelation(explicitId);
        var currentId = service.GetCurrentCorrelationId();

        // Assert
        currentId.Should().Be(explicitId);
    }

    [Fact]
    public async Task Should_IsolateCorrelations_BetweenTasks()
    {
        // Arrange
        var service = new CorrelationService();
        CorrelationId? task1Id = null;
        CorrelationId? task2Id = null;

        // Act
        var task1 = Task.Run(() =>
        {
            using (service.BeginCorrelation())
            {
                task1Id = service.GetCurrentCorrelationId();
                Task.Delay(10).Wait();
            }
        });

        var task2 = Task.Run(() =>
        {
            using (service.BeginCorrelation())
            {
                task2Id = service.GetCurrentCorrelationId();
                Task.Delay(10).Wait();
            }
        });

        await Task.WhenAll(task1, task2);

        // Assert
        task1Id.Should().NotBeNull();
        task2Id.Should().NotBeNull();
        task1Id.Should().NotBe(task2Id);
    }

    [Fact]
    public void Should_HandleMultipleDispose()
    {
        // Arrange
        var service = new CorrelationService();
        var scope = service.BeginCorrelation();

        // Act & Assert
        scope.Dispose();
        var action = () => scope.Dispose(); // Should not throw
        action.Should().NotThrow();
    }
}

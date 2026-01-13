using Acode.Application.Audit;
using FluentAssertions;

namespace Acode.Application.Tests.Audit;

/// <summary>
/// Tests for IAuditLogger interface contract.
/// </summary>
public sealed class AuditLoggerTests
{
    [Fact]
    public void IAuditLogger_Interface_Exists()
    {
        // Assert - interface should exist
        typeof(IAuditLogger).Should().NotBeNull();
        typeof(IAuditLogger).IsInterface.Should().BeTrue();
    }

    [Fact]
    public void IAuditLogger_HasLogAsyncMethod()
    {
        // Act - Get all LogAsync methods (there should be 2 overloads)
        var methods = typeof(IAuditLogger).GetMethods()
            .Where(m => m.Name == "LogAsync")
            .ToList();

        // Assert
        methods.Should().HaveCount(2, "IAuditLogger should have 2 LogAsync overloads");
        methods.Should().AllSatisfy(m => m.ReturnType.Name.Should().Be("Task"));
    }

    [Fact]
    public void IAuditLogger_HasFlushAsyncMethod()
    {
        // Act
        var method = typeof(IAuditLogger).GetMethod("FlushAsync");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Name.Should().Be("Task");
    }
}

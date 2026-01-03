namespace Acode.Domain.Tests.Audit;

using Acode.Domain.Audit;
using FluentAssertions;
using Xunit;

public class AuditEventTypeTests
{
    [Fact]
    public void AuditEventType_ShouldHaveAllMandatoryEventTypes()
    {
        // Arrange & Act - FR-003c-21 to FR-003c-45 define these as MUST
        var mandatoryEvents = new[]
        {
            AuditEventType.SessionStart,
            AuditEventType.SessionEnd,
            AuditEventType.ConfigLoad,
            AuditEventType.ConfigError,
            AuditEventType.ModeSelect,
            AuditEventType.CommandStart,
            AuditEventType.CommandEnd,
            AuditEventType.CommandError,
            AuditEventType.FileRead,
            AuditEventType.FileWrite,
            AuditEventType.FileDelete,
            AuditEventType.DirCreate,
            AuditEventType.DirDelete,
            AuditEventType.ProtectedPathBlocked,
            AuditEventType.SecurityViolation,
            AuditEventType.TaskStart,
            AuditEventType.TaskEnd,
            AuditEventType.TaskError,
            AuditEventType.ApprovalRequest,
            AuditEventType.ApprovalResponse,
            AuditEventType.CodeGenerated,
            AuditEventType.TestExecution,
            AuditEventType.BuildExecution,
            AuditEventType.ErrorRecovery,
            AuditEventType.Shutdown
        };

        // Assert - All mandatory events should exist
        foreach (var eventType in mandatoryEvents)
        {
            Enum.IsDefined(typeof(AuditEventType), eventType).Should().BeTrue();
        }
    }

    [Fact]
    public void AuditEventType_ShouldHaveAtLeast25Values()
    {
        // Arrange & Act
        var values = Enum.GetValues<AuditEventType>();

        // Assert - At least the 25 mandatory events from FR-003c
        values.Should().HaveCountGreaterOrEqualTo(25);
    }

    [Fact]
    public void AuditEventType_AllValuesShouldBeDistinct()
    {
        // Arrange & Act
        var values = Enum.GetValues<AuditEventType>();

        // Assert
        values.Should().OnlyHaveUniqueItems();
    }

    [Theory]
    [InlineData(AuditEventType.SessionStart)]
    [InlineData(AuditEventType.SessionEnd)]
    [InlineData(AuditEventType.CommandStart)]
    [InlineData(AuditEventType.CommandEnd)]
    [InlineData(AuditEventType.TaskStart)]
    [InlineData(AuditEventType.TaskEnd)]
    public void AuditEventType_CriticalEvents_ShouldBeDefined(AuditEventType eventType)
    {
        // Arrange & Act & Assert
        Enum.IsDefined(typeof(AuditEventType), eventType).Should().BeTrue();
    }
}

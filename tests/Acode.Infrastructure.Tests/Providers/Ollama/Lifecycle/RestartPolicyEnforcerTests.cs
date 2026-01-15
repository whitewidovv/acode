using Acode.Infrastructure.Providers.Ollama.Lifecycle;

namespace Acode.Infrastructure.Tests.Providers.Ollama.Lifecycle;

/// <summary>
/// Tests for RestartPolicyEnforcer rate limiting logic.
/// Validates restart rate limiting and backoff calculations.
/// </summary>
public class RestartPolicyEnforcerTests
{
    [Fact]
    public void RestartPolicyEnforcer_CanRestart_WhenNoRestartsRecorded()
    {
        // Arrange & Act
        var enforcer = new RestartPolicyEnforcer(maxRestartsPerMinute: 3);

        // Assert
        Assert.True(enforcer.CanRestart());
    }

    [Fact]
    public void RestartPolicyEnforcer_AllowsUpToMaxRestarts()
    {
        // Arrange
        var enforcer = new RestartPolicyEnforcer(maxRestartsPerMinute: 3);

        // Act
        var result1 = enforcer.CanRestart();
        enforcer.RecordRestart();
        var result2 = enforcer.CanRestart();
        enforcer.RecordRestart();
        var result3 = enforcer.CanRestart();
        enforcer.RecordRestart();

        // Assert
        Assert.True(result1);
        Assert.True(result2);
        Assert.True(result3);
    }

    [Fact]
    public void RestartPolicyEnforcer_DeniesRestartAfterMaxReached()
    {
        // Arrange
        var enforcer = new RestartPolicyEnforcer(maxRestartsPerMinute: 3);

        // Act
        enforcer.RecordRestart();
        enforcer.RecordRestart();
        enforcer.RecordRestart();
        var canRestart = enforcer.CanRestart();

        // Assert
        Assert.False(canRestart);
    }

    [Fact]
    public void RestartPolicyEnforcer_BackoffDurationIncreases()
    {
        // Arrange
        var enforcer = new RestartPolicyEnforcer(maxRestartsPerMinute: 3);

        // Act
        var backoff1 = enforcer.GetNextBackoffDuration();
        enforcer.RecordRestart();
        var backoff2 = enforcer.GetNextBackoffDuration();
        enforcer.RecordRestart();
        var backoff3 = enforcer.GetNextBackoffDuration();

        // Assert
        Assert.True(backoff2 > backoff1);
        Assert.True(backoff3 > backoff2);
    }

    [Fact]
    public void RestartPolicyEnforcer_BackoffStartsAt1Second()
    {
        // Arrange
        var enforcer = new RestartPolicyEnforcer(maxRestartsPerMinute: 3);

        // Act
        var backoff = enforcer.GetNextBackoffDuration();

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(1), backoff);
    }

    [Fact]
    public void RestartPolicyEnforcer_BackoffDoubles()
    {
        // Arrange
        var enforcer = new RestartPolicyEnforcer(maxRestartsPerMinute: 3);

        // Act
        var backoff1 = enforcer.GetNextBackoffDuration(); // 1s
        enforcer.RecordRestart();
        var backoff2 = enforcer.GetNextBackoffDuration(); // 2s
        enforcer.RecordRestart();
        var backoff3 = enforcer.GetNextBackoffDuration(); // 4s
        enforcer.RecordRestart();
        var backoff4 = enforcer.GetNextBackoffDuration(); // 8s

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(1), backoff1);
        Assert.Equal(TimeSpan.FromSeconds(2), backoff2);
        Assert.Equal(TimeSpan.FromSeconds(4), backoff3);
        Assert.Equal(TimeSpan.FromSeconds(8), backoff4);
    }

    [Fact]
    public void RestartPolicyEnforcer_CanReset()
    {
        // Arrange
        var enforcer = new RestartPolicyEnforcer(maxRestartsPerMinute: 3);
        enforcer.RecordRestart();
        enforcer.RecordRestart();
        enforcer.RecordRestart();
        Assert.False(enforcer.CanRestart());

        // Act
        enforcer.Reset();

        // Assert
        Assert.True(enforcer.CanRestart());
        Assert.Equal(TimeSpan.FromSeconds(1), enforcer.GetNextBackoffDuration());
    }

    [Fact]
    public void RestartPolicyEnforcer_DifferentMaxRestartsValue()
    {
        // Arrange
        var enforcer = new RestartPolicyEnforcer(maxRestartsPerMinute: 5);

        // Act
        for (int i = 0; i < 5; i++)
        {
            Assert.True(enforcer.CanRestart());
            enforcer.RecordRestart();
        }

        // Assert
        Assert.False(enforcer.CanRestart());
    }

    [Fact]
    public void RestartPolicyEnforcer_GetNextBackoffWithoutRestart()
    {
        // Arrange
        var enforcer = new RestartPolicyEnforcer(maxRestartsPerMinute: 3);

        // Act
        var backoff1 = enforcer.GetNextBackoffDuration();
        var backoff2 = enforcer.GetNextBackoffDuration();

        // Assert - Calling without recording restart should always return 1s
        Assert.Equal(TimeSpan.FromSeconds(1), backoff1);
        Assert.Equal(TimeSpan.FromSeconds(1), backoff2);
    }
}

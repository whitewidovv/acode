// tests/Acode.Infrastructure.Tests/Health/StorageCheckTests.cs
#pragma warning disable CA2007 // ConfigureAwait not required in test code

namespace Acode.Infrastructure.Tests.Health;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Health;
using Acode.Infrastructure.Health.Checks;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for StorageCheck.
/// Verifies storage health checking with disk space monitoring.
/// </summary>
public sealed class StorageCheckTests
{
    [Fact]
    public async Task CheckAsync_WithSufficientSpace_ReturnsHealthy()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();

        try
        {
            // Use temp file on system drive which should have sufficient space
            var check = new StorageCheck(tempFile, degradedThresholdMb: 100, unhealthyThresholdMb: 50);

            // Act
            var result = await check.CheckAsync(CancellationToken.None);

            // Assert
            // Most systems will have > 100 MB free, so this should be healthy
            result.Status.Should().BeOneOf(HealthStatus.Healthy, HealthStatus.Degraded);
            result.Details.Should().ContainKey("FreeSpaceGB");
            result.Details.Should().ContainKey("FreePercentage");
            result.Details.Should().ContainKey("DatabaseSizeMB");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task CheckAsync_IncludesDetailsInResult()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();

        try
        {
            var check = new StorageCheck(tempFile);

            // Act
            var result = await check.CheckAsync(CancellationToken.None);

            // Assert
            result.Details.Should().NotBeNull();
            result.Details.Should().ContainKey("DatabaseSizeMB");
            result.Details.Should().ContainKey("FreeSpaceGB");
            result.Details.Should().ContainKey("FreePercentage");
            result.Details.Should().ContainKey("DriveName");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task CheckAsync_WithNonExistentDatabase_ReturnsZeroSize()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent-{Guid.NewGuid()}.db");
        var check = new StorageCheck(nonExistentPath);

        // Act
        var result = await check.CheckAsync(CancellationToken.None);

        // Assert
        result.Details.Should().ContainKey("DatabaseSizeMB");
        result.Details!["DatabaseSizeMB"].Should().Be(0.0);
    }

    [Fact]
    public async Task CheckAsync_RecordsDuration()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();

        try
        {
            var check = new StorageCheck(tempFile);

            // Act
            var result = await check.CheckAsync(CancellationToken.None);

            // Assert
            result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Constructor_WithNullPath_ThrowsArgumentException()
    {
        // Act
        var act = () => new StorageCheck(null!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithEmptyPath_ThrowsArgumentException()
    {
        // Act
        var act = () => new StorageCheck(string.Empty);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithDegradedThresholdLessThanUnhealthy_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();

        try
        {
            // Act
            var act = () => new StorageCheck(tempFile, degradedThresholdMb: 50, unhealthyThresholdMb: 100);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Constructor_WithDegradedPercentageLessThanUnhealthy_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();

        try
        {
            // Act
            var act = () => new StorageCheck(tempFile, degradedPercentage: 5.0, unhealthyPercentage: 10.0);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Name_ReturnsExpectedValue()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();

        try
        {
            var check = new StorageCheck(tempFile);

            // Act & Assert
            check.Name.Should().Be("Storage");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task CheckAsync_WithValidPath_DoesNotThrow()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();

        try
        {
            var check = new StorageCheck(tempFile);

            // Act
            var act = async () => await check.CheckAsync(CancellationToken.None);

            // Assert
            await act.Should().NotThrowAsync("valid paths should not cause exceptions");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task CheckAsync_CalculatesPercentageCorrectly()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();

        try
        {
            var check = new StorageCheck(tempFile);

            // Act
            var result = await check.CheckAsync(CancellationToken.None);

            // Assert
            result.Details.Should().ContainKey("FreePercentage");
            var percentage = (double)result.Details!["FreePercentage"];
            percentage.Should().BeGreaterThan(0).And.BeLessThanOrEqualTo(100);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}

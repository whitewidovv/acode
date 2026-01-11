// src/Acode.Infrastructure/Configuration/RetryOptions.cs
namespace Acode.Infrastructure.Configuration;

/// <summary>
/// Configuration options for database retry policy.
/// </summary>
public sealed class RetryOptions
{
    /// <summary>Gets or sets a value indicating whether retry policy is enabled.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Gets or sets the maximum retry attempts.</summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>Gets or sets the base delay between retries in milliseconds.</summary>
    public int BaseDelayMs { get; set; } = 100;

    /// <summary>Gets or sets the maximum delay between retries in milliseconds.</summary>
    public int MaxDelayMs { get; set; } = 5000;
}

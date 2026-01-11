// <copyright file="ProgressInterval.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

namespace Acode.Cli.Progress;

/// <summary>
/// Manages progress reporting intervals.
/// </summary>
/// <remarks>
/// FR-048: Progress frequency MUST be configurable.
/// FR-049: Default progress interval: 10 seconds.
/// </remarks>
public sealed class ProgressInterval
{
    /// <summary>
    /// The default progress interval (10 seconds).
    /// </summary>
    public static readonly TimeSpan Default = TimeSpan.FromSeconds(10);

    /// <summary>
    /// The minimum allowed progress interval (1 second).
    /// </summary>
    public static readonly TimeSpan Minimum = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The maximum allowed progress interval (5 minutes).
    /// </summary>
    public static readonly TimeSpan Maximum = TimeSpan.FromMinutes(5);

    private TimeSpan _interval;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProgressInterval"/> class with the default interval.
    /// </summary>
    public ProgressInterval()
        : this(Default)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProgressInterval"/> class.
    /// </summary>
    /// <param name="interval">The interval between progress reports.</param>
    public ProgressInterval(TimeSpan interval)
    {
        Interval = interval;
    }

    /// <summary>
    /// Gets or sets the progress reporting interval.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the interval is less than <see cref="Minimum"/> or greater than <see cref="Maximum"/>.
    /// </exception>
    public TimeSpan Interval
    {
        get => _interval;
        set
        {
            if (value < Minimum || value > Maximum)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    $"Interval must be between {Minimum.TotalSeconds}s and {Maximum.TotalMinutes}m");
            }

            _interval = value;
        }
    }

    /// <summary>
    /// Creates a <see cref="ProgressInterval"/> from seconds.
    /// </summary>
    /// <param name="seconds">The interval in seconds.</param>
    /// <returns>The progress interval.</returns>
    public static ProgressInterval FromSeconds(int seconds)
    {
        return new ProgressInterval(TimeSpan.FromSeconds(seconds));
    }
}

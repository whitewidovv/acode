// <copyright file="ModelEvent.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

namespace Acode.Cli.Events;

/// <summary>
/// Event for model operations.
/// </summary>
/// <remarks>
/// Emitted for inference, loading, and other model operations.
/// Enables monitoring token consumption and latency.
/// FR-026: "model_event" for model operations.
/// </remarks>
public sealed record ModelEvent : BaseEvent
{
    /// <summary>
    /// Gets the model identifier.
    /// </summary>
    public required string ModelId { get; init; }

    /// <summary>
    /// Gets the operation performed.
    /// </summary>
    /// <remarks>
    /// Examples: "inference", "load", "unload".
    /// </remarks>
    public required string Operation { get; init; }

    /// <summary>
    /// Gets the number of tokens used.
    /// </summary>
    public int? TokensUsed { get; init; }

    /// <summary>
    /// Gets the operation latency in milliseconds.
    /// </summary>
    public long? LatencyMs { get; init; }

    /// <summary>
    /// Gets the tokens per second throughput.
    /// </summary>
    public double? TokensPerSecond { get; init; }

    /// <summary>
    /// Gets the result status.
    /// </summary>
    public string? Result { get; init; }
}

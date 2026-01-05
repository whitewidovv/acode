namespace Acode.Application.Truncation;

/// <summary>
/// Interface for content truncation strategies.
/// </summary>
public interface ITruncationStrategy
{
    /// <summary>
    /// Gets the strategy type implemented by this instance.
    /// </summary>
    TruncationStrategy StrategyType { get; }

    /// <summary>
    /// Truncates content according to the strategy's rules.
    /// </summary>
    /// <param name="content">The content to truncate.</param>
    /// <param name="limits">The truncation limits to apply.</param>
    /// <returns>The truncation result with metadata.</returns>
    TruncationResult Truncate(string content, TruncationLimits limits);
}

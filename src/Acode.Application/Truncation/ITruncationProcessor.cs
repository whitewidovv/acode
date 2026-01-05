namespace Acode.Application.Truncation;

/// <summary>
/// Interface for processing tool outputs through truncation and artifact creation.
/// </summary>
public interface ITruncationProcessor
{
    /// <summary>
    /// Processes content through truncation or artifact creation.
    /// </summary>
    /// <param name="content">The content to process.</param>
    /// <param name="toolName">The name of the tool that produced the content.</param>
    /// <param name="contentType">The content type (default: "text/plain").</param>
    /// <returns>The truncation result.</returns>
    Task<TruncationResult> ProcessAsync(
        string content,
        string toolName,
        string contentType = "text/plain");

    /// <summary>
    /// Gets the configured limits for a specific tool.
    /// </summary>
    /// <param name="toolName">The tool name.</param>
    /// <returns>The truncation limits for the tool.</returns>
    TruncationLimits GetLimitsForTool(string toolName);

    /// <summary>
    /// Gets the configured strategy for a specific tool.
    /// </summary>
    /// <param name="toolName">The tool name.</param>
    /// <returns>The truncation strategy for the tool.</returns>
    TruncationStrategy GetStrategyForTool(string toolName);
}

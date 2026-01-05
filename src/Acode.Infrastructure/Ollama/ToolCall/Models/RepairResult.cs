namespace Acode.Infrastructure.Ollama.ToolCall.Models;

/// <summary>
/// Result of a JSON repair attempt.
/// </summary>
public sealed class RepairResult
{
    /// <summary>
    /// Gets a value indicating whether the repair was successful (output is valid JSON).
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the original malformed JSON input.
    /// </summary>
    public string OriginalJson { get; init; } = string.Empty;

    /// <summary>
    /// Gets the repaired JSON (valid) if successful, or original if repair failed.
    /// </summary>
    public string RepairedJson { get; init; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether any repairs were actually applied.
    /// False if input was already valid JSON.
    /// </summary>
    public bool WasRepaired { get; init; }

    /// <summary>
    /// Gets the list of repair operations that were applied.
    /// </summary>
    public IReadOnlyList<string> Repairs { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the error message if repair failed.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Create a successful repair result.
    /// </summary>
    /// <param name="original">The original JSON.</param>
    /// <param name="repaired">The repaired JSON.</param>
    /// <param name="repairs">List of repairs applied.</param>
    /// <returns>A successful repair result.</returns>
    public static RepairResult Ok(string original, string repaired, IReadOnlyList<string> repairs)
    {
        ArgumentNullException.ThrowIfNull(repairs);

        return new()
        {
            Success = true,
            OriginalJson = original,
            RepairedJson = repaired,
            WasRepaired = repairs.Count > 0,
            Repairs = repairs
        };
    }

    /// <summary>
    /// Create a result for already-valid JSON.
    /// </summary>
    /// <param name="json">The valid JSON.</param>
    /// <returns>A repair result indicating no repair was needed.</returns>
    public static RepairResult AlreadyValid(string json) =>
        new()
        {
            Success = true,
            OriginalJson = json,
            RepairedJson = json,
            WasRepaired = false,
            Repairs = Array.Empty<string>()
        };

    /// <summary>
    /// Create a failed repair result.
    /// </summary>
    /// <param name="original">The original JSON.</param>
    /// <param name="error">The error message.</param>
    /// <returns>A failed repair result.</returns>
    public static RepairResult Fail(string original, string error) =>
        new()
        {
            Success = false,
            OriginalJson = original,
            RepairedJson = original,
            WasRepaired = false,
            Error = error
        };
}

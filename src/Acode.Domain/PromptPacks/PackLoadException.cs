namespace Acode.Domain.PromptPacks;

/// <summary>
/// Exception thrown when a prompt pack fails to load.
/// </summary>
public sealed class PackLoadException : PackException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PackLoadException"/> class.
    /// </summary>
    /// <param name="packId">The ID of the pack that failed to load.</param>
    /// <param name="message">The error message.</param>
    public PackLoadException(string packId, string message)
        : base($"Failed to load pack '{packId}': {message}")
    {
        PackId = packId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PackLoadException"/> class.
    /// </summary>
    /// <param name="packId">The ID of the pack that failed to load.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public PackLoadException(string packId, string message, Exception innerException)
        : base($"Failed to load pack '{packId}': {message}", innerException)
    {
        PackId = packId;
    }

    /// <summary>
    /// Gets the ID of the pack that failed to load.
    /// </summary>
    public string PackId { get; }
}

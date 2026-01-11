namespace Acode.Domain.PromptPacks.Exceptions;

/// <summary>
/// Exception thrown when a requested pack is not found.
/// </summary>
public sealed class PackNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PackNotFoundException"/> class.
    /// </summary>
    /// <param name="packId">The pack ID that was not found.</param>
    public PackNotFoundException(string packId)
        : base($"Pack '{packId}' was not found.")
    {
        PackId = packId;
        ErrorCode = "ACODE-PKL-001";
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PackNotFoundException"/> class.
    /// </summary>
    /// <param name="packId">The pack ID that was not found.</param>
    /// <param name="searchedPaths">The paths that were searched.</param>
    public PackNotFoundException(string packId, IEnumerable<string> searchedPaths)
        : base($"Pack '{packId}' was not found. Searched: {string.Join(", ", searchedPaths)}")
    {
        PackId = packId;
        SearchedPaths = searchedPaths.ToList().AsReadOnly();
        ErrorCode = "ACODE-PKL-001";
    }

    /// <summary>
    /// Gets the error code.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Gets the pack ID that was not found.
    /// </summary>
    public string PackId { get; }

    /// <summary>
    /// Gets the paths that were searched.
    /// </summary>
    public IReadOnlyList<string>? SearchedPaths { get; }
}

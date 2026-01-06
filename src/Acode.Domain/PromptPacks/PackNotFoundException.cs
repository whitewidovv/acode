namespace Acode.Domain.PromptPacks;

/// <summary>
/// Exception thrown when a requested prompt pack cannot be found.
/// </summary>
public sealed class PackNotFoundException : PackException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PackNotFoundException"/> class.
    /// </summary>
    /// <param name="packId">The ID of the pack that was not found.</param>
    public PackNotFoundException(string packId)
        : base($"Pack '{packId}' not found")
    {
        PackId = packId;
    }

    /// <summary>
    /// Gets the ID of the pack that was not found.
    /// </summary>
    public string PackId { get; }
}

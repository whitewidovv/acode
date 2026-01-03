namespace Acode.Domain.Entities;

/// <summary>
/// Placeholder entity to verify Domain layer compilation.
/// This class should be replaced with actual domain entities.
/// </summary>
[Obsolete("Placeholder for initial structure. Replace with actual domain entities.")]
public sealed class PlaceholderEntity
{
    /// <summary>
    /// Gets the unique identifier for this entity.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the name of this entity.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets the date and time when this entity was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

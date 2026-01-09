// src/Acode.Domain/Common/Entity.cs
namespace Acode.Domain.Common;

using System;

/// <summary>
/// Base class for entities with identity-based equality.
/// Entities are objects that have a distinct identity and lifecycle.
/// </summary>
/// <typeparam name="TId">The type of the entity's identifier.</typeparam>
public abstract class Entity<TId>
    where TId : IEquatable<TId>
{
    /// <summary>
    /// Gets or sets the entity's unique identifier.
    /// </summary>
    public TId Id { get; set; } = default!;

    /// <summary>
    /// Determines whether two entities are equal based on their IDs.
    /// </summary>
    /// <param name="left">The first entity.</param>
    /// <param name="right">The second entity.</param>
    /// <returns>True if the entities have the same ID and type; otherwise false.</returns>
    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
    {
        if (left is null && right is null)
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two entities are not equal.
    /// </summary>
    /// <param name="left">The first entity.</param>
    /// <param name="right">The second entity.</param>
    /// <returns>True if the entities do not have the same ID or type; otherwise false.</returns>
    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
    {
        return !(left == right);
    }

    /// <summary>
    /// Determines whether this entity is equal to another object.
    /// Entities are equal if they have the same ID and are of the same type.
    /// </summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>True if the objects are equal; otherwise false.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        var other = (Entity<TId>)obj;
        return Id.Equals(other.Id);
    }

    /// <summary>
    /// Returns the hash code for this entity based on its ID.
    /// </summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}

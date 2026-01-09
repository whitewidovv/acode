// src/Acode.Domain/Common/AggregateRoot.cs
namespace Acode.Domain.Common;

using System;

/// <summary>
/// Base class for aggregate roots in Domain-Driven Design.
/// Aggregate roots are entities that serve as the entry point to an aggregate and enforce invariants.
/// </summary>
/// <typeparam name="TId">The type of the aggregate root's identifier.</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : IEquatable<TId>
{
}

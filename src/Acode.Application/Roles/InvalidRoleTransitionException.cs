namespace Acode.Application.Roles;

using Acode.Domain.Roles;

/// <summary>
/// Exception thrown when a role transition violates preconditions or transition rules.
/// </summary>
/// <remarks>
/// Error code: ACODE-ROL-002 - Role transition not allowed.
/// </remarks>
public sealed class InvalidRoleTransitionException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidRoleTransitionException"/> class.
    /// </summary>
    /// <param name="fromRole">The role the transition was attempted from.</param>
    /// <param name="toRole">The role the transition was attempted to.</param>
    /// <param name="message">The error message.</param>
    public InvalidRoleTransitionException(AgentRole fromRole, AgentRole toRole, string message)
        : base(message)
    {
        FromRole = fromRole;
        ToRole = toRole;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidRoleTransitionException"/> class with an inner exception.
    /// </summary>
    /// <param name="fromRole">The role the transition was attempted from.</param>
    /// <param name="toRole">The role the transition was attempted to.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public InvalidRoleTransitionException(
        AgentRole fromRole,
        AgentRole toRole,
        string message,
        Exception innerException
    )
        : base(message, innerException)
    {
        FromRole = fromRole;
        ToRole = toRole;
    }

    /// <summary>
    /// Gets the role the transition was attempted from.
    /// </summary>
    public AgentRole FromRole { get; }

    /// <summary>
    /// Gets the role the transition was attempted to.
    /// </summary>
    public AgentRole ToRole { get; }
}

namespace Acode.Cli.Routing;

/// <summary>
/// Represents the result of routing a command.
/// </summary>
/// <remarks>
/// Contains the resolved command, remaining arguments, and information
/// about whether an unknown command was encountered.
/// </remarks>
/// <param name="Command">The resolved command, or null if unknown.</param>
/// <param name="RemainingArgs">Arguments remaining after command resolution.</param>
/// <param name="IsUnknown">True if the command was not found.</param>
/// <param name="UnknownName">The name of the unknown command, if applicable.</param>
public sealed record RouteResult(
    ICommand? Command,
    string[] RemainingArgs,
    bool IsUnknown,
    string? UnknownName
)
{
    /// <summary>
    /// Creates a successful route result.
    /// </summary>
    /// <param name="command">The resolved command.</param>
    /// <param name="remainingArgs">Arguments after command name.</param>
    /// <returns>A successful RouteResult.</returns>
    public static RouteResult Success(ICommand command, string[] remainingArgs) =>
        new(command, remainingArgs, IsUnknown: false, UnknownName: null);

    /// <summary>
    /// Creates a failed route result for an unknown command.
    /// </summary>
    /// <param name="unknownName">The name of the unknown command.</param>
    /// <returns>An unknown command RouteResult.</returns>
    public static RouteResult Unknown(string unknownName) =>
        new(Command: null, RemainingArgs: Array.Empty<string>(), IsUnknown: true, unknownName);
}

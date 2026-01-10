namespace Acode.Cli.Help;

using Acode.Cli.Commands;

/// <summary>
/// Interface for commands that expose metadata.
/// </summary>
public interface IHasMetadata
{
    /// <summary>
    /// Gets the command metadata.
    /// </summary>
    CommandMetadata Metadata { get; }
}

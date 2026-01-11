using Acode.Domain.Commands;

namespace Acode.Application.Commands;

/// <summary>
/// Interface for parsing command specifications from configuration.
/// Supports string, array, and object formats per Task 002.c FR-002c-31 through FR-002c-50.
/// </summary>
public interface ICommandParser
{
    /// <summary>
    /// Parses a command value from configuration into one or more CommandSpec objects.
    /// Handles string, array, and object formats automatically.
    /// </summary>
    /// <param name="commandValue">The command value from YAML config (string, array, or object).</param>
    /// <returns>A list of parsed CommandSpec objects.</returns>
    /// <exception cref="ArgumentNullException">When commandValue is null.</exception>
    /// <exception cref="ArgumentException">When commandValue format is invalid.</exception>
    IReadOnlyList<CommandSpec> Parse(object commandValue);

    /// <summary>
    /// Parses a simple string command into a CommandSpec.
    /// Example: "npm install" → CommandSpec{ Run = "npm install" }.
    /// </summary>
    /// <param name="command">The command string.</param>
    /// <returns>A CommandSpec with the command.</returns>
    /// <exception cref="ArgumentException">When command is null, empty, or whitespace-only.</exception>
    CommandSpec ParseString(string command);

    /// <summary>
    /// Parses an array of commands into a list of CommandSpec objects.
    /// Each element can be a string or an object (mixed format supported).
    /// Example: ["npm install", { run: "npm build", timeout: 60 }].
    /// </summary>
    /// <param name="commands">Array of command values.</param>
    /// <returns>A list of CommandSpec objects.</returns>
    /// <exception cref="ArgumentNullException">When commands array is null.</exception>
    IReadOnlyList<CommandSpec> ParseArray(IEnumerable<object> commands);

    /// <summary>
    /// Parses a command object with full options into a CommandSpec.
    /// Example: { run: "npm test", cwd: "src", timeout: 120, retry: 2 }.
    /// </summary>
    /// <param name="commandObject">Dictionary representing the command object.</param>
    /// <returns>A CommandSpec with all options.</returns>
    /// <exception cref="ArgumentNullException">When commandObject is null.</exception>
    /// <exception cref="ArgumentException">When "run" property is missing or invalid.</exception>
    CommandSpec ParseObject(IDictionary<string, object> commandObject);
}

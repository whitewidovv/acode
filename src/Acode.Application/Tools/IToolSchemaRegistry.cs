namespace Acode.Application.Tools;

using System.Text.Json;
using Acode.Domain.Models.Inference;
using Acode.Domain.Tools;

/// <summary>
/// Registry for tool definitions with JSON Schema validation.
/// </summary>
/// <remarks>
/// FR-007: Tool Schema Registry requirements.
/// AC-010 to AC-030: Registration behavior.
/// AC-037 to AC-055: Validation behavior.
/// </remarks>
public interface IToolSchemaRegistry
{
    /// <summary>
    /// Gets the number of registered tools.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Registers a tool definition with its schema.
    /// </summary>
    /// <param name="tool">The tool definition to register.</param>
    /// <exception cref="ArgumentNullException">Thrown when tool is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a tool with the same name but different definition is already registered.
    /// </exception>
    /// <remarks>
    /// Registration is idempotent for identical definitions.
    /// </remarks>
    void RegisterTool(ToolDefinition tool);

    /// <summary>
    /// Gets a tool definition by name.
    /// </summary>
    /// <param name="toolName">The name of the tool.</param>
    /// <returns>The tool definition.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when tool is not registered.</exception>
    ToolDefinition GetToolDefinition(string toolName);

    /// <summary>
    /// Tries to get a tool definition by name.
    /// </summary>
    /// <param name="toolName">The name of the tool.</param>
    /// <param name="tool">The tool definition if found; otherwise, null.</param>
    /// <returns>True if found; otherwise, false.</returns>
    bool TryGetToolDefinition(string toolName, out ToolDefinition? tool);

    /// <summary>
    /// Gets all registered tool definitions.
    /// </summary>
    /// <returns>A read-only collection of all tool definitions.</returns>
    IReadOnlyCollection<ToolDefinition> GetAllTools();

    /// <summary>
    /// Validates tool arguments against the registered schema.
    /// </summary>
    /// <param name="toolName">The name of the tool.</param>
    /// <param name="arguments">The arguments to validate.</param>
    /// <returns>The validated arguments.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when tool is not registered.</exception>
    /// <exception cref="SchemaValidationException">Thrown when validation fails.</exception>
    JsonElement ValidateArguments(string toolName, JsonElement arguments);

    /// <summary>
    /// Tries to validate tool arguments against the registered schema.
    /// </summary>
    /// <param name="toolName">The name of the tool.</param>
    /// <param name="arguments">The arguments to validate.</param>
    /// <param name="errors">The validation errors if validation failed.</param>
    /// <param name="validated">The validated arguments if validation succeeded.</param>
    /// <returns>True if validation succeeded; otherwise, false.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when tool is not registered.</exception>
    bool TryValidateArguments(
        string toolName,
        JsonElement arguments,
        out IReadOnlyCollection<SchemaValidationError> errors,
        out JsonElement validated);

    /// <summary>
    /// Checks if a tool is registered.
    /// </summary>
    /// <param name="toolName">The name of the tool.</param>
    /// <returns>True if registered; otherwise, false.</returns>
    bool IsRegistered(string toolName);
}

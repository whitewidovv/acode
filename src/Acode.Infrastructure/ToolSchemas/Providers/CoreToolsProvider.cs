namespace Acode.Infrastructure.ToolSchemas.Providers;

using Acode.Application.Tools;
using Acode.Domain.Models.Inference;
using Acode.Infrastructure.ToolSchemas.Providers.Schemas.CodeAnalysis;
using Acode.Infrastructure.ToolSchemas.Providers.Schemas.CodeExecution;
using Acode.Infrastructure.ToolSchemas.Providers.Schemas.FileOperations;
using Acode.Infrastructure.ToolSchemas.Providers.Schemas.UserInteraction;
using Acode.Infrastructure.ToolSchemas.Providers.Schemas.VersionControl;

/// <summary>
/// Provides JSON Schema definitions for all 18 core tools.
/// </summary>
/// <remarks>
/// FR-007a: JSON Schema definitions for all core tools.
/// Core tools are organized into 5 categories:
/// - File Operations (6 tools)
/// - Code Execution (2 tools)
/// - Code Analysis (3 tools)
/// - Version Control (4 tools)
/// - User Interaction (2 tools). Note: confirm_action merged with ask_user, yielding 17 distinct schemas.
/// </remarks>
public sealed class CoreToolsProvider : IToolSchemaProvider
{
    /// <inheritdoc />
    public string Name => "CoreTools";

    /// <inheritdoc />
    public string Version => "1.0.0";

    /// <inheritdoc />
    public int Order => 0; // Core tools load first

    /// <inheritdoc />
    public IEnumerable<ToolDefinition> GetToolDefinitions()
    {
        // File Operations (6 tools)
        yield return ReadFileSchema.CreateToolDefinition();
        yield return WriteFileSchema.CreateToolDefinition();
        yield return ListDirectorySchema.CreateToolDefinition();
        yield return SearchFilesSchema.CreateToolDefinition();
        yield return DeleteFileSchema.CreateToolDefinition();
        yield return MoveFileSchema.CreateToolDefinition();

        // Code Execution (2 tools)
        yield return ExecuteCommandSchema.CreateToolDefinition();
        yield return ExecuteScriptSchema.CreateToolDefinition();

        // Code Analysis (3 tools)
        yield return SemanticSearchSchema.CreateToolDefinition();
        yield return FindSymbolSchema.CreateToolDefinition();
        yield return GetDefinitionSchema.CreateToolDefinition();

        // Version Control (4 tools)
        yield return GitStatusSchema.CreateToolDefinition();
        yield return GitDiffSchema.CreateToolDefinition();
        yield return GitLogSchema.CreateToolDefinition();
        yield return GitCommitSchema.CreateToolDefinition();

        // User Interaction (2 tools)
        yield return AskUserSchema.CreateToolDefinition();
        yield return ConfirmActionSchema.CreateToolDefinition();
    }
}

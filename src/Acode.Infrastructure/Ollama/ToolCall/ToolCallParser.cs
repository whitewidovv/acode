namespace Acode.Infrastructure.Ollama.ToolCall;

using System.Text.RegularExpressions;
using Acode.Domain.Models.Inference;
using Acode.Infrastructure.Ollama.ToolCall.Models;

/// <summary>
/// Parses tool calls from Ollama responses, applying JSON repair when needed.
/// </summary>
/// <remarks>
/// FR-007d: Tool call parsing with retry-on-invalid-JSON.
/// Handles malformed JSON arguments using JsonRepairer.
/// </remarks>
public sealed class ToolCallParser
{
    private const int MaxNameLength = 64;
    private static readonly Regex NamePattern = new("^[a-zA-Z0-9_]+$", RegexOptions.Compiled);

    private readonly JsonRepairer repairer;
    private readonly Func<string> idGenerator;
    private int idCounter;

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolCallParser"/> class.
    /// </summary>
    /// <param name="repairer">The JSON repairer to use. If null, creates a default repairer.</param>
    /// <param name="idGenerator">Function to generate IDs for tool calls missing an ID.</param>
    public ToolCallParser(JsonRepairer? repairer = null, Func<string>? idGenerator = null)
    {
        this.repairer = repairer ?? new JsonRepairer();
        this.idGenerator = idGenerator ?? this.DefaultIdGenerator;
    }

    /// <summary>
    /// Parses tool calls from Ollama response format to domain ToolCall objects.
    /// </summary>
    /// <param name="toolCalls">The raw tool calls from Ollama.</param>
    /// <returns>A result containing successfully parsed calls and any errors.</returns>
    public ToolCallParseResult Parse(OllamaToolCall[]? toolCalls)
    {
        if (toolCalls == null || toolCalls.Length == 0)
        {
            return ToolCallParseResult.Empty();
        }

        var parsedCalls = new List<ToolCall>();
        var errors = new List<ToolCallError>();
        var repairs = new List<RepairResult>();

        foreach (var ollamaCall in toolCalls)
        {
            var parseResult = this.ParseSingle(ollamaCall);

            if (parseResult.ToolCall != null)
            {
                parsedCalls.Add(parseResult.ToolCall);
            }

            if (parseResult.Error != null)
            {
                errors.Add(parseResult.Error);
            }

            if (parseResult.Repair != null && parseResult.Repair.WasRepaired)
            {
                repairs.Add(parseResult.Repair);
            }
        }

        return new ToolCallParseResult
        {
            ToolCalls = parsedCalls,
            Errors = errors,
            Repairs = repairs,
        };
    }

    private SingleParseResult ParseSingle(OllamaToolCall ollamaCall)
    {
        // Validate function exists
        if (ollamaCall.Function == null)
        {
            return SingleParseResult.WithError(new ToolCallError(
                "Tool call is missing function definition",
                "ACODE-TLP-001"));
        }

        var function = ollamaCall.Function;
        var toolName = function.Name;

        // Validate function name is not empty
        if (string.IsNullOrWhiteSpace(toolName))
        {
            return SingleParseResult.WithError(new ToolCallError(
                "Tool call has empty function name",
                "ACODE-TLP-002"));
        }

        // Validate function name format (alphanumeric + underscore only)
        if (!NamePattern.IsMatch(toolName))
        {
            return SingleParseResult.WithError(new ToolCallError(
                $"Tool name '{toolName}' contains invalid characters (only alphanumeric and underscore allowed)",
                "ACODE-TLP-003")
            {
                ToolName = toolName,
            });
        }

        // Validate function name length
        if (toolName.Length > MaxNameLength)
        {
            return SingleParseResult.WithError(new ToolCallError(
                $"Tool name exceeds maximum length of {MaxNameLength} characters",
                "ACODE-TLP-005")
            {
                ToolName = toolName,
            });
        }

        // Handle arguments
        var rawArguments = function.Arguments;

        // Default to empty object if null or empty
        if (string.IsNullOrWhiteSpace(rawArguments))
        {
            rawArguments = "{}";
        }

        // Attempt to repair JSON if needed
        var repairResult = this.repairer.TryRepair(rawArguments);

        if (!repairResult.Success)
        {
            return SingleParseResult.WithError(new ToolCallError(
                $"Unable to parse arguments: {repairResult.Error}",
                "ACODE-TLP-004")
            {
                ToolName = toolName,
                RawArguments = rawArguments,
            });
        }

        // Generate ID if missing
        var id = ollamaCall.Id;
        if (string.IsNullOrWhiteSpace(id))
        {
            id = this.idGenerator();
        }

        try
        {
            var toolCall = new ToolCall(id, toolName, repairResult.RepairedJson);
            return new SingleParseResult(toolCall, null, repairResult.WasRepaired ? repairResult : null);
        }
        catch (ArgumentException ex)
        {
            return SingleParseResult.WithError(new ToolCallError(
                $"Failed to create tool call: {ex.Message}",
                "ACODE-TLP-006")
            {
                ToolName = toolName,
                RawArguments = rawArguments,
            });
        }
    }

    private string DefaultIdGenerator()
    {
        return $"gen_{Interlocked.Increment(ref this.idCounter)}";
    }

    /// <summary>
    /// Internal result for parsing a single tool call.
    /// </summary>
    private sealed class SingleParseResult
    {
        public SingleParseResult(ToolCall? toolCall, ToolCallError? error, RepairResult? repair)
        {
            this.ToolCall = toolCall;
            this.Error = error;
            this.Repair = repair;
        }

        public ToolCall? ToolCall { get; }

        public ToolCallError? Error { get; }

        public RepairResult? Repair { get; }

        public static SingleParseResult WithError(ToolCallError error)
        {
            return new SingleParseResult(null, error, null);
        }
    }
}

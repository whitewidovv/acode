using System.Text.Json;
using System.Text.Json.Serialization;
using Acode.Cli.Events;

namespace Acode.Cli.JSONL;

/// <summary>
/// Serializes events to JSON format using System.Text.Json.
/// </summary>
/// <remarks>
/// Uses snake_case naming policy and compact formatting for JSONL output.
/// </remarks>
public sealed class EventSerializer : IEventSerializer
{
    private readonly JsonSerializerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSerializer"/> class.
    /// </summary>
    /// <param name="prettyPrint">Whether to format JSON with indentation.</param>
    public EventSerializer(bool prettyPrint = false)
    {
        _options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = prettyPrint,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
    }

    /// <inheritdoc/>
    public string Serialize(BaseEvent baseEvent)
    {
        ArgumentNullException.ThrowIfNull(baseEvent);
        return JsonSerializer.Serialize<object>(baseEvent, _options);
    }
}

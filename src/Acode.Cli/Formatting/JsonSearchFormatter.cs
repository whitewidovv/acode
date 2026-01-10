// src/Acode.Cli/Formatting/JsonSearchFormatter.cs
namespace Acode.Cli.Formatting;

using System.Text.Json;
using Acode.Domain.Search;

/// <summary>
/// Formats search results as JSON.
/// </summary>
public sealed class JsonSearchFormatter : IOutputFormatter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <inheritdoc/>
    public void WriteSearchResults(SearchResults results, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(results);
        ArgumentNullException.ThrowIfNull(output);

        var json = JsonSerializer.Serialize(results, Options);
        output.WriteLine(json);
    }
}

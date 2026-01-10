// src/Acode.Cli/Formatting/IOutputFormatter.cs
namespace Acode.Cli.Formatting;

using Acode.Domain.Search;

/// <summary>
/// Interface for formatting search results output.
/// </summary>
public interface IOutputFormatter
{
    /// <summary>
    /// Writes search results to the specified output writer.
    /// </summary>
    /// <param name="results">The search results to format.</param>
    /// <param name="output">The output writer.</param>
    void WriteSearchResults(SearchResults results, TextWriter output);
}

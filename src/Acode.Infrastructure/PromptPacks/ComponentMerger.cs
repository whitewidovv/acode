using System.Text;
using System.Text.RegularExpressions;
using Acode.Domain.PromptPacks;

namespace Acode.Infrastructure.PromptPacks;

/// <summary>
/// Merges pack components with conflict resolution and context filtering.
/// </summary>
public sealed partial class ComponentMerger
{
    private readonly bool _deduplicateHeadings;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComponentMerger"/> class.
    /// </summary>
    /// <param name="deduplicateHeadings">Whether to remove duplicate markdown headings.</param>
    public ComponentMerger(bool deduplicateHeadings = true)
    {
        _deduplicateHeadings = deduplicateHeadings;
    }

    /// <summary>
    /// Merges components into a single prompt string, filtering by context and handling overrides.
    /// </summary>
    /// <param name="components">The components to merge.</param>
    /// <param name="context">The composition context for filtering.</param>
    /// <returns>The merged prompt content.</returns>
    public string Merge(
        IReadOnlyList<LoadedComponent> components,
        CompositionContext context)
    {
        if (components == null || components.Count == 0)
        {
            return string.Empty;
        }

        var filtered = FilterByContext(components, context);
        var ordered = OrderByPrecedence(filtered);
        var merged = MergeWithOverrides(ordered);

        if (_deduplicateHeadings)
        {
            merged = RemoveDuplicateHeadings(merged);
        }

        return merged;
    }

    [GeneratedRegex(@"^#\s+OVERRIDE:\s*(.+)$", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex OverrideHeadingPattern();

    private static List<LoadedComponent> FilterByContext(
        IReadOnlyList<LoadedComponent> components,
        CompositionContext context)
    {
        return components.Where(c =>
        {
            if (c.Type == ComponentType.System)
            {
                return true;
            }

            if (c.Type == ComponentType.Role)
            {
                var role = GetMetadataValue(c, "role");
                return string.Equals(role, context.Role, StringComparison.OrdinalIgnoreCase);
            }

            if (c.Type == ComponentType.Language)
            {
                var language = GetMetadataValue(c, "language");
                return string.Equals(language, context.Language, StringComparison.OrdinalIgnoreCase);
            }

            if (c.Type == ComponentType.Framework)
            {
                var framework = GetMetadataValue(c, "framework");
                return string.Equals(framework, context.Framework, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }).ToList();
    }

    private static string? GetMetadataValue(LoadedComponent component, string key)
    {
        if (component.Metadata is null)
        {
            return null;
        }

        return component.Metadata.TryGetValue(key, out var value) ? value : null;
    }

    private static List<LoadedComponent> OrderByPrecedence(List<LoadedComponent> components)
    {
        // Order: System → Role → Language → Framework
        return components.OrderBy(c => c.Type switch
        {
            ComponentType.System => 1,
            ComponentType.Role => 2,
            ComponentType.Language => 3,
            ComponentType.Framework => 4,
            _ => 99,
        }).ToList();
    }

    private static string MergeWithOverrides(List<LoadedComponent> components)
    {
        if (components.Count == 0)
        {
            return string.Empty;
        }

        // Collect all override sections from non-system components
        var overrideSections = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var component in components)
        {
            if (component.Type == ComponentType.System)
            {
                continue;
            }

            var content = component.Content ?? string.Empty;
            var overrideMatches = OverrideHeadingPattern().Matches(content);
            foreach (Match match in overrideMatches)
            {
                var sectionName = match.Groups[1].Value.Trim();
                var overrideContent = ExtractSectionContent(content, match);
                overrideSections[$"OVERRIDE:{sectionName}"] = overrideContent;
            }
        }

        var hasOverrides = overrideSections.Count > 0;
        var builder = new StringBuilder();

        // Process components with override handling
        foreach (var component in components)
        {
            var content = component.Content?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(content))
            {
                continue;
            }

            if (hasOverrides && component.Type == ComponentType.System)
            {
                // Apply overrides to system content
                content = ApplyOverrides(content, overrideSections);
            }
            else if (OverrideHeadingPattern().IsMatch(content))
            {
                // Skip the override markers themselves, they've been applied
                content = RemoveOverrideSections(content);
                if (string.IsNullOrWhiteSpace(content))
                {
                    continue;
                }
            }

            if (builder.Length > 0)
            {
                builder.Append("\n\n");
            }

            builder.Append(content);
        }

        return builder.ToString();
    }

    private static string ExtractSectionContent(string content, Match overrideMatch)
    {
        // Extract content after the OVERRIDE heading until the next heading or end
        var startIndex = overrideMatch.Index + overrideMatch.Length;
        var remaining = content[startIndex..];

        // Find next heading (# at line start)
        var nextHeadingMatch = HeadingPattern().Match(remaining);
        if (nextHeadingMatch.Success)
        {
            return remaining[..nextHeadingMatch.Index].Trim();
        }

        return remaining.Trim();
    }

    private static string ApplyOverrides(string systemContent, Dictionary<string, string> overrides)
    {
        var lines = systemContent.Split('\n');
        var result = new List<string>();
        var skipUntilNextHeading = false;

        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith('#'))
            {
                // Extract heading text (remove # and leading space)
                var headingText = trimmed.TrimStart('#').Trim();
                var overrideKey = $"OVERRIDE:{headingText}";

                if (overrides.TryGetValue(overrideKey, out var replacement))
                {
                    // Replace this section
                    result.Add(line); // Keep the heading
                    result.Add(string.Empty);
                    result.Add(replacement);
                    skipUntilNextHeading = true;
                    continue;
                }
                else
                {
                    skipUntilNextHeading = false;
                }
            }

            if (!skipUntilNextHeading)
            {
                result.Add(line);
            }
        }

        return string.Join("\n", result);
    }

    private static string RemoveOverrideSections(string content)
    {
        var lines = content.Split('\n');
        var result = new List<string>();
        var skipSection = false;

        foreach (var line in lines)
        {
            if (OverrideHeadingPattern().IsMatch(line))
            {
                skipSection = true;
                continue;
            }

            // Check if we hit a non-override heading
            if (skipSection && HeadingPattern().IsMatch(line) && !OverrideHeadingPattern().IsMatch(line))
            {
                skipSection = false;
            }

            if (!skipSection)
            {
                result.Add(line);
            }
        }

        return string.Join("\n", result).Trim();
    }

    private static string RemoveDuplicateHeadings(string content)
    {
        var lines = content.Split('\n');
        var seenHeadings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();

        foreach (var line in lines)
        {
            var trimmedLine = line.TrimStart();
            if (trimmedLine.StartsWith('#'))
            {
                var heading = trimmedLine.TrimStart('#').Trim();
                if (seenHeadings.Contains(heading))
                {
                    continue;
                }

                seenHeadings.Add(heading);
            }

            result.Add(line);
        }

        return string.Join("\n", result);
    }

    [GeneratedRegex(@"^(#+\s+.+)$", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex HeadingPattern();
}

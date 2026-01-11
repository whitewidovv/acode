using Acode.Domain.Commands;

namespace Acode.Application.Commands;

/// <summary>
/// Parses command specifications from configuration into CommandSpec objects.
/// Supports string, array, and object formats per Task 002.c FR-002c-31 through FR-002c-50.
/// </summary>
public sealed class CommandParser : ICommandParser
{
    /// <inheritdoc/>
    public IReadOnlyList<CommandSpec> Parse(object commandValue)
    {
        ArgumentNullException.ThrowIfNull(commandValue);

        return commandValue switch
        {
            string str => new[] { ParseString(str) },
            IEnumerable<object> array => ParseArray(array),
            IDictionary<string, object> dict => new[] { ParseObject(dict) },
            _ => throw new ArgumentException(
                $"Unsupported command format: {commandValue.GetType().Name}. " +
                "Expected string, array, or object.")
        };
    }

    /// <inheritdoc/>
    public CommandSpec ParseString(string command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var trimmed = command.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new ArgumentException("Command cannot be empty or whitespace-only.", nameof(command));
        }

        return new CommandSpec
        {
            Run = trimmed
        };
    }

    /// <inheritdoc/>
    public IReadOnlyList<CommandSpec> ParseArray(IEnumerable<object> commands)
    {
        ArgumentNullException.ThrowIfNull(commands);

        var result = new List<CommandSpec>();

        foreach (var cmd in commands)
        {
            if (cmd is string str)
            {
                result.Add(ParseString(str));
            }
            else if (cmd is IDictionary<string, object> dict)
            {
                result.Add(ParseObject(dict));
            }
            else
            {
                throw new ArgumentException(
                    $"Array element has unsupported type: {cmd.GetType().Name}. " +
                    "Expected string or object.");
            }
        }

        return result;
    }

    /// <inheritdoc/>
    public CommandSpec ParseObject(IDictionary<string, object> commandObject)
    {
        ArgumentNullException.ThrowIfNull(commandObject);

        if (!commandObject.TryGetValue("run", out var runValue) || runValue is not string run)
        {
            throw new ArgumentException(
                "The 'run' property is required and must be a string.",
                nameof(commandObject));
        }

        var trimmedRun = run.Trim();
        if (string.IsNullOrWhiteSpace(trimmedRun))
        {
            throw new ArgumentException(
                "The 'run' property cannot be empty or whitespace-only.",
                nameof(commandObject));
        }

        return new CommandSpec
        {
            Run = trimmedRun,
            Cwd = GetStringValue(commandObject, "cwd") ?? ".",
            Timeout = GetIntValue(commandObject, "timeout") ?? 300,
            Retry = GetIntValue(commandObject, "retry") ?? 0,
            ContinueOnError = GetBoolValue(commandObject, "continue_on_error") ?? false,
            Env = GetDictionaryValue(commandObject, "env") ?? new Dictionary<string, string>(),
            Platforms = GetDictionaryValue(commandObject, "platforms")
        };
    }

    private static string? GetStringValue(IDictionary<string, object> dict, string key)
    {
        if (dict.TryGetValue(key, out var value) && value is string str)
        {
            return str;
        }

        return null;
    }

    private static int? GetIntValue(IDictionary<string, object> dict, string key)
    {
        if (dict.TryGetValue(key, out var value))
        {
            return value switch
            {
                int i => i,
                long l => (int)l,
                double d => (int)d,
                string s when int.TryParse(s, out var parsed) => parsed,
                _ => null
            };
        }

        return null;
    }

    private static bool? GetBoolValue(IDictionary<string, object> dict, string key)
    {
        if (dict.TryGetValue(key, out var value))
        {
            return value switch
            {
                bool b => b,
                string s when bool.TryParse(s, out var parsed) => parsed,
                _ => null
            };
        }

        return null;
    }

    private static IReadOnlyDictionary<string, string>? GetDictionaryValue(
        IDictionary<string, object> dict,
        string key)
    {
        if (!dict.TryGetValue(key, out var value))
        {
            return null;
        }

        if (value is not IDictionary<string, object> innerDict)
        {
            return null;
        }

        var result = new Dictionary<string, string>();
        foreach (var (k, v) in innerDict)
        {
            if (v is string str)
            {
                result[k] = str;
            }
        }

        return result;
    }
}

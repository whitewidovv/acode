namespace Acode.Infrastructure.Common;

using System.Text;
using System.Text.Json;

/// <summary>
/// JSON naming policy that converts property names to snake_case.
/// Example: "PropertyName" -> "property_name".
/// </summary>
public sealed class SnakeCaseNamingPolicy : JsonNamingPolicy
{
    /// <inheritdoc/>
    public override string ConvertName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var builder = new StringBuilder();
        var previousWasUpper = false;
        var previousWasUnderscore = false;

        for (int i = 0; i < name.Length; i++)
        {
            var currentChar = name[i];
            var isUpper = char.IsUpper(currentChar);
            var isUnderscore = currentChar == '_';

            // Skip if this is an underscore
            if (isUnderscore)
            {
                builder.Append('_');
                previousWasUnderscore = true;
                previousWasUpper = false;
                continue;
            }

            // Add underscore before uppercase letter if:
            // 1. Not at the start
            // 2. Not after an underscore
            // 3. Previous was lowercase OR (previous was uppercase AND next is lowercase)
            if (isUpper && i > 0 && !previousWasUnderscore)
            {
                // Check if we're in a sequence of uppercase letters
                // Only add underscore if:
                // - Previous was lowercase (aBc -> a_bc)
                // - We're transitioning from uppercase sequence to lowercase (ABCDef -> abc_def)
                if (!previousWasUpper)
                {
                    // Previous was lowercase, add underscore
                    builder.Append('_');
                }
                else if (i + 1 < name.Length && char.IsLower(name[i + 1]))
                {
                    // In uppercase sequence, but next is lowercase (ABCDef -> ABC_Def)
                    builder.Append('_');
                }
            }

            builder.Append(char.ToLowerInvariant(currentChar));
            previousWasUpper = isUpper;
            previousWasUnderscore = false;
        }

        return builder.ToString();
    }
}

using System.Collections;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Acode.Infrastructure.Configuration;

/// <summary>
/// YamlDotNet node deserializer that handles IReadOnlyList and IReadOnlyCollection.
/// Converts YAML sequences to read-only collections.
/// </summary>
public sealed class ReadOnlyCollectionNodeDeserializer : INodeDeserializer
{
    public bool Deserialize(IParser reader, Type expectedType, Func<IParser, Type, object?> nestedObjectDeserializer, out object? value, ObjectDeserializer rootDeserializer)
    {
        ArgumentNullException.ThrowIfNull(expectedType);
        ArgumentNullException.ThrowIfNull(nestedObjectDeserializer);

        if (expectedType.IsGenericType)
        {
            var genericTypeDefinition = expectedType.GetGenericTypeDefinition();

            if (genericTypeDefinition == typeof(IReadOnlyList<>) || genericTypeDefinition == typeof(IReadOnlyCollection<>))
            {
                var itemType = expectedType.GetGenericArguments()[0];
                var listType = typeof(List<>).MakeGenericType(itemType);

                var tempValue = nestedObjectDeserializer(reader, listType);
                if (tempValue is IList list)
                {
                    var readOnlyListType = typeof(System.Collections.ObjectModel.ReadOnlyCollection<>).MakeGenericType(itemType);
                    value = Activator.CreateInstance(readOnlyListType, list);
                    return true;
                }

                value = null;
                return false;
            }
        }

        value = null;
        return false;
    }
}

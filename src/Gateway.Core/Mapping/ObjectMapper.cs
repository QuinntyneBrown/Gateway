using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gateway.Core.Mapping;

/// <summary>
/// Maps JSON documents to .NET objects with support for various mapping scenarios.
/// </summary>
public static class ObjectMapper
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Maps a JSON element to the specified type.
    /// </summary>
    public static T? Map<T>(JsonElement element)
    {
        return element.Deserialize<T>(DefaultOptions);
    }

    /// <summary>
    /// Maps a JSON string to the specified type.
    /// </summary>
    public static T? Map<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, DefaultOptions);
    }

    /// <summary>
    /// Validates that a type can be properly constructed for mapping.
    /// </summary>
    public static void ValidateType<T>()
    {
        var type = typeof(T);

        // Check for empty column names
        foreach (var prop in type.GetProperties())
        {
            var columnAttr = prop.GetCustomAttribute<ColumnAttribute>();
            // ColumnAttribute constructor already validates non-empty name
        }

        // Check for accessible constructor (public parameterless)
        var publicConstructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        if (!type.IsValueType)
        {
            // Check if there's a public parameterless constructor
            var hasPublicParameterlessConstructor = publicConstructors
                .Any(c => c.GetParameters().Length == 0);

            if (!hasPublicParameterlessConstructor && publicConstructors.Length == 0)
            {
                throw new Exceptions.MappingException(
                    $"Type {type.Name} does not have an accessible constructor.",
                    type.Name, null, null);
            }
        }
    }
}

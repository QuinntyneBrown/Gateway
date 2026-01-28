namespace Gateway.Core.Mapping;

/// <summary>
/// Specifies the column name to map a property to/from in the JSON document.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class ColumnAttribute : Attribute
{
    /// <summary>
    /// The column name in the JSON document.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Creates a new ColumnAttribute with the specified column name.
    /// </summary>
    /// <param name="name">The column name. Cannot be empty.</param>
    public ColumnAttribute(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new InvalidOperationException("Column name cannot be null or empty.");
        }
        Name = name;
    }
}

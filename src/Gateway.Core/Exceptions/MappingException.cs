namespace Gateway.Core.Exceptions;

/// <summary>
/// Exception thrown when object mapping fails.
/// </summary>
public class MappingException : Exception
{
    /// <summary>
    /// The target type that was being mapped to.
    /// </summary>
    public string? TargetType { get; }

    /// <summary>
    /// The property name that caused the mapping failure.
    /// </summary>
    public string? PropertyName { get; }

    /// <summary>
    /// The value that could not be mapped.
    /// </summary>
    public object? Value { get; }

    public MappingException(string message)
        : base(message)
    {
    }

    public MappingException(string message, string? targetType, string? propertyName, object? value)
        : base(message)
    {
        TargetType = targetType;
        PropertyName = propertyName;
        Value = value;
    }

    public MappingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public MappingException(string message, string? targetType, string? propertyName, object? value, Exception innerException)
        : base(message, innerException)
    {
        TargetType = targetType;
        PropertyName = propertyName;
        Value = value;
    }
}

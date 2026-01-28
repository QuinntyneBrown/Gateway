namespace Gateway.Core.Mapping;

/// <summary>
/// Specifies that a property should be ignored during mapping.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class IgnoreAttribute : Attribute
{
}

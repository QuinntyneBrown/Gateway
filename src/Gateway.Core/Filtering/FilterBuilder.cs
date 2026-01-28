using System.Text;

namespace Gateway.Core.Filtering;

/// <summary>
/// Fluent builder for constructing SQL++ WHERE clauses.
/// </summary>
public class FilterBuilder<T>
{
    private readonly List<string> _conditions = new();
    private readonly Dictionary<string, object?> _parameters = new();
    private string _orderBy = string.Empty;
    private int? _limit;
    private int? _offset;

    /// <summary>
    /// Adds an equality condition: property = value
    /// </summary>
    public FilterBuilder<T> Where(string property, object? value)
    {
        var paramName = $"p{_parameters.Count}";
        _conditions.Add($"{property} = ${paramName}");
        _parameters[paramName] = value;
        return this;
    }

    /// <summary>
    /// Adds a greater than condition: property > value
    /// </summary>
    public FilterBuilder<T> WhereGreaterThan(string property, object value)
    {
        var paramName = $"p{_parameters.Count}";
        _conditions.Add($"{property} > ${paramName}");
        _parameters[paramName] = value;
        return this;
    }

    /// <summary>
    /// Adds a less than condition: property < value
    /// </summary>
    public FilterBuilder<T> WhereLessThan(string property, object value)
    {
        var paramName = $"p{_parameters.Count}";
        _conditions.Add($"{property} < ${paramName}");
        _parameters[paramName] = value;
        return this;
    }

    /// <summary>
    /// Adds a greater than or equal condition: property >= value
    /// </summary>
    public FilterBuilder<T> WhereGreaterThanOrEqual(string property, object value)
    {
        var paramName = $"p{_parameters.Count}";
        _conditions.Add($"{property} >= ${paramName}");
        _parameters[paramName] = value;
        return this;
    }

    /// <summary>
    /// Adds a less than or equal condition: property <= value
    /// </summary>
    public FilterBuilder<T> WhereLessThanOrEqual(string property, object value)
    {
        var paramName = $"p{_parameters.Count}";
        _conditions.Add($"{property} <= ${paramName}");
        _parameters[paramName] = value;
        return this;
    }

    /// <summary>
    /// Adds a not equal condition: property != value
    /// </summary>
    public FilterBuilder<T> WhereNotEqual(string property, object value)
    {
        var paramName = $"p{_parameters.Count}";
        _conditions.Add($"{property} != ${paramName}");
        _parameters[paramName] = value;
        return this;
    }

    /// <summary>
    /// Adds a LIKE condition for pattern matching.
    /// </summary>
    public FilterBuilder<T> WhereLike(string property, string pattern)
    {
        var paramName = $"p{_parameters.Count}";
        _conditions.Add($"{property} LIKE ${paramName}");
        _parameters[paramName] = pattern;
        return this;
    }

    /// <summary>
    /// Adds a CONTAINS condition for substring matching.
    /// </summary>
    public FilterBuilder<T> WhereContains(string property, string value)
    {
        var paramName = $"p{_parameters.Count}";
        _conditions.Add($"CONTAINS({property}, ${paramName})");
        _parameters[paramName] = value;
        return this;
    }

    /// <summary>
    /// Adds an IN condition: property IN [values]
    /// </summary>
    public FilterBuilder<T> WhereIn(string property, IEnumerable<object> values)
    {
        var valuesList = values.ToList();
        if (valuesList.Count == 0)
        {
            _conditions.Add("FALSE");
            return this;
        }
        var paramName = $"p{_parameters.Count}";
        _conditions.Add($"{property} IN ${paramName}");
        _parameters[paramName] = valuesList;
        return this;
    }

    /// <summary>
    /// Adds a NOT IN condition: property NOT IN [values]
    /// </summary>
    public FilterBuilder<T> WhereNotIn(string property, IEnumerable<object> values)
    {
        var paramName = $"p{_parameters.Count}";
        _conditions.Add($"{property} NOT IN ${paramName}");
        _parameters[paramName] = values.ToList();
        return this;
    }

    /// <summary>
    /// Adds a raw SQL++ condition with optional parameters.
    /// </summary>
    public FilterBuilder<T> WhereRaw(string rawCondition, object? parameters = null)
    {
        _conditions.Add(rawCondition);
        if (parameters != null)
        {
            var props = parameters.GetType().GetProperties();
            foreach (var prop in props)
            {
                _parameters[prop.Name] = prop.GetValue(parameters);
            }
        }
        return this;
    }

    /// <summary>
    /// Adds an IS NULL condition.
    /// </summary>
    public FilterBuilder<T> WhereNull(string property)
    {
        _conditions.Add($"{property} IS NULL");
        return this;
    }

    /// <summary>
    /// Adds an IS NOT NULL condition.
    /// </summary>
    public FilterBuilder<T> WhereNotNull(string property)
    {
        _conditions.Add($"{property} IS NOT NULL");
        return this;
    }

    /// <summary>
    /// Adds a BETWEEN condition.
    /// </summary>
    public FilterBuilder<T> WhereBetween(string property, object min, object max)
    {
        var minParam = $"p{_parameters.Count}";
        _parameters[minParam] = min;
        var maxParam = $"p{_parameters.Count}";
        _parameters[maxParam] = max;
        _conditions.Add($"{property} BETWEEN ${minParam} AND ${maxParam}");
        return this;
    }

    /// <summary>
    /// Adds ORDER BY clause.
    /// </summary>
    public FilterBuilder<T> OrderBy(string property, bool descending = false)
    {
        _orderBy = descending ? $" ORDER BY {property} DESC" : $" ORDER BY {property}";
        return this;
    }

    /// <summary>
    /// Adds LIMIT clause for pagination.
    /// </summary>
    public FilterBuilder<T> Take(int count)
    {
        _limit = count;
        return this;
    }

    /// <summary>
    /// Adds OFFSET clause for pagination.
    /// </summary>
    public FilterBuilder<T> Skip(int count)
    {
        _offset = count;
        return this;
    }

    /// <summary>
    /// Builds the WHERE clause (without the WHERE keyword).
    /// </summary>
    public string BuildWhereClause()
    {
        if (_conditions.Count == 0)
            return string.Empty;

        return string.Join(" AND ", _conditions);
    }

    /// <summary>
    /// Builds the complete query suffix (WHERE + ORDER BY + LIMIT + OFFSET).
    /// </summary>
    public string Build()
    {
        var sb = new StringBuilder();

        if (_conditions.Count > 0)
        {
            sb.Append(" WHERE ");
            sb.Append(string.Join(" AND ", _conditions));
        }

        sb.Append(_orderBy);

        if (_limit.HasValue)
        {
            sb.Append($" LIMIT {_limit.Value}");
        }

        if (_offset.HasValue)
        {
            sb.Append($" OFFSET {_offset.Value}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Gets the parameters dictionary.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Parameters => _parameters;

    /// <summary>
    /// Returns true if no conditions have been added.
    /// </summary>
    public bool IsEmpty => _conditions.Count == 0;
}

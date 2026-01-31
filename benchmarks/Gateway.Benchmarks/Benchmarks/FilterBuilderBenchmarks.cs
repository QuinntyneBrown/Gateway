using BenchmarkDotNet.Attributes;
using Gateway.Benchmarks.Models;
using Gateway.Core.Filtering;

namespace Gateway.Benchmarks.Benchmarks;

/// <summary>
/// Benchmarks for FilterBuilder operations.
/// </summary>
[MemoryDiagnoser]
[RankColumn]
public class FilterBuilderBenchmarks
{
    private List<object> _inValues = null!;
    private List<object> _largeInValues = null!;

    [GlobalSetup]
    public void Setup()
    {
        _inValues = new List<object> { "electronics", "books", "clothing", "home", "sports" };
        _largeInValues = Enumerable.Range(1, 100).Select(i => (object)$"category-{i}").ToList();
    }

    [Benchmark(Description = "Single Where equality")]
    public string SingleWhere()
    {
        var filter = new FilterBuilder<Product>();
        filter.Where("status", "active");
        return filter.Build();
    }

    [Benchmark(Description = "Where with 5 equality conditions")]
    public string MultipleWhere()
    {
        var filter = new FilterBuilder<Product>();
        filter.Where("status", "active")
              .Where("category", "electronics")
              .Where("brand", "Acme")
              .Where("inStock", true)
              .Where("featured", true);
        return filter.Build();
    }

    [Benchmark(Description = "WhereGreaterThan comparison")]
    public string WhereGreaterThan()
    {
        var filter = new FilterBuilder<Product>();
        filter.WhereGreaterThan("price", 100);
        return filter.Build();
    }

    [Benchmark(Description = "WhereLessThan comparison")]
    public string WhereLessThan()
    {
        var filter = new FilterBuilder<Product>();
        filter.WhereLessThan("stock", 50);
        return filter.Build();
    }

    [Benchmark(Description = "WhereGreaterThanOrEqual comparison")]
    public string WhereGreaterThanOrEqual()
    {
        var filter = new FilterBuilder<Product>();
        filter.WhereGreaterThanOrEqual("rating", 4.5);
        return filter.Build();
    }

    [Benchmark(Description = "WhereLessThanOrEqual comparison")]
    public string WhereLessThanOrEqual()
    {
        var filter = new FilterBuilder<Product>();
        filter.WhereLessThanOrEqual("price", 999.99);
        return filter.Build();
    }

    [Benchmark(Description = "WhereNotEqual comparison")]
    public string WhereNotEqual()
    {
        var filter = new FilterBuilder<Product>();
        filter.WhereNotEqual("status", "deleted");
        return filter.Build();
    }

    [Benchmark(Description = "WhereLike pattern matching")]
    public string WhereLike()
    {
        var filter = new FilterBuilder<Product>();
        filter.WhereLike("name", "%phone%");
        return filter.Build();
    }

    [Benchmark(Description = "WhereContains substring")]
    public string WhereContains()
    {
        var filter = new FilterBuilder<Product>();
        filter.WhereContains("description", "wireless");
        return filter.Build();
    }

    [Benchmark(Description = "WhereIn with 5 values")]
    public string WhereIn()
    {
        var filter = new FilterBuilder<Product>();
        filter.WhereIn("category", _inValues);
        return filter.Build();
    }

    [Benchmark(Description = "WhereIn with 100 values")]
    public string WhereInLarge()
    {
        var filter = new FilterBuilder<Product>();
        filter.WhereIn("category", _largeInValues);
        return filter.Build();
    }

    [Benchmark(Description = "WhereNotIn with 5 values")]
    public string WhereNotIn()
    {
        var filter = new FilterBuilder<Product>();
        filter.WhereNotIn("status", _inValues);
        return filter.Build();
    }

    [Benchmark(Description = "WhereNull check")]
    public string WhereNull()
    {
        var filter = new FilterBuilder<Product>();
        filter.WhereNull("deletedAt");
        return filter.Build();
    }

    [Benchmark(Description = "WhereNotNull check")]
    public string WhereNotNull()
    {
        var filter = new FilterBuilder<Product>();
        filter.WhereNotNull("email");
        return filter.Build();
    }

    [Benchmark(Description = "WhereBetween range")]
    public string WhereBetween()
    {
        var filter = new FilterBuilder<Product>();
        filter.WhereBetween("price", 10, 100);
        return filter.Build();
    }

    [Benchmark(Description = "WhereRaw custom condition")]
    public string WhereRaw()
    {
        var filter = new FilterBuilder<Product>();
        filter.WhereRaw("ARRAY_LENGTH(tags) > $minTags", new { minTags = 3 });
        return filter.Build();
    }

    [Benchmark(Description = "OrderBy ascending")]
    public string OrderByAsc()
    {
        var filter = new FilterBuilder<Product>();
        filter.OrderBy("name");
        return filter.Build();
    }

    [Benchmark(Description = "OrderBy descending")]
    public string OrderByDesc()
    {
        var filter = new FilterBuilder<Product>();
        filter.OrderBy("createdAt", descending: true);
        return filter.Build();
    }

    [Benchmark(Description = "Skip and Take pagination")]
    public string SkipTake()
    {
        var filter = new FilterBuilder<Product>();
        filter.Skip(100).Take(25);
        return filter.Build();
    }

    [Benchmark(Description = "Complex filter with multiple conditions")]
    public string ComplexFilter()
    {
        var filter = new FilterBuilder<Product>();
        filter.Where("status", "active")
              .WhereGreaterThan("price", 50)
              .WhereLessThan("price", 500)
              .WhereIn("category", _inValues)
              .WhereNotNull("description")
              .WhereLike("name", "%Pro%")
              .OrderBy("price", descending: true)
              .Skip(0)
              .Take(20);
        return filter.Build();
    }

    [Benchmark(Description = "Build WHERE clause only")]
    public string BuildWhereClauseOnly()
    {
        var filter = new FilterBuilder<Product>();
        filter.Where("status", "active")
              .WhereGreaterThan("price", 50)
              .WhereLessThan("price", 500);
        return filter.BuildWhereClause();
    }

    [Benchmark(Description = "Access Parameters dictionary")]
    public IReadOnlyDictionary<string, object?> AccessParameters()
    {
        var filter = new FilterBuilder<Product>();
        filter.Where("status", "active")
              .WhereGreaterThan("price", 50)
              .WhereIn("category", _inValues);
        return filter.Parameters;
    }

    [Benchmark(Description = "Check IsEmpty property")]
    public bool CheckIsEmpty()
    {
        var filter = new FilterBuilder<Product>();
        return filter.IsEmpty;
    }

    [Benchmark(Description = "Chain 10 conditions")]
    public string ChainManyConditions()
    {
        var filter = new FilterBuilder<Product>();
        for (int i = 0; i < 10; i++)
        {
            filter.Where($"field{i}", $"value{i}");
        }
        return filter.Build();
    }

    [Benchmark(Description = "Chain 50 conditions")]
    public string ChainVeryManyConditions()
    {
        var filter = new FilterBuilder<Product>();
        for (int i = 0; i < 50; i++)
        {
            filter.Where($"field{i}", $"value{i}");
        }
        return filter.Build();
    }
}

using BenchmarkDotNet.Attributes;
using Gateway.Benchmarks.Models;
using Gateway.Core.Pagination;

namespace Gateway.Benchmarks.Benchmarks;

/// <summary>
/// Benchmarks for Pagination operations.
/// </summary>
[MemoryDiagnoser]
[RankColumn]
public class PaginationBenchmarks
{
    private List<SimpleUser> _smallList = null!;
    private List<SimpleUser> _mediumList = null!;
    private List<SimpleUser> _largeList = null!;
    private List<ComplexUser> _complexList = null!;
    private PaginationOptions _defaultOptions = null!;
    private PaginationOptions _customOptions = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Small list (25 items - 1 page)
        _smallList = Enumerable.Range(1, 25).Select(i => new SimpleUser
        {
            Id = $"user-{i:000}",
            Name = $"User {i}",
            Email = $"user{i}@example.com",
            Age = 20 + (i % 50),
            IsActive = i % 2 == 0
        }).ToList();

        // Medium list (250 items - 10 pages)
        _mediumList = Enumerable.Range(1, 250).Select(i => new SimpleUser
        {
            Id = $"user-{i:000}",
            Name = $"User {i}",
            Email = $"user{i}@example.com",
            Age = 20 + (i % 50),
            IsActive = i % 2 == 0
        }).ToList();

        // Large list (1000 items - 40 pages)
        _largeList = Enumerable.Range(1, 1000).Select(i => new SimpleUser
        {
            Id = $"user-{i:0000}",
            Name = $"User {i}",
            Email = $"user{i}@example.com",
            Age = 20 + (i % 50),
            IsActive = i % 2 == 0
        }).ToList();

        // Complex user list
        _complexList = Enumerable.Range(1, 100).Select(i => new ComplexUser
        {
            Id = $"user-{i:000}",
            FirstName = $"First{i}",
            LastName = $"Last{i}",
            Email = $"user{i}@example.com",
            Age = 20 + (i % 50),
            IsActive = i % 2 == 0,
            CreatedAt = DateTime.UtcNow.AddDays(-i),
            Address = new Address
            {
                Street = $"{i} Main St",
                City = "City",
                State = "ST",
                ZipCode = $"{10000 + i}",
                Country = "USA"
            },
            Tags = new List<string> { "tag1", "tag2" }
        }).ToList();

        _defaultOptions = new PaginationOptions();
        _customOptions = new PaginationOptions
        {
            DefaultPageSize = 50,
            MaxPageSize = 200
        };
    }

    // PagedResult creation benchmarks

    [Benchmark(Description = "Create PagedResult with 25 items")]
    public PagedResult<SimpleUser> CreatePagedResultSmall()
    {
        return new PagedResult<SimpleUser>(_smallList, 1, 25, 25);
    }

    [Benchmark(Description = "Create PagedResult with 250 items")]
    public PagedResult<SimpleUser> CreatePagedResultMedium()
    {
        return new PagedResult<SimpleUser>(_mediumList, 1, 25, 250);
    }

    [Benchmark(Description = "Create PagedResult with 1000 items")]
    public PagedResult<SimpleUser> CreatePagedResultLarge()
    {
        return new PagedResult<SimpleUser>(_largeList, 1, 25, 1000);
    }

    [Benchmark(Description = "Create PagedResult with complex users")]
    public PagedResult<ComplexUser> CreatePagedResultComplex()
    {
        return new PagedResult<ComplexUser>(_complexList, 1, 25, 100);
    }

    [Benchmark(Description = "Create PagedResult without total count")]
    public PagedResult<SimpleUser> CreatePagedResultNoCount()
    {
        return new PagedResult<SimpleUser>(_smallList, 1, 25, hasMoreItems: true);
    }

    // PagedResult property access benchmarks

    [Benchmark(Description = "Access TotalPages property")]
    public int? AccessTotalPages()
    {
        var result = new PagedResult<SimpleUser>(_mediumList, 5, 25, 250);
        return result.TotalPages;
    }

    [Benchmark(Description = "Access HasPreviousPage property")]
    public bool AccessHasPreviousPage()
    {
        var result = new PagedResult<SimpleUser>(_mediumList, 5, 25, 250);
        return result.HasPreviousPage;
    }

    [Benchmark(Description = "Access HasNextPage property")]
    public bool AccessHasNextPage()
    {
        var result = new PagedResult<SimpleUser>(_mediumList, 5, 25, 250);
        return result.HasNextPage;
    }

    [Benchmark(Description = "Access HasNextPage without total count")]
    public bool AccessHasNextPageNoCount()
    {
        var result = new PagedResult<SimpleUser>(_smallList, 1, 25, hasMoreItems: true);
        return result.HasNextPage;
    }

    [Benchmark(Description = "Access all pagination metadata")]
    public (int, int, int?, bool, bool) AccessAllMetadata()
    {
        var result = new PagedResult<SimpleUser>(_mediumList, 5, 25, 250);
        return (result.PageNumber, result.PageSize, result.TotalPages, result.HasPreviousPage, result.HasNextPage);
    }

    // PaginationOptions benchmarks

    [Benchmark(Description = "GetEffectivePageSize with null")]
    public int GetEffectivePageSizeNull()
    {
        return _defaultOptions.GetEffectivePageSize(null);
    }

    [Benchmark(Description = "GetEffectivePageSize under max")]
    public int GetEffectivePageSizeUnderMax()
    {
        return _defaultOptions.GetEffectivePageSize(50);
    }

    [Benchmark(Description = "GetEffectivePageSize over max")]
    public int GetEffectivePageSizeOverMax()
    {
        return _defaultOptions.GetEffectivePageSize(2000);
    }

    [Benchmark(Description = "GetEffectivePageSize with custom options")]
    public int GetEffectivePageSizeCustom()
    {
        return _customOptions.GetEffectivePageSize(100);
    }

    // Simulated pagination workflow benchmarks

    [Benchmark(Description = "Simulate page 1 of 10")]
    public PagedResult<SimpleUser> SimulatePage1()
    {
        var pageSize = 25;
        var page = 1;
        var total = 250;
        var items = _mediumList.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new PagedResult<SimpleUser>(items, page, pageSize, total);
    }

    [Benchmark(Description = "Simulate page 5 of 10")]
    public PagedResult<SimpleUser> SimulatePage5()
    {
        var pageSize = 25;
        var page = 5;
        var total = 250;
        var items = _mediumList.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new PagedResult<SimpleUser>(items, page, pageSize, total);
    }

    [Benchmark(Description = "Simulate last page")]
    public PagedResult<SimpleUser> SimulateLastPage()
    {
        var pageSize = 25;
        var page = 10;
        var total = 250;
        var items = _mediumList.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new PagedResult<SimpleUser>(items, page, pageSize, total);
    }

    [Benchmark(Description = "Simulate large page size (100 items)")]
    public PagedResult<SimpleUser> SimulateLargePageSize()
    {
        var pageSize = 100;
        var page = 1;
        var total = 1000;
        var items = _largeList.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new PagedResult<SimpleUser>(items, page, pageSize, total);
    }

    [Benchmark(Description = "Iterate through PagedResult items")]
    public int IterateItems()
    {
        var result = new PagedResult<SimpleUser>(_mediumList.Take(25).ToList(), 1, 25, 250);
        var count = 0;
        foreach (var item in result.Items)
        {
            count++;
        }
        return count;
    }
}

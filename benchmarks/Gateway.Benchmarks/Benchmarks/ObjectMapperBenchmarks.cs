using System.Text.Json;
using BenchmarkDotNet.Attributes;
using Gateway.Benchmarks.Models;
using Gateway.Core.Mapping;

namespace Gateway.Benchmarks.Benchmarks;

/// <summary>
/// Benchmarks for ObjectMapper JSON to POCO mapping operations.
/// </summary>
[MemoryDiagnoser]
[RankColumn]
public class ObjectMapperBenchmarks
{
    private string _simpleUserJson = null!;
    private string _complexUserJson = null!;
    private string _simpleUserArrayJson = null!;
    private string _complexUserArrayJson = null!;
    private JsonElement _simpleUserElement;
    private JsonElement _complexUserElement;

    [GlobalSetup]
    public void Setup()
    {
        // Simple user JSON
        _simpleUserJson = JsonSerializer.Serialize(new SimpleUser
        {
            Id = "user-001",
            Name = "John Doe",
            Email = "john.doe@example.com",
            Age = 30,
            IsActive = true
        });

        // Complex user JSON with nested objects
        _complexUserJson = JsonSerializer.Serialize(new ComplexUser
        {
            Id = "user-001",
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Age = 30,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Address = new Address
            {
                Street = "123 Main St",
                City = "New York",
                State = "NY",
                ZipCode = "10001",
                Country = "USA"
            },
            Tags = new List<string> { "premium", "verified", "active" },
            Metadata = new Dictionary<string, string>
            {
                { "source", "web" },
                { "campaign", "summer2024" },
                { "referrer", "google" }
            },
            Orders = new List<Order>
            {
                new Order
                {
                    OrderId = "order-001",
                    Amount = 199.99m,
                    OrderDate = DateTime.UtcNow.AddDays(-7),
                    Status = "completed",
                    Items = new List<OrderItem>
                    {
                        new OrderItem { ProductId = "prod-001", ProductName = "Widget", Quantity = 2, Price = 49.99m },
                        new OrderItem { ProductId = "prod-002", ProductName = "Gadget", Quantity = 1, Price = 99.99m }
                    }
                },
                new Order
                {
                    OrderId = "order-002",
                    Amount = 59.99m,
                    OrderDate = DateTime.UtcNow.AddDays(-2),
                    Status = "pending",
                    Items = new List<OrderItem>
                    {
                        new OrderItem { ProductId = "prod-003", ProductName = "Thing", Quantity = 1, Price = 59.99m }
                    }
                }
            }
        });

        // Array of simple users
        var simpleUsers = Enumerable.Range(1, 100).Select(i => new SimpleUser
        {
            Id = $"user-{i:000}",
            Name = $"User {i}",
            Email = $"user{i}@example.com",
            Age = 20 + (i % 50),
            IsActive = i % 2 == 0
        }).ToList();
        _simpleUserArrayJson = JsonSerializer.Serialize(simpleUsers);

        // Array of complex users
        var complexUsers = Enumerable.Range(1, 50).Select(i => new ComplexUser
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
            Tags = new List<string> { "tag1", "tag2" },
            Orders = new List<Order>
            {
                new Order
                {
                    OrderId = $"order-{i}",
                    Amount = 100m + i,
                    OrderDate = DateTime.UtcNow.AddDays(-i),
                    Status = "completed",
                    Items = new List<OrderItem>
                    {
                        new OrderItem { ProductId = $"prod-{i}", ProductName = $"Product {i}", Quantity = 1, Price = 100m + i }
                    }
                }
            }
        }).ToList();
        _complexUserArrayJson = JsonSerializer.Serialize(complexUsers);

        // Parse JSON elements
        _simpleUserElement = JsonDocument.Parse(_simpleUserJson).RootElement;
        _complexUserElement = JsonDocument.Parse(_complexUserJson).RootElement;
    }

    [Benchmark(Description = "Map simple JSON string to POCO")]
    public SimpleUser? MapSimpleFromString()
    {
        return ObjectMapper.Map<SimpleUser>(_simpleUserJson);
    }

    [Benchmark(Description = "Map complex JSON string to POCO")]
    public ComplexUser? MapComplexFromString()
    {
        return ObjectMapper.Map<ComplexUser>(_complexUserJson);
    }

    [Benchmark(Description = "Map simple JsonElement to POCO")]
    public SimpleUser? MapSimpleFromElement()
    {
        return ObjectMapper.Map<SimpleUser>(_simpleUserElement);
    }

    [Benchmark(Description = "Map complex JsonElement to POCO")]
    public ComplexUser? MapComplexFromElement()
    {
        return ObjectMapper.Map<ComplexUser>(_complexUserElement);
    }

    [Benchmark(Description = "Map 100 simple users from JSON array")]
    public List<SimpleUser>? MapSimpleUserArray()
    {
        return ObjectMapper.Map<List<SimpleUser>>(_simpleUserArrayJson);
    }

    [Benchmark(Description = "Map 50 complex users from JSON array")]
    public List<ComplexUser>? MapComplexUserArray()
    {
        return ObjectMapper.Map<List<ComplexUser>>(_complexUserArrayJson);
    }

    [Benchmark(Description = "Validate simple type")]
    public void ValidateSimpleType()
    {
        ObjectMapper.ValidateType<SimpleUser>();
    }

    [Benchmark(Description = "Validate complex type")]
    public void ValidateComplexType()
    {
        ObjectMapper.ValidateType<ComplexUser>();
    }

    [Benchmark(Description = "Validate type with attributes")]
    public void ValidateTypeWithAttributes()
    {
        ObjectMapper.ValidateType<UserWithAttributes>();
    }
}

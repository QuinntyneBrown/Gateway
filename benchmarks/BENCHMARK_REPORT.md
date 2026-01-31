# Gateway.Core Performance Benchmark Report

## Overview

This report contains comprehensive performance benchmarks for the Gateway.Core library using BenchmarkDotNet v0.14.0. The benchmarks measure execution time and memory allocation for key library features including object mapping, filtering, and pagination.

**Test Environment:**
- Windows 11 (10.0.26200.7623)
- .NET SDK 10.0.101
- Runtime: .NET 9.0.11 (9.0.1125.51716), Arm64 RyuJIT AdvSIMD

---

## Summary of Key Results

### Object Mapping Performance

| Operation | Mean Time | Memory Allocated |
|-----------|-----------|------------------|
| Map simple JSON to POCO | 275 ns | 192 B |
| Map complex JSON to POCO | 2.8 µs | 2.8 KB |
| Map 100 simple objects | 24 µs | 21.3 KB |
| Map 50 complex objects | 76 µs | 70.6 KB |
| Validate simple type | 393 ns | 96 B |

### FilterBuilder Performance

| Operation | Mean Time | Memory Allocated |
|-----------|-----------|------------------|
| Single Where equality | 100 ns | 712 B |
| 5 equality conditions | 323 ns | 1.8 KB |
| Complex filter (7 conditions) | 456 ns | 2.6 KB |
| Chain 10 conditions | 858 ns | 4.1 KB |
| Chain 50 conditions | 4.3 µs | 19.5 KB |
| WhereNull check | 78 ns | 560 B |
| WhereIn (5 values) | 136 ns | 824 B |

### Pagination Performance

| Operation | Mean Time | Memory Allocated |
|-----------|-----------|------------------|
| Create PagedResult | 3-4 ns | 48 B |
| Access TotalPages | 5 ns | 48 B |
| Access HasPreviousPage | <1 ns | 0 B |
| Access HasNextPage | 4 ns | 48 B |
| GetEffectivePageSize | <1 ns | 0 B |
| Simulate page fetch (25 items) | 125 ns | 400 B |

---

## Detailed Results

### ObjectMapper Benchmarks

```
BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.7623)
.NET 9.0.11 (9.0.1125.51716), Arm64 RyuJIT AdvSIMD
```

| Method | Mean | Error | StdDev | Rank | Gen0 | Gen1 | Allocated |
|--------|------|-------|--------|------|------|------|-----------|
| Map simple JSON string to POCO | 274.9 ns | 0.79 ns | 0.66 ns | 1 | 0.0458 | - | 192 B |
| Map complex JSON string to POCO | 2,808.8 ns | 58.08 ns | 171.24 ns | 7 | 0.6714 | - | 2816 B |
| Map simple JsonElement to POCO | 341.8 ns | 19.68 ns | 58.03 ns | 2 | 0.0458 | - | 192 B |
| Map complex JsonElement to POCO | 2,516.9 ns | 49.63 ns | 88.22 ns | 6 | 0.6714 | - | 2816 B |
| Map 100 simple users from JSON array | 23,856.5 ns | 450.49 ns | 889.22 ns | 8 | 5.1880 | - | 21793 B |
| Map 50 complex users from JSON array | 76,239.6 ns | 1,478.85 ns | 2,213.47 ns | 9 | 17.2119 | 0.6104 | 72264 B |
| Validate simple type | 392.7 ns | 7.77 ns | 11.87 ns | 3 | 0.0229 | - | 96 B |
| Validate complex type | 995.5 ns | 19.86 ns | 30.92 ns | 4 | 0.0477 | - | 200 B |
| Validate type with attributes | 1,283.0 ns | 25.16 ns | 30.90 ns | 5 | 0.2060 | - | 864 B |

**Key Insights:**
- Simple object mapping is extremely fast at ~275 ns per operation
- JsonElement mapping is slightly slower than string deserialization
- Memory allocation scales linearly with object complexity
- Type validation is a one-time cost that can be cached

### FilterBuilder Benchmarks

| Method | Mean | Error | StdDev | Rank | Gen0 | Allocated |
|--------|------|-------|--------|------|------|-----------|
| Single Where equality | 99.85 ns | 1.925 ns | 2.503 ns | 5 | 0.1702 | 712 B |
| Where with 5 equality conditions | 322.61 ns | 6.467 ns | 15.985 ns | 9 | 0.4511 | 1888 B |
| WhereGreaterThan comparison | 102.01 ns | 2.074 ns | 3.230 ns | 5 | 0.1760 | 736 B |
| WhereLessThan comparison | 99.82 ns | 2.002 ns | 2.226 ns | 5 | 0.1760 | 736 B |
| WhereGreaterThanOrEqual comparison | 113.15 ns | 2.304 ns | 4.439 ns | 6 | 0.1760 | 736 B |
| WhereLessThanOrEqual comparison | 117.30 ns | 2.380 ns | 4.105 ns | 6 | 0.1760 | 736 B |
| WhereNotEqual comparison | 112.38 ns | 2.265 ns | 5.597 ns | 6 | 0.1702 | 712 B |
| WhereLike pattern matching | 110.11 ns | 2.226 ns | 5.667 ns | 6 | 0.1702 | 712 B |
| WhereContains substring | 141.19 ns | 2.466 ns | 2.307 ns | 7 | 0.1855 | 776 B |
| WhereIn with 5 values | 136.36 ns | 2.747 ns | 5.093 ns | 7 | 0.1969 | 824 B |
| WhereIn with 100 values | 169.33 ns | 3.425 ns | 8.592 ns | 8 | 0.3786 | 1584 B |
| WhereNotIn with 5 values | 133.82 ns | 2.722 ns | 6.199 ns | 7 | 0.1969 | 824 B |
| WhereNull check | 77.77 ns | 1.002 ns | 0.938 ns | 4 | 0.1339 | 560 B |
| WhereNotNull check | 77.05 ns | 0.928 ns | 0.823 ns | 4 | 0.1339 | 560 B |
| WhereBetween range | 171.58 ns | 3.220 ns | 3.012 ns | 8 | 0.2007 | 840 B |
| WhereRaw custom condition | 134.70 ns | 1.645 ns | 1.539 ns | 7 | 0.1798 | 752 B |
| OrderBy ascending | 41.63 ns | 0.195 ns | 0.152 ns | 2 | 0.0918 | 384 B |
| OrderBy descending | 66.87 ns | 0.730 ns | 0.683 ns | 3 | 0.1243 | 520 B |
| Skip and Take pagination | 64.68 ns | 1.338 ns | 1.541 ns | 3 | 0.1051 | 440 B |
| Complex filter with multiple conditions | 455.53 ns | 7.755 ns | 6.874 ns | 10 | 0.6366 | 2664 B |
| Build WHERE clause only | 171.82 ns | 0.684 ns | 0.571 ns | 8 | 0.1817 | 760 B |
| Access Parameters dictionary | 145.96 ns | 2.831 ns | 2.907 ns | 7 | 0.1740 | 728 B |
| Check IsEmpty property | 17.55 ns | 0.378 ns | 0.631 ns | 1 | 0.0401 | 168 B |
| Chain 10 conditions | 858.09 ns | 13.857 ns | 12.962 ns | 11 | 1.0004 | 4184 B |
| Chain 50 conditions | 4,268.29 ns | 80.132 ns | 117.456 ns | 12 | 4.7607 | 19912 B |

**Key Insights:**
- Simple operations (WhereNull, IsEmpty) are under 80 ns
- Adding conditions scales linearly (~85 ns per condition)
- Memory allocation is efficient with minimal Gen0 collections
- Complex queries with 7+ conditions still complete in under 500 ns

### Pagination Benchmarks

| Method | Mean | Error | StdDev | Rank | Gen0 | Allocated |
|--------|------|-------|--------|------|------|-----------|
| Create PagedResult with 25 items | 3.0075 ns | 0.1032 ns | 0.1413 ns | 4 | 0.0115 | 48 B |
| Create PagedResult with 250 items | 3.4858 ns | 0.1197 ns | 0.3530 ns | 5 | 0.0115 | 48 B |
| Create PagedResult with 1000 items | 3.6506 ns | 0.0550 ns | 0.0515 ns | 5 | 0.0115 | 48 B |
| Create PagedResult with complex users | 3.6143 ns | 0.1186 ns | 0.1165 ns | 5 | 0.0115 | 48 B |
| Create PagedResult without total count | 3.5332 ns | 0.0621 ns | 0.0519 ns | 5 | 0.0115 | 48 B |
| Access TotalPages property | 4.9694 ns | 0.1243 ns | 0.1162 ns | 7 | 0.0115 | 48 B |
| Access HasPreviousPage property | 0.0684 ns | 0.0100 ns | 0.0093 ns | 1 | - | - |
| Access HasNextPage property | 4.1731 ns | 0.0749 ns | 0.0701 ns | 6 | 0.0115 | 48 B |
| Access HasNextPage without total count | 4.3842 ns | 0.1207 ns | 0.1569 ns | 6 | 0.0115 | 48 B |
| Access all pagination metadata | 14.3370 ns | 0.3198 ns | 0.8915 ns | 8 | 0.0115 | 48 B |
| GetEffectivePageSize with null | 0.6043 ns | 0.0576 ns | 0.1624 ns | 2 | - | - |
| GetEffectivePageSize under max | 0.7385 ns | 0.0826 ns | 0.2262 ns | 2 | - | - |
| GetEffectivePageSize over max | 1.0201 ns | 0.1146 ns | 0.3380 ns | 3 | - | - |
| GetEffectivePageSize with custom options | 1.1174 ns | 0.1342 ns | 0.3916 ns | 3 | - | - |
| Simulate page 1 of 10 | 124.2704 ns | 2.5298 ns | 6.5753 ns | 9 | 0.0956 | 400 B |
| Simulate page 5 of 10 | 125.1856 ns | 2.5339 ns | 6.5860 ns | 9 | 0.0956 | 400 B |
| Simulate last page | 119.9733 ns | 2.4297 ns | 7.0104 ns | 9 | 0.0956 | 400 B |
| Simulate large page size (100 items) | 357.0123 ns | 7.1811 ns | 20.8337 ns | 11 | 0.2389 | 1000 B |
| Iterate through PagedResult items | 182.3345 ns | 3.4709 ns | 3.2467 ns | 10 | 0.0823 | 344 B |

**Key Insights:**
- PagedResult creation is extremely fast at ~3-4 ns regardless of collection size
- Property access (HasPreviousPage) is virtually instant at <1 ns with zero allocation
- PaginationOptions operations are sub-nanosecond
- Page position (first, middle, last) has no impact on performance

---

## Performance Recommendations

1. **Object Mapping**: Consider caching JsonSerializerOptions for repeated operations. Simple objects map in under 300 ns.

2. **FilterBuilder**: Build filters once and reuse when possible. Complex queries with multiple conditions are still sub-microsecond.

3. **Pagination**: PagedResult has negligible overhead. Safe to use extensively without performance concerns.

4. **Memory**: All operations have minimal Gen0 allocations. No Gen2 collections observed during benchmarks.

---

## Running Benchmarks

To run the benchmarks yourself:

```bash
cd benchmarks/Gateway.Benchmarks
dotnet run -c Release
```

To run specific benchmark classes:

```bash
dotnet run -c Release -- --filter *ObjectMapper*
dotnet run -c Release -- --filter *FilterBuilder*
dotnet run -c Release -- --filter *Pagination*
```

Results are saved to `BenchmarkDotNet.Artifacts/results/` in multiple formats (Markdown, CSV, HTML).

---

*Report generated by Gateway.Benchmarks using BenchmarkDotNet v0.14.0*

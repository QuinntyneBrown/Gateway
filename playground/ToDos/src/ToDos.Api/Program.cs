// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Couchbase;
using Couchbase.KeyValue;
using Couchbase.Query;
using Gateway.Core.Extensions;
using Gateway.Core.Filtering;
using Gateway.Core.Pagination;
using Microsoft.AspNetCore.Mvc;
using ToDos.Api.Models;

// Helper to convert FilterBuilder parameters to Couchbase QueryOptions
static QueryOptions ToQueryOptions(IReadOnlyDictionary<string, object?> parameters)
{
    var options = new QueryOptions();
    foreach (var param in parameters)
    {
        options.Parameter(param.Key, param.Value ?? DBNull.Value);
    }
    return options;
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("CorsPolicy");

// =============================================================================
// ToDo API Endpoints - Demonstrating Gateway (Couchbase SimpleMapper)
// =============================================================================

var todosApi = app.MapGroup("/api/todos")
    .WithTags("ToDos")
    .WithOpenApi();

// -----------------------------------------------------------------------------
// GET /api/todos - Get paginated ToDos (demonstrates FilterBuilder + Pagination)
// -----------------------------------------------------------------------------
todosApi.MapGet("/", async (
    IBucket bucket,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] bool? isCompleted = null,
    [FromQuery] string? category = null,
    [FromQuery] int? minPriority = null,
    [FromQuery] string? sortBy = "createdAt",
    [FromQuery] bool sortDescending = true) =>
{
    var scope = await bucket.ScopeAsync("_default");
    var collection = await bucket.CollectionAsync("_default");

    // Build dynamic filter using Gateway FilterBuilder
    var filter = new FilterBuilder<ToDo>();

    // Apply optional filters
    if (isCompleted.HasValue)
    {
        filter.Where("isCompleted", isCompleted.Value);
    }

    if (!string.IsNullOrEmpty(category))
    {
        filter.Where("category", category);
    }

    if (minPriority.HasValue)
    {
        filter.WhereGreaterThanOrEqual("priority", minPriority.Value);
    }

    // Add sorting
    filter.OrderBy(sortBy ?? "createdAt", sortDescending);

    // Build the query with pagination
    var paginationOptions = new PaginationOptions();
    var effectivePageSize = paginationOptions.GetEffectivePageSize(pageSize);
    var offset = (page - 1) * effectivePageSize;

    filter.Skip(offset).Take(effectivePageSize + 1); // Fetch one extra to determine HasNextPage

    // Build the full SQL++ query
    var whereClause = filter.Build();
    var query = $"SELECT META().id, t.* FROM `todos`.`_default`.`_default` t {whereClause}";

    // Execute query using Gateway extension methods
    var queryOptions = ToQueryOptions(filter.Parameters);
    var results = await scope.QueryToListAsync<ToDo>(query, queryOptions);

    // Determine if there are more pages
    var hasNextPage = results.Count > effectivePageSize;
    var items = hasNextPage ? results.Take(effectivePageSize).ToList() : results;

    // Build paged result
    var pagedResult = new PagedResult<ToDo>(
        items: items,
        pageNumber: page,
        pageSize: effectivePageSize,
        hasMoreItems: hasNextPage
    );

    return Results.Ok(new
    {
        pagedResult.Items,
        pagedResult.PageNumber,
        pagedResult.PageSize,
        pagedResult.HasPreviousPage,
        pagedResult.HasNextPage,
        Filter = new
        {
            IsCompleted = isCompleted,
            Category = category,
            MinPriority = minPriority,
            SortBy = sortBy,
            SortDescending = sortDescending
        }
    });
})
.WithName("GetToDos")
.WithDescription("Get paginated ToDos with optional filtering and sorting");

// -----------------------------------------------------------------------------
// GET /api/todos/count - Get ToDo counts by status (demonstrates raw query)
// -----------------------------------------------------------------------------
todosApi.MapGet("/count", async (IBucket bucket) =>
{
    var scope = await bucket.ScopeAsync("_default");

    var totalQuery = "SELECT COUNT(*) as count FROM `todos`.`_default`.`_default`";
    var completedQuery = "SELECT COUNT(*) as count FROM `todos`.`_default`.`_default` WHERE isCompleted = true";
    var pendingQuery = "SELECT COUNT(*) as count FROM `todos`.`_default`.`_default` WHERE isCompleted = false";

    var totalTask = scope.QueryFirstOrDefaultAsync<CountResult>(totalQuery);
    var completedTask = scope.QueryFirstOrDefaultAsync<CountResult>(completedQuery);
    var pendingTask = scope.QueryFirstOrDefaultAsync<CountResult>(pendingQuery);

    await Task.WhenAll(totalTask, completedTask, pendingTask);

    return Results.Ok(new
    {
        Total = totalTask.Result?.Count ?? 0,
        Completed = completedTask.Result?.Count ?? 0,
        Pending = pendingTask.Result?.Count ?? 0
    });
})
.WithName("GetToDoCount")
.WithDescription("Get ToDo counts by status");

// -----------------------------------------------------------------------------
// GET /api/todos/{id} - Get a single ToDo (demonstrates GetAsync)
// -----------------------------------------------------------------------------
todosApi.MapGet("/{id}", async (string id, IBucket bucket) =>
{
    var collection = await bucket.CollectionAsync("_default");

    var todo = await collection.GetAsync<ToDo>(id);

    if (todo is null)
    {
        return Results.NotFound(new { Message = $"ToDo with id '{id}' not found" });
    }

    return Results.Ok(todo);
})
.WithName("GetToDoById")
.WithDescription("Get a ToDo by its ID");

// -----------------------------------------------------------------------------
// POST /api/todos - Create a new ToDo (demonstrates InsertAsync)
// -----------------------------------------------------------------------------
todosApi.MapPost("/", async (CreateToDoRequest request, IBucket bucket) =>
{
    var collection = await bucket.CollectionAsync("_default");

    var id = $"todo::{Guid.NewGuid()}";
    var todo = new ToDo
    {
        Id = id,
        Title = request.Title,
        Description = request.Description,
        Category = request.Category,
        Priority = request.Priority,
        IsCompleted = false,
        CreatedAt = DateTime.UtcNow
    };

    await collection.InsertAsync(id, todo);

    return Results.Created($"/api/todos/{id}", todo);
})
.WithName("CreateToDo")
.WithDescription("Create a new ToDo");

// -----------------------------------------------------------------------------
// PUT /api/todos/{id} - Update a ToDo (demonstrates ReplaceAsync)
// -----------------------------------------------------------------------------
todosApi.MapPut("/{id}", async (string id, UpdateToDoRequest request, IBucket bucket) =>
{
    var collection = await bucket.CollectionAsync("_default");

    var existing = await collection.GetAsync<ToDo>(id);
    if (existing is null)
    {
        return Results.NotFound(new { Message = $"ToDo with id '{id}' not found" });
    }

    var updated = existing with
    {
        Title = request.Title,
        Description = request.Description,
        Category = request.Category,
        Priority = request.Priority,
        IsCompleted = request.IsCompleted,
        CompletedAt = request.IsCompleted && !existing.IsCompleted ? DateTime.UtcNow : existing.CompletedAt
    };

    await collection.ReplaceAsync(id, updated);

    return Results.Ok(updated);
})
.WithName("UpdateToDo")
.WithDescription("Update an existing ToDo");

// -----------------------------------------------------------------------------
// PATCH /api/todos/{id}/complete - Mark ToDo as complete (demonstrates UpsertAsync)
// -----------------------------------------------------------------------------
todosApi.MapPatch("/{id}/complete", async (string id, IBucket bucket) =>
{
    var collection = await bucket.CollectionAsync("_default");

    var existing = await collection.GetAsync<ToDo>(id);
    if (existing is null)
    {
        return Results.NotFound(new { Message = $"ToDo with id '{id}' not found" });
    }

    var completed = existing with
    {
        IsCompleted = true,
        CompletedAt = DateTime.UtcNow
    };

    await collection.UpsertAsync(id, completed);

    return Results.Ok(completed);
})
.WithName("CompleteToDo")
.WithDescription("Mark a ToDo as complete");

// -----------------------------------------------------------------------------
// DELETE /api/todos/{id} - Delete a ToDo (demonstrates RemoveAsync)
// -----------------------------------------------------------------------------
todosApi.MapDelete("/{id}", async (string id, IBucket bucket) =>
{
    var collection = await bucket.CollectionAsync("_default");

    var existing = await collection.GetAsync<ToDo>(id);
    if (existing is null)
    {
        return Results.NotFound(new { Message = $"ToDo with id '{id}' not found" });
    }

    await collection.RemoveAsync(id);

    return Results.NoContent();
})
.WithName("DeleteToDo")
.WithDescription("Delete a ToDo by its ID");

// -----------------------------------------------------------------------------
// GET /api/todos/search - Search ToDos (demonstrates WhereContains/WhereLike)
// -----------------------------------------------------------------------------
todosApi.MapGet("/search", async (
    IBucket bucket,
    [FromQuery] string q,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10) =>
{
    if (string.IsNullOrWhiteSpace(q))
    {
        return Results.BadRequest(new { Message = "Search query 'q' is required" });
    }

    var scope = await bucket.ScopeAsync("_default");

    var filter = new FilterBuilder<ToDo>();
    filter.WhereContains("title", q);
    filter.OrderBy("createdAt", descending: true);

    var paginationOptions = new PaginationOptions();
    var effectivePageSize = paginationOptions.GetEffectivePageSize(pageSize);
    filter.Skip((page - 1) * effectivePageSize).Take(effectivePageSize);

    var whereClause = filter.Build();
    var query = $"SELECT META().id, t.* FROM `todos`.`_default`.`_default` t {whereClause}";

    var queryOptions = ToQueryOptions(filter.Parameters);
    var results = await scope.QueryToListAsync<ToDo>(query, queryOptions);

    return Results.Ok(new
    {
        Query = q,
        Results = results,
        Page = page,
        PageSize = effectivePageSize
    });
})
.WithName("SearchToDos")
.WithDescription("Search ToDos by title");

// -----------------------------------------------------------------------------
// GET /api/todos/by-category - Get ToDos grouped by category (demonstrates filtering)
// -----------------------------------------------------------------------------
todosApi.MapGet("/by-priority/{priority:int}", async (int priority, IBucket bucket) =>
{
    var scope = await bucket.ScopeAsync("_default");

    var filter = new FilterBuilder<ToDo>();
    filter.Where("priority", priority);
    filter.WhereNotNull("category");
    filter.OrderBy("createdAt", descending: true);

    var whereClause = filter.Build();
    var query = $"SELECT META().id, t.* FROM `todos`.`_default`.`_default` t {whereClause}";

    var queryOptions = ToQueryOptions(filter.Parameters);
    var results = await scope.QueryToListAsync<ToDo>(query, queryOptions);

    return Results.Ok(new
    {
        Priority = priority,
        PriorityName = priority switch { 1 => "Low", 2 => "Medium", 3 => "High", _ => "Unknown" },
        Count = results.Count,
        Items = results
    });
})
.WithName("GetToDosByPriority")
.WithDescription("Get ToDos by priority level");

// -----------------------------------------------------------------------------
// POST /api/todos/seed - Seed sample data for testing
// -----------------------------------------------------------------------------
todosApi.MapPost("/seed", async (IBucket bucket) =>
{
    var collection = await bucket.CollectionAsync("_default");

    var sampleTodos = new[]
    {
        new ToDo { Id = "todo::1", Title = "Learn Couchbase", Description = "Study N1QL and document modeling", Category = "Learning", Priority = 3, IsCompleted = false, CreatedAt = DateTime.UtcNow.AddDays(-5) },
        new ToDo { Id = "todo::2", Title = "Build Gateway Demo", Description = "Create a sample API using Gateway SimpleMapper", Category = "Development", Priority = 3, IsCompleted = true, CreatedAt = DateTime.UtcNow.AddDays(-4), CompletedAt = DateTime.UtcNow.AddDays(-1) },
        new ToDo { Id = "todo::3", Title = "Write Tests", Description = "Add unit and integration tests", Category = "Development", Priority = 2, IsCompleted = false, CreatedAt = DateTime.UtcNow.AddDays(-3) },
        new ToDo { Id = "todo::4", Title = "Review PR", Description = "Review the pull request for pagination feature", Category = "Development", Priority = 2, IsCompleted = false, CreatedAt = DateTime.UtcNow.AddDays(-2) },
        new ToDo { Id = "todo::5", Title = "Update Documentation", Description = "Update README with new API endpoints", Category = "Documentation", Priority = 1, IsCompleted = false, CreatedAt = DateTime.UtcNow.AddDays(-1) },
        new ToDo { Id = "todo::6", Title = "Deploy to Production", Description = "Deploy the application to production environment", Category = "DevOps", Priority = 3, IsCompleted = false, CreatedAt = DateTime.UtcNow },
        new ToDo { Id = "todo::7", Title = "Setup CI/CD", Description = "Configure GitHub Actions for automated builds", Category = "DevOps", Priority = 2, IsCompleted = true, CreatedAt = DateTime.UtcNow.AddDays(-6), CompletedAt = DateTime.UtcNow.AddDays(-3) },
        new ToDo { Id = "todo::8", Title = "Performance Testing", Description = "Run load tests on the API", Category = "Testing", Priority = 1, IsCompleted = false, CreatedAt = DateTime.UtcNow.AddHours(-12) },
        new ToDo { Id = "todo::9", Title = "Code Review Guidelines", Description = "Document code review best practices", Category = "Documentation", Priority = 1, IsCompleted = false, CreatedAt = DateTime.UtcNow.AddHours(-6) },
        new ToDo { Id = "todo::10", Title = "Team Meeting", Description = "Weekly sync with the development team", Category = "Meetings", Priority = 2, IsCompleted = false, CreatedAt = DateTime.UtcNow.AddHours(-2) }
    };

    var created = 0;
    foreach (var todo in sampleTodos)
    {
        try
        {
            await collection.InsertAsync(todo.Id, todo);
            created++;
        }
        catch (Couchbase.Core.Exceptions.KeyValue.DocumentExistsException)
        {
            // Already exists, skip
        }
    }

    return Results.Ok(new { Message = $"Seeded {created} new ToDos", Total = sampleTodos.Length });
})
.WithName("SeedToDos")
.WithDescription("Seed sample ToDo data for testing");

app.Run();
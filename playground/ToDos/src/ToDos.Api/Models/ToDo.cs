// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace ToDos.Api.Models;

/// <summary>
/// Represents a ToDo item stored in Couchbase
/// </summary>
public record ToDo
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsCompleted { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; init; }
    public string? Category { get; init; }
    public int Priority { get; init; } = 1; // 1 = Low, 2 = Medium, 3 = High
}

public record CreateToDoRequest(string Title, string? Description, string? Category, int Priority = 1);

public record UpdateToDoRequest(string Title, string? Description, string? Category, int Priority, bool IsCompleted);

public record CountResult(long Count);

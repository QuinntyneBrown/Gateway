// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace SimpleStorage.Core.DTOs;

public record UpdateMetadataRequest
{
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public int FileType { get; init; }
    public string StoragePath { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public Dictionary<string, object>? Tags { get; init; }
}

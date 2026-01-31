// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using SimpleStorage.Core.DTOs;
using SimpleStorage.Core.Interfaces;
using SimpleStorage.Core.Models;

namespace SimpleStorage.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class MetadataController : ControllerBase
{
    private readonly IMetadataRepository _repository;

    public MetadataController(IMetadataRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Get all file metadata
    /// </summary>
    /// <returns>List of all metadata records</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<Metadata>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<Metadata>>> GetAll()
    {
        var results = await _repository.GetAllAsync();
        return Ok(results);
    }

    /// <summary>
    /// Get paginated file metadata with optional filtering by file type
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 10)</param>
    /// <param name="fileType">Optional file type filter</param>
    /// <returns>Paginated metadata results</returns>
    [HttpGet("page")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPage(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int? fileType = null)
    {
        var pagedResult = await _repository.GetPageAsync(page, pageSize, fileType);

        return Ok(new
        {
            pagedResult.Items,
            pagedResult.PageNumber,
            pagedResult.PageSize,
            pagedResult.HasPreviousPage,
            pagedResult.HasNextPage,
            Filter = new { FileType = fileType }
        });
    }

    /// <summary>
    /// Get file metadata by ID
    /// </summary>
    /// <param name="id">Metadata ID</param>
    /// <returns>Metadata record</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Metadata), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Metadata>> GetById(string id)
    {
        var metadata = await _repository.GetByIdAsync(id);

        if (metadata is null)
        {
            return NotFound(new { Message = $"Metadata with id '{id}' not found" });
        }

        return Ok(metadata);
    }

    /// <summary>
    /// Create new file metadata
    /// </summary>
    /// <param name="request">Metadata creation request</param>
    /// <returns>Created metadata record</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Metadata), StatusCodes.Status201Created)]
    public async Task<ActionResult<Metadata>> Create([FromBody] CreateMetadataRequest request)
    {
        var id = $"metadata::{Guid.NewGuid()}";
        var metadata = new Metadata
        {
            Id = id,
            FileName = request.FileName,
            ContentType = request.ContentType,
            FileSize = request.FileSize,
            FileType = request.FileType,
            StoragePath = request.StoragePath,
            Version = request.Version,
            UploadedAt = DateTime.UtcNow,
            UploadedBy = request.UploadedBy,
            Tags = request.Tags
        };

        var created = await _repository.CreateAsync(metadata);
        return CreatedAtAction(nameof(GetById), new { id }, created);
    }

    /// <summary>
    /// Update existing file metadata
    /// </summary>
    /// <param name="id">Metadata ID</param>
    /// <param name="request">Metadata update request</param>
    /// <returns>Updated metadata record</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(Metadata), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Metadata>> Update(string id, [FromBody] UpdateMetadataRequest request)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing is null)
        {
            return NotFound(new { Message = $"Metadata with id '{id}' not found" });
        }

        var updated = existing with
        {
            FileName = request.FileName,
            ContentType = request.ContentType,
            FileSize = request.FileSize,
            FileType = request.FileType,
            StoragePath = request.StoragePath,
            Version = request.Version,
            ModifiedAt = DateTime.UtcNow,
            Tags = request.Tags
        };

        var result = await _repository.UpdateAsync(id, updated);
        return Ok(result);
    }

    /// <summary>
    /// Delete file metadata by ID
    /// </summary>
    /// <param name="id">Metadata ID</param>
    /// <returns>No content</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing is null)
        {
            return NotFound(new { Message = $"Metadata with id '{id}' not found" });
        }

        await _repository.DeleteAsync(id);
        return NoContent();
    }
}

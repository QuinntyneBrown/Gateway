// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Gateway.Core.Pagination;
using SimpleStorage.Core.Models;

namespace SimpleStorage.Core.Interfaces;

public interface IMetadataRepository
{
    Task<IReadOnlyList<Metadata>> GetAllAsync();
    Task<PagedResult<Metadata>> GetPageAsync(int pageNumber, int pageSize, int? fileType = null);
    Task<Metadata?> GetByIdAsync(string id);
    Task<Metadata> CreateAsync(Metadata metadata);
    Task<Metadata> UpdateAsync(string id, Metadata metadata);
    Task DeleteAsync(string id);
}

# SimpleStorage - File Metadata Management API

A complete N-Tier backend API in C# that performs CRUD operations on file metadata using Couchbase and Gateway (SimpleMapper) for persistence.

## Architecture

The solution follows a clean N-Tier architecture pattern:

```
SimpleStorage/
├── src/
│   ├── SimpleStorage.Api/          # Web API Layer
│   ├── SimpleStorage.Core/         # Domain Layer (Models, DTOs, Interfaces)
│   └── SimpleStorage.Infrastructure/  # Data Access Layer (Repository Implementation)
└── SimpleStorage.sln
```

### Projects

- **SimpleStorage.Core**: Contains domain models, DTOs, and repository interfaces
  - `Models/Metadata.cs`: The Metadata entity with file information
  - `DTOs/`: Request DTOs for Create and Update operations
  - `Interfaces/IMetadataRepository.cs`: Repository contract

- **SimpleStorage.Infrastructure**: Implements data access using Couchbase and Gateway
  - `Repositories/MetadataRepository.cs`: Couchbase implementation using Gateway SimpleMapper
  - `DependencyInjection.cs`: Service registration

- **SimpleStorage.Api**: RESTful API with Controllers
  - `Controllers/MetadataController.cs`: API endpoints for CRUD operations

## Metadata Model

The `Metadata` model represents file metadata with the following properties:

```csharp
- Id (string): Unique identifier
- FileName (string): Name of the file
- ContentType (string): MIME type
- FileSize (long): Size in bytes
- FileType (int): Numeric file type classifier
- StoragePath (string): Path where file is stored
- Version (string): File version
- UploadedAt (DateTime): Upload timestamp
- ModifiedAt (DateTime?): Last modification timestamp
- UploadedBy (string): User who uploaded the file
- Tags (Dictionary<string, object>?): Custom metadata tags
```

## API Endpoints

### Get All Metadata
```http
GET /api/metadata
```
Returns all file metadata records.

### Get Paginated Metadata
```http
GET /api/metadata/page?page=1&pageSize=10&fileType=1
```
Returns paginated metadata with optional filtering by `fileType`.

**Query Parameters:**
- `page` (int, default: 1): Page number
- `pageSize` (int, default: 10): Items per page
- `fileType` (int, optional): Filter by file type

### Get Metadata by ID
```http
GET /api/metadata/{id}
```
Returns a single metadata record by its ID.

### Create Metadata
```http
POST /api/metadata
Content-Type: application/json

{
  "fileName": "document.pdf",
  "contentType": "application/pdf",
  "fileSize": 1024000,
  "fileType": 1,
  "storagePath": "/uploads/2024/01/document.pdf",
  "version": "1.0.0",
  "uploadedBy": "user@example.com",
  "tags": {
    "category": "documents",
    "department": "engineering"
  }
}
```

### Update Metadata
```http
PUT /api/metadata/{id}
Content-Type: application/json

{
  "fileName": "updated-document.pdf",
  "contentType": "application/pdf",
  "fileSize": 1024000,
  "fileType": 1,
  "storagePath": "/uploads/2024/01/updated-document.pdf",
  "version": "1.0.1",
  "tags": {
    "category": "documents",
    "department": "engineering"
  }
}
```

### Delete Metadata
```http
DELETE /api/metadata/{id}
```
Deletes a metadata record by its ID.

## Configuration

The API uses Couchbase with the following default configuration (in `appsettings.json`):

```json
{
  "Couchbase": {
    "ConnectionString": "couchbase://localhost",
    "UserName": "Administrator",
    "Password": "password",
    "DefaultBucket": "general",
    "DefaultScope": "artifacts"
  }
}
```

## Technology Stack

- **.NET 10.0**: Latest .NET framework
- **ASP.NET Core Controllers**: Traditional controller-based API
- **Couchbase**: NoSQL document database
- **Gateway (SimpleMapper)**: Lightweight object mapper for Couchbase
- **Swagger/OpenAPI**: API documentation and testing

## Gateway Integration

This solution leverages **Gateway (SimpleMapper)** for clean Couchbase integration:

- **FilterBuilder**: Type-safe query building with parameterization
- **Pagination**: Built-in `PagedResult<T>` with HasNext/HasPrevious
- **Extension Methods**: Clean `QueryToListAsync<T>`, `GetAsync<T>`, etc.
- **CRUD Operations**: Simplified InsertAsync, ReplaceAsync, RemoveAsync

Example usage in the repository:

```csharp
// Build dynamic filters with Gateway FilterBuilder
var filter = new FilterBuilder<Metadata>();
filter.Where("fileType", fileType.Value);
filter.OrderBy("uploadedAt", descending: true);
filter.Skip(offset).Take(effectivePageSize);

// Execute query using Gateway extension methods
var query = $"SELECT META().id, m.* FROM `{_bucket.Name}`.`{_scopeName}`.`{_collectionName}` m {filter.Build()}";
var results = await scope.QueryToListAsync<Metadata>(query, queryOptions);
```

## Running the Application

### Prerequisites
1. Couchbase Server running locally or accessible instance
2. .NET 10.0 SDK installed
3. Bucket named "general" with scope "artifacts" and collection "metadata" created in Couchbase

### Build
```bash
cd C:\projects\Gateway\playground\SimpleStorage
dotnet build
```

### Run
```bash
cd src\SimpleStorage.Api
dotnet run
```

The API will start on `https://localhost:5001` (or the port specified in launchSettings.json).

### Access Swagger UI
Navigate to `https://localhost:5001/swagger` to explore and test the API endpoints.

## Development Notes

- Document IDs use the format: `metadata::{Guid}`
- All timestamps are stored in UTC
- The repository uses Gateway's extension methods for clean, parameterized queries
- Pagination automatically includes HasNextPage/HasPreviousPage metadata
- CORS is configured to allow all origins in development

## License

Copyright (c) Quinntyne Brown. All Rights Reserved.
Licensed under the MIT License.

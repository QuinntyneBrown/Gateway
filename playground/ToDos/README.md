# ToDos API - Gateway Demo

A sample ToDo API demonstrating the **Gateway (Couchbase SimpleMapper)** library features including:

- **FilterBuilder** - Dynamic SQL++ WHERE clause generation
- **Pagination** - PagedResult with metadata (HasPreviousPage, HasNextPage)
- **CRUD Operations** - GetAsync, InsertAsync, UpsertAsync, ReplaceAsync, RemoveAsync
- **Query Execution** - QueryToListAsync, QueryFirstOrDefaultAsync

## Prerequisites

- .NET 9.0 SDK
- Docker Desktop

## Quick Start with Docker Compose

### Option 1: Run Everything in Docker (Recommended)

```bash
cd playground/ToDos

# Start Couchbase + API
docker-compose up -d

# Wait for services to start (about 30-60 seconds)
# Then initialize Couchbase
.\scripts\setup-couchbase.ps1

# Seed sample data
curl -X POST http://localhost:5000/api/todos/seed
```

**Services:**
| Service | URL |
|---------|-----|
| ToDos API | http://localhost:5000 |
| Swagger UI | http://localhost:5000/swagger |
| Couchbase Console | http://localhost:8091 |

### Option 2: Run Only Couchbase in Docker

```bash
cd playground/ToDos

# Start only Couchbase
docker-compose up -d couchbase

# Initialize Couchbase
.\scripts\setup-couchbase.ps1

# Run API locally
dotnet run --project src/ToDos.Api
```

### Initialize Couchbase (Required for Both Options)

**Option A: PowerShell Script (Recommended)**
```powershell
.\scripts\setup-couchbase.ps1
```

**Option B: Manual Setup**

1. Open http://localhost:8091 in your browser
2. Click "Setup New Cluster"
3. Set Cluster Name: `todos-cluster`
4. Set Admin Username: `Administrator`
5. Set Password: `password`
6. Accept terms and click "Configure Disk, Memory, Services"
7. Check services: Data, Query, Index (uncheck others to save memory)
8. Set Data RAM Quota: 512 MB, Index RAM Quota: 256 MB
9. Click "Save & Finish"

**Create the Bucket:**
1. Go to "Buckets" in the left menu
2. Click "Add Bucket"
3. Name: `todos`, RAM Quota: 256 MB
4. Click "Add Bucket"

**Create Primary Index:**
1. Go to "Query" in the left menu
2. Run: `CREATE PRIMARY INDEX ON \`todos\`._default._default`

### Run the API Locally (if not using Docker)

```bash
cd playground/ToDos
dotnet run --project src/ToDos.Api
```

The API runs at `https://localhost:5001` (or `http://localhost:5000`).

## API Endpoints

### Paginated ToDos (Main Feature)
```
GET /api/todos?page=1&pageSize=10&isCompleted=false&category=Development&sortBy=createdAt&sortDescending=true
```

Query Parameters:
- `page` - Page number (default: 1)
- `pageSize` - Items per page (default: 10, max: 1000)
- `isCompleted` - Filter by completion status
- `category` - Filter by category
- `minPriority` - Filter by minimum priority (1=Low, 2=Medium, 3=High)
- `sortBy` - Sort field (default: createdAt)
- `sortDescending` - Sort direction (default: true)

### Other Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/todos` | Get paginated ToDos with filtering |
| GET | `/api/todos/{id}` | Get a single ToDo |
| POST | `/api/todos` | Create a new ToDo |
| PUT | `/api/todos/{id}` | Update a ToDo |
| PATCH | `/api/todos/{id}/complete` | Mark as complete |
| DELETE | `/api/todos/{id}` | Delete a ToDo |
| GET | `/api/todos/search?q=text` | Search by title |
| GET | `/api/todos/count` | Get counts by status |
| GET | `/api/todos/by-priority/{priority}` | Filter by priority |
| POST | `/api/todos/seed` | Seed sample data |

## Gateway Features Demonstrated

### FilterBuilder
```csharp
var filter = new FilterBuilder<ToDo>();
filter.Where("isCompleted", false);
filter.WhereGreaterThanOrEqual("priority", 2);
filter.OrderBy("createdAt", descending: true);
filter.Skip(0).Take(10);

var whereClause = filter.Build();
// Result: WHERE isCompleted = $p0 AND priority >= $p1 ORDER BY createdAt DESC LIMIT 10 OFFSET 0
```

### PagedResult
```csharp
var pagedResult = new PagedResult<ToDo>(
    items: results,
    pageNumber: 1,
    pageSize: 10,
    hasMoreItems: true
);
// Includes: HasPreviousPage, HasNextPage, TotalPages (if totalCount provided)
```

### Collection Extensions (CRUD)
```csharp
await collection.GetAsync<ToDo>(id);
await collection.InsertAsync(id, todo);
await collection.UpsertAsync(id, todo);
await collection.ReplaceAsync(id, todo);
await collection.RemoveAsync(id);
```

### Scope Extensions (Queries)
```csharp
await scope.QueryToListAsync<ToDo>(query, options);
await scope.QueryFirstOrDefaultAsync<CountResult>(query);
```

## Docker Commands

```bash
# Start all services (Couchbase + API)
docker-compose up -d

# Start only Couchbase
docker-compose up -d couchbase

# Rebuild and start API (after code changes)
docker-compose up -d --build todos-api

# View logs
docker-compose logs -f
docker-compose logs -f todos-api
docker-compose logs -f couchbase

# Stop services (preserves data)
docker-compose stop

# Stop and remove containers (preserves data volume)
docker-compose down

# Stop and remove everything including data
docker-compose down -v

# Check status
docker-compose ps
```

## Configuration

Edit `appsettings.json`:

```json
{
  "Couchbase": {
    "ConnectionString": "couchbase://localhost",
    "Username": "Administrator",
    "Password": "password",
    "BucketName": "todos"
  }
}
```

## Troubleshooting

**Couchbase won't start:**
- Ensure ports 8091-8096 and 11210-11211 are not in use
- Check Docker Desktop is running
- Run `docker-compose logs couchbase` for errors

**Connection refused errors:**
- Wait 30-60 seconds after starting Couchbase
- Verify cluster is initialized at http://localhost:8091
- Check the `todos` bucket exists

**Query errors:**
- Ensure primary index is created
- Run: `CREATE PRIMARY INDEX IF NOT EXISTS ON \`todos\`._default._default`
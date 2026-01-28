# Couchbase Setup Script for ToDos API
# This script initializes Couchbase with the required bucket and indexes

$CouchbaseHost = "localhost"
$Username = "Administrator"
$Password = "password"
$BucketName = "todos"

Write-Host "Waiting for Couchbase to be ready..." -ForegroundColor Yellow

# Wait for Couchbase to be ready
$maxRetries = 30
$retryCount = 0
do {
    try {
        $response = Invoke-WebRequest -Uri "http://${CouchbaseHost}:8091/pools" -UseBasicParsing -ErrorAction Stop
        if ($response.StatusCode -eq 200) {
            Write-Host "Couchbase is responding!" -ForegroundColor Green
            break
        }
    }
    catch {
        $retryCount++
        Write-Host "Waiting for Couchbase... ($retryCount/$maxRetries)"
        Start-Sleep -Seconds 2
    }
} while ($retryCount -lt $maxRetries)

if ($retryCount -eq $maxRetries) {
    Write-Host "ERROR: Couchbase did not start in time" -ForegroundColor Red
    exit 1
}

# Check if cluster is already initialized
try {
    $poolsResponse = Invoke-RestMethod -Uri "http://${CouchbaseHost}:8091/pools/default" -ErrorAction Stop
    Write-Host "Cluster already initialized" -ForegroundColor Green
}
catch {
    Write-Host "Initializing Couchbase cluster..." -ForegroundColor Yellow

    # Initialize the cluster
    $initParams = @{
        hostname = $CouchbaseHost
        username = $Username
        password = $Password
        port = "8091"
    }

    # Set up memory quotas
    Invoke-RestMethod -Uri "http://${CouchbaseHost}:8091/pools/default" `
        -Method Post `
        -Body "memoryQuota=512&indexMemoryQuota=256" `
        -ErrorAction SilentlyContinue

    # Set up services
    Invoke-RestMethod -Uri "http://${CouchbaseHost}:8091/node/controller/setupServices" `
        -Method Post `
        -Body "services=kv%2Cn1ql%2Cindex" `
        -ErrorAction SilentlyContinue

    # Set credentials
    Invoke-RestMethod -Uri "http://${CouchbaseHost}:8091/settings/web" `
        -Method Post `
        -Body "username=$Username&password=$Password&port=8091" `
        -ErrorAction SilentlyContinue

    Write-Host "Cluster initialized!" -ForegroundColor Green
}

# Create credentials for API calls
$pair = "${Username}:${Password}"
$bytes = [System.Text.Encoding]::ASCII.GetBytes($pair)
$base64 = [System.Convert]::ToBase64String($bytes)
$headers = @{ Authorization = "Basic $base64" }

# Check if bucket exists
try {
    $bucketResponse = Invoke-RestMethod -Uri "http://${CouchbaseHost}:8091/pools/default/buckets/$BucketName" `
        -Headers $headers -ErrorAction Stop
    Write-Host "Bucket '$BucketName' already exists" -ForegroundColor Green
}
catch {
    Write-Host "Creating bucket '$BucketName'..." -ForegroundColor Yellow

    $bucketParams = "name=$BucketName&ramQuota=256&bucketType=couchbase&authType=sasl"

    Invoke-RestMethod -Uri "http://${CouchbaseHost}:8091/pools/default/buckets" `
        -Method Post `
        -Headers $headers `
        -Body $bucketParams

    Write-Host "Bucket created! Waiting for it to be ready..." -ForegroundColor Green
    Start-Sleep -Seconds 5
}

# Create primary index
Write-Host "Creating primary index..." -ForegroundColor Yellow

$indexQuery = @{
    statement = "CREATE PRIMARY INDEX IF NOT EXISTS ON \`$BucketName\`._default._default"
} | ConvertTo-Json

try {
    Invoke-RestMethod -Uri "http://${CouchbaseHost}:8093/query/service" `
        -Method Post `
        -Headers $headers `
        -ContentType "application/json" `
        -Body $indexQuery

    Write-Host "Primary index created!" -ForegroundColor Green
}
catch {
    Write-Host "Index may already exist or query service not ready: $_" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Couchbase setup complete!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Web Console: http://localhost:8091" -ForegroundColor White
Write-Host "Username: $Username" -ForegroundColor White
Write-Host "Password: $Password" -ForegroundColor White
Write-Host "Bucket: $BucketName" -ForegroundColor White
Write-Host ""
Write-Host "Run the API with: dotnet run --project src/ToDos.Api" -ForegroundColor Green

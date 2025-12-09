# R2 Object Storage

The `Cloudflare.NET.R2` package provides a high-level client for Cloudflare R2 object storage using the S3-compatible API.

## Installation

```bash
dotnet add package Cloudflare.NET.R2
```

## Configuration

```json
{
  "Cloudflare": {
    "AccountId": "your-cloudflare-account-id"
  },
  "R2": {
    "AccessKeyId": "your-r2-access-key-id",
    "SecretAccessKey": "your-r2-secret-access-key"
  }
}
```

```csharp
builder.Services.AddCloudflareR2Client(builder.Configuration);
```

## Basic Operations

### Upload Objects

```csharp
public class StorageService(IR2Client r2)
{
    // Upload a file (auto-selects single or multipart based on size)
    public async Task<R2Result> UploadFileAsync(string bucket, string key, string filePath)
    {
        return await r2.UploadAsync(bucket, key, filePath);
    }

    // Upload from stream
    public async Task<R2Result> UploadStreamAsync(string bucket, string key, Stream stream)
    {
        return await r2.UploadAsync(bucket, key, stream);
    }

    // Upload with content type
    public async Task<R2Result> UploadWithMetadataAsync(
        string bucket,
        string key,
        Stream stream,
        string contentType)
    {
        return await r2.UploadAsync(bucket, key, stream, contentType: contentType);
    }
}
```

### Download Objects

```csharp
// Download to file
public async Task DownloadToFileAsync(string bucket, string key, string filePath)
{
    await r2.DownloadFileAsync(bucket, key, filePath);
}

// Download to stream
public async Task<Stream> DownloadToStreamAsync(string bucket, string key)
{
    var memoryStream = new MemoryStream();
    await r2.DownloadFileAsync(bucket, key, memoryStream);
    memoryStream.Position = 0;
    return memoryStream;
}
```

### Delete Objects

```csharp
// Delete single object (free operation)
public async Task DeleteObjectAsync(string bucket, string key)
{
    await r2.DeleteObjectAsync(bucket, key);
}

// Batch delete (up to 1000 keys per request)
public async Task DeleteObjectsAsync(string bucket, IEnumerable<string> keys)
{
    await r2.DeleteObjectsAsync(bucket, keys);
}

// Clear entire bucket
public async Task ClearBucketAsync(string bucket)
{
    await r2.ClearBucketAsync(bucket);
}
```

### List Objects

```csharp
// List all objects with automatic pagination
public async IAsyncEnumerable<S3Object> ListAllAsync(string bucket, string? prefix = null)
{
    await foreach (var obj in r2.ListObjectsAsync(bucket, prefix))
    {
        yield return obj;
    }
}
```

## Presigned URLs

Generate presigned URLs for direct client uploads:

```csharp
// Generate presigned PUT URL (valid for 1 hour by default)
public string GetPresignedUploadUrl(string bucket, string key)
{
    return r2.CreatePresignedPutUrl(bucket, key, TimeSpan.FromHours(1));
}

// Generate presigned URL for multipart upload part
public string GetPresignedPartUrl(string bucket, string key, string uploadId, int partNumber)
{
    return r2.CreatePresignedUploadPartUrl(bucket, key, uploadId, partNumber);
}
```

## Multipart Uploads

For large files (> 5 GiB), use multipart uploads:

```csharp
// Automatic multipart upload based on size
public async Task<R2Result> UploadLargeFileAsync(string bucket, string key, string filePath)
{
    // Automatically uses multipart for files > threshold
    return await r2.UploadAsync(bucket, key, filePath);
}

// Explicit multipart upload
public async Task<R2Result> UploadMultipartAsync(string bucket, string key, Stream stream)
{
    return await r2.UploadMultipartAsync(bucket, key, stream);
}
```

## Operation Metrics

All R2 operations return `R2Result` or `R2Result<T>` which includes operation metrics:

```csharp
var result = await r2.UploadAsync(bucket, key, stream);

Console.WriteLine($"Class A Operations: {result.ClassAOperations}");
Console.WriteLine($"Class B Operations: {result.ClassBOperations}");
Console.WriteLine($"Ingress Bytes: {result.IngressBytes}");
Console.WriteLine($"Egress Bytes: {result.EgressBytes}");
```

## Error Handling

The R2 client provides specialized exceptions:

```csharp
try
{
    await r2.UploadAsync(bucket, key, stream);
}
catch (CloudflareR2OperationException ex)
{
    // Single operation failure
    Console.WriteLine($"Operation failed: {ex.Message}");
    Console.WriteLine($"Partial metrics: {ex.PartialMetrics}");
}
catch (CloudflareR2BatchException<string> ex)
{
    // Batch operation failure
    foreach (var failedKey in ex.FailedItems)
    {
        Console.WriteLine($"Failed to delete: {failedKey}");
    }
}
catch (CloudflareR2ConfigurationException ex)
{
    // Missing or invalid configuration
    Console.WriteLine($"Configuration error: {ex.Message}");
}
```

## Bucket Management

Bucket operations are available through the REST API client:

```csharp
public class BucketService(ICloudflareApiClient cf)
{
    // Basic bucket creation
    public async Task<R2Bucket> CreateBucketAsync(string name)
    {
        return await cf.Accounts.CreateR2BucketAsync(name);
    }

    // Create with location hint and jurisdiction
    public async Task<R2Bucket> CreateEuBucketAsync(string name)
    {
        return await cf.Accounts.CreateR2BucketAsync(
            name,
            locationHint: R2LocationHint.WestEurope,
            jurisdiction: R2Jurisdiction.EuropeanUnion
        );
    }

    public async IAsyncEnumerable<R2Bucket> ListBucketsAsync()
    {
        await foreach (var bucket in cf.Accounts.ListAllR2BucketsAsync())
        {
            // Access extensible enum properties with IntelliSense
            if (bucket.Location == R2LocationHint.WestEurope)
            {
                Console.WriteLine($"EU bucket: {bucket.Name}");
            }

            yield return bucket;
        }
    }

    public async Task DeleteBucketAsync(string name)
    {
        await cf.Accounts.DeleteR2BucketAsync(name);
    }
}
```

The `R2Bucket` model uses **extensible enums** for `Location`, `Jurisdiction`, and `StorageClass` properties. See [SDK Conventions](conventions.md#extensible-enums) for details.

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

## Jurisdiction Support

R2 supports jurisdictional restrictions that ensure data stays within specific geographic regions. The SDK provides full support for jurisdictions through configuration and the `IR2ClientFactory`.

### Available Jurisdictions

| Jurisdiction | Description | Endpoint |
|--------------|-------------|----------|
| `Default` | No restriction (global) | `https://{account_id}.r2.cloudflarestorage.com` |
| `EuropeanUnion` | EU data residency | `https://{account_id}.eu.r2.cloudflarestorage.com` |
| `FedRamp` | FedRAMP compliance | `https://{account_id}.fedramp.r2.cloudflarestorage.com` |

### Configuration-Based Jurisdiction

Set the jurisdiction in your configuration to use it as the default:

```json
{
  "R2": {
    "AccessKeyId": "...",
    "SecretAccessKey": "...",
    "Jurisdiction": "eu"
  }
}
```

### Runtime Jurisdiction with IR2ClientFactory

Use `IR2ClientFactory` to create clients for different jurisdictions at runtime:

```csharp
public class MultiJurisdictionService(IR2ClientFactory factory)
{
    // Get client for specific jurisdiction using default credentials
    public async Task UploadToEuAsync(string bucket, string key, Stream data)
    {
        var euClient = factory.GetClient(R2Jurisdiction.EuropeanUnion);
        await euClient.UploadAsync(bucket, key, data);
    }

    // Get client for specific jurisdiction using named credentials
    public async Task UploadToProdEuAsync(string bucket, string key, Stream data)
    {
        var client = factory.GetClient("production", R2Jurisdiction.EuropeanUnion);
        await client.UploadAsync(bucket, key, data);
    }
}
```

> [!NOTE]
> The same R2 credentials work across all jurisdictions within an account—only the S3 endpoint differs. Clients are cached by `(name, jurisdiction)` tuple and reused.

### Named Clients

For multi-account scenarios, register named R2 clients:

```csharp
// Registration
services.AddCloudflareR2Client("production", config);
services.AddCloudflareR2Client("staging", config);

// Usage
public class StorageService(IR2ClientFactory factory)
{
    public IR2Client Production => factory.GetClient("production");
    public IR2Client Staging => factory.GetClient("staging");
}
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
        return await cf.Accounts.Buckets.CreateAsync(name);
    }

    // Create with location hint and jurisdiction
    public async Task<R2Bucket> CreateEuBucketAsync(string name)
    {
        return await cf.Accounts.Buckets.CreateAsync(
            name,
            locationHint: R2LocationHint.WestEurope,
            jurisdiction: R2Jurisdiction.EuropeanUnion
        );
    }

    public async IAsyncEnumerable<R2Bucket> ListBucketsAsync()
    {
        await foreach (var bucket in cf.Accounts.Buckets.ListAllAsync())
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
        await cf.Accounts.Buckets.DeleteAsync(name);
    }
}
```

The `R2Bucket` model uses **extensible enums** for `Location`, `Jurisdiction`, and `StorageClass` properties. See [SDK Conventions](conventions.md#extensible-enums) for details.

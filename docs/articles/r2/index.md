# R2 Object Storage

The `Cloudflare.NET.R2` package provides a high-level S3-compatible client for Cloudflare R2 object storage with intelligent upload strategies, multipart support, presigned URLs, and comprehensive operation metrics.

## Installation

```bash
dotnet add package Cloudflare.NET.R2
```

## Quick Start

### Configuration

Add R2 settings to your `appsettings.json`:

```json
{
  "Cloudflare": {
    "AccountId": "your-account-id"
  },
  "R2": {
    "AccessKeyId": "your-r2-access-key-id",
    "SecretAccessKey": "your-r2-secret-access-key"
  }
}
```

### Registration

```csharp
// Program.cs
builder.Services.AddCloudflareR2Client(builder.Configuration);
```

### Basic Usage

Inject <xref:Cloudflare.NET.R2.IR2Client> into your services:

```csharp
public class StorageService(IR2Client r2)
{
    public async Task<R2Result> UploadFileAsync(string key, string filePath)
    {
        return await r2.UploadAsync("my-bucket", key, filePath);
    }

    public async Task<R2Result> DownloadFileAsync(string key, string downloadPath)
    {
        return await r2.DownloadFileAsync("my-bucket", key, downloadPath);
    }
}
```

## Features

| Feature | Description |
|---------|-------------|
| **Intelligent Uploads** | Auto-selects single-part or multipart based on file size |
| **Multipart Uploads** | Handle files up to 5 TiB with parallel part uploads |
| **Presigned URLs** | Generate secure URLs for direct browser uploads |
| **Jurisdiction Support** | EU, FedRAMP, and default global endpoints |
| **Named Clients** | Multi-account scenarios with `IR2ClientFactory` |
| **Operation Metrics** | Track Class A/B operations, ingress, and egress bytes |
| **Error Handling** | Rich exceptions with partial metrics for debugging |

## Operation Pricing

R2 has a unique pricing model with free and paid operations:

| Operation Type | Examples | Cost |
|----------------|----------|------|
| **Class A** | Put, List, Copy | $4.50 per million |
| **Class B** | Get, Head | $0.36 per million |
| **Free** | Delete | Free |
| **Egress** | Downloads | Free |

The SDK tracks these metrics in the <xref:Cloudflare.NET.R2.Models.R2Result> object returned by all operations.

## API Overview

### Upload Operations

| Method | Description |
|--------|-------------|
| [`UploadAsync`](uploads.md) | Auto-select upload strategy |
| [`UploadSinglePartAsync`](uploads.md#single-part-upload) | Force single PUT request |
| [`UploadMultipartAsync`](multipart.md) | Force multipart upload |

### Download Operations

| Method | Description |
|--------|-------------|
| [`DownloadFileAsync`](downloads.md) | Download to file or stream |

### Delete Operations

| Method | Description |
|--------|-------------|
| [`DeleteObjectAsync`](deletes.md) | Delete single object (free) |
| [`DeleteObjectsAsync`](deletes.md#batch-delete) | Batch delete (free) |
| [`ClearBucketAsync`](deletes.md#clear-bucket) | Delete all objects (free) |

### List Operations

| Method | Description |
|--------|-------------|
| [`ListObjectsAsync`](listing.md) | List objects with pagination |
| [`ListPartsAsync`](multipart.md#listing-parts) | List multipart upload parts |

### Presigned URLs

| Method | Description |
|--------|-------------|
| [`CreatePresignedPutUrl`](presigned-urls.md) | URL for direct upload |
| [`CreatePresignedUploadPartUrl`](presigned-urls.md#multipart-presigned-urls) | URL for multipart part |
| [`CreatePresignedUploadPartsUrls`](presigned-urls.md#batch-presigned-urls) | Batch URLs for parts |

### Low-Level Multipart

| Method | Description |
|--------|-------------|
| [`InitiateMultipartUploadAsync`](multipart.md#manual-multipart-control) | Start multipart upload |
| [`CompleteMultipartUploadAsync`](multipart.md#completing-upload) | Finalize multipart upload |
| [`AbortMultipartUploadAsync`](multipart.md#aborting-upload) | Cancel multipart upload |

## R2Result Model

All operations return <xref:Cloudflare.NET.R2.Models.R2Result> with operation metrics:

```csharp
public record R2Result(
    long ClassAOperations = 0,  // Writes, lists
    long ClassBOperations = 0,  // Reads
    long IngressBytes = 0,      // Bytes uploaded
    long EgressBytes = 0        // Bytes downloaded
);
```

Results can be combined:

```csharp
var total = result1 + result2 + result3;
Console.WriteLine($"Total ingress: {total.IngressBytes} bytes");
```

## Exception Handling

The SDK provides rich exceptions for error handling:

| Exception | Scenario |
|-----------|----------|
| <xref:Cloudflare.NET.R2.Exceptions.CloudflareR2OperationException> | Single operation failure |
| <xref:Cloudflare.NET.R2.Exceptions.CloudflareR2BatchException`1> | Batch operation with failures |
| <xref:Cloudflare.NET.R2.Exceptions.CloudflareR2ListException`1> | List operation failed mid-stream |

```csharp
try
{
    await r2.UploadAsync("bucket", "key", filePath);
}
catch (CloudflareR2OperationException ex)
{
    // Single operation failed
    Console.WriteLine($"Upload failed: {ex.Message}");
    Console.WriteLine($"Partial metrics: {ex.PartialMetrics}");
}
catch (CloudflareR2BatchException<string> ex)
{
    // Batch operation had failures
    Console.WriteLine($"Failed items: {ex.FailedItems.Count}");
    Console.WriteLine($"Partial metrics: {ex.PartialMetrics}");
}
catch (CloudflareR2ListException<S3Object> ex)
{
    // List operation failed mid-stream
    Console.WriteLine($"Partial data retrieved: {ex.PartialData.Count}");
}
```

## Size Limits

| Limit | Value |
|-------|-------|
| Maximum object size | 5 TiB |
| Single PUT limit | 5 GiB |
| Minimum part size | 5 MiB |
| Maximum part size | 5 GiB |
| Maximum parts per upload | 10,000 |

## Required Permissions

| Permission | Description |
|------------|-------------|
| `s3:GetObject` | Download objects |
| `s3:PutObject` | Upload objects |
| `s3:DeleteObject` | Delete objects |
| `s3:ListBucket` | List objects |

## Jurisdictions

R2 supports jurisdictional restrictions for data residency compliance. Use `IR2ClientFactory` to create clients for different jurisdictions:

```csharp
public class MultiRegionService(IR2ClientFactory factory)
{
    public async Task ReplicateToEuAsync(string bucket, string key, Stream data)
    {
        // Create a client for the EU jurisdiction
        var euClient = factory.GetClient(R2Jurisdiction.EuropeanUnion);
        await euClient.UploadAsync(bucket, key, data);
    }
}
```

| Jurisdiction | Endpoint |
|--------------|----------|
| `Default` | `https://{account_id}.r2.cloudflarestorage.com` |
| `EuropeanUnion` | `https://{account_id}.eu.r2.cloudflarestorage.com` |
| `FedRamp` | `https://{account_id}.fedramp.r2.cloudflarestorage.com` |

See [R2 Object Storage](../r2-client.md#jurisdiction-support) for detailed jurisdiction documentation.

## Related

- [Uploading Objects](uploads.md) - Single and automatic uploads
- [Downloading Objects](downloads.md) - Download to files and streams
- [Deleting Objects](deletes.md) - Single and batch deletes
- [Listing Objects](listing.md) - Object enumeration
- [Multipart Uploads](multipart.md) - Large file handling
- [Presigned URLs](presigned-urls.md) - Direct browser uploads
- [R2 Bucket Management](../accounts/r2/buckets.md) - Create and configure buckets

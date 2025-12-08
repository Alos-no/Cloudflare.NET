# Multipart Uploads

Handle large file uploads (up to 5 TiB) using R2's multipart upload API.

## Overview

Multipart uploads split large files into smaller parts that can be uploaded independently and assembled on the server.

```csharp
public class LargeFileService(IR2Client r2)
{
    public async Task UploadLargeFileAsync(string key, string filePath)
    {
        // Automatic multipart for files > 5 GiB
        var result = await r2.UploadAsync("my-bucket", key, filePath);

        Console.WriteLine($"Uploaded {result.IngressBytes} bytes");
        Console.WriteLine($"Class A operations: {result.ClassAOperations}");
    }
}
```

## Automatic Multipart

The `UploadAsync` method automatically uses multipart for large files:

```csharp
// Files > 5 GiB are automatically uploaded as multipart
var result = await r2.UploadAsync("bucket", "large-file.zip", "/path/to/large-file.zip");
```

## Explicit Multipart Upload

Force multipart upload regardless of file size:

### From File Path

```csharp
var result = await r2.UploadMultipartAsync(
    bucketName: "my-bucket",
    objectKey: "backup/database.sql",
    filePath: "/path/to/database.sql",
    partSize: 100 * 1024 * 1024); // 100 MiB parts
```

### From Stream

```csharp
await using var stream = File.OpenRead("/path/to/large-file.bin");

var result = await r2.UploadMultipartAsync(
    bucketName: "my-bucket",
    objectKey: "data/large-file.bin",
    inputStream: stream, // Must be seekable
    partSize: 50 * 1024 * 1024); // 50 MiB parts
```

## Part Size Configuration

The `partSize` parameter controls chunk sizes (default is calculated based on file size):

| Part Size | When to Use |
|-----------|-------------|
| 5 MiB (minimum) | Many small files, limited memory |
| 50-100 MiB | General purpose |
| 500 MiB - 1 GiB | Very large files, fast networks |
| 5 GiB (maximum) | Minimize operations for huge files |

```csharp
// Small parts for memory-constrained environments
await r2.UploadMultipartAsync("bucket", "key", filePath,
    partSize: 5 * 1024 * 1024); // 5 MiB

// Large parts for fast connections
await r2.UploadMultipartAsync("bucket", "key", filePath,
    partSize: 1024 * 1024 * 1024); // 1 GiB
```

## Manual Multipart Control

For fine-grained control over the upload process:

### Initiating Upload

```csharp
var result = await r2.InitiateMultipartUploadAsync(
    bucketName: "my-bucket",
    objectKey: "uploads/large-file.bin");

var uploadId = result.Data;
Console.WriteLine($"Upload ID: {uploadId}");
```

### Listing Parts

Check which parts have been uploaded:

```csharp
var partsResult = await r2.ListPartsAsync(
    bucketName: "my-bucket",
    objectKey: "uploads/large-file.bin",
    uploadId: uploadId);

foreach (var part in partsResult.Data)
{
    Console.WriteLine($"Part {part.PartNumber}: {part.Size} bytes");
    Console.WriteLine($"  ETag: {part.ETag}");
}
```

### Completing Upload

After all parts are uploaded:

```csharp
// Collect ETags from each uploaded part
var partETags = new List<PartETag>
{
    new("etag-from-part-1", 1),
    new("etag-from-part-2", 2),
    new("etag-from-part-3", 3)
};

var result = await r2.CompleteMultipartUploadAsync(
    bucketName: "my-bucket",
    objectKey: "uploads/large-file.bin",
    uploadId: uploadId,
    parts: partETags);
```

### Aborting Upload

Cancel an incomplete multipart upload (free operation):

```csharp
await r2.AbortMultipartUploadAsync(
    bucketName: "my-bucket",
    objectKey: "uploads/large-file.bin",
    uploadId: uploadId);
```

> [!NOTE]
> Aborting deletes all uploaded parts. This is a free operation.

## Error Handling

```csharp
try
{
    await r2.UploadMultipartAsync("bucket", "key", filePath);
}
catch (NotSupportedException ex)
{
    // Stream is not seekable
    Console.WriteLine($"Stream error: {ex.Message}");
}
catch (ArgumentException ex)
{
    // File too large (> 5 TiB)
    Console.WriteLine($"Size error: {ex.Message}");
}
catch (CloudflareR2OperationException ex)
{
    // Upload failed
    Console.WriteLine($"Upload failed: {ex.Message}");
    Console.WriteLine($"Partial metrics: {ex.PartialMetrics}");

    // Consider cleanup
    if (ex.Message.Contains("uploadId"))
    {
        // Extract uploadId and abort if needed
    }
}
```

## Common Patterns

### Upload with Progress

```csharp
public async Task UploadWithProgressAsync(
    string bucket, string key, string filePath, IProgress<double> progress)
{
    var fileInfo = new FileInfo(filePath);
    var totalSize = fileInfo.Length;
    var partSize = 100 * 1024 * 1024L; // 100 MiB

    // Initiate
    var initResult = await r2.InitiateMultipartUploadAsync(bucket, key);
    var uploadId = initResult.Data;

    try
    {
        var partETags = new List<PartETag>();
        var partNumber = 1;
        var uploadedBytes = 0L;

        await using var stream = File.OpenRead(filePath);
        var buffer = new byte[partSize];

        while (true)
        {
            var bytesRead = await stream.ReadAsync(buffer);
            if (bytesRead == 0) break;

            // Upload part (would need presigned URL or S3 SDK for this)
            // partETags.Add(new PartETag(etag, partNumber));

            uploadedBytes += bytesRead;
            progress.Report((double)uploadedBytes / totalSize * 100);
            partNumber++;
        }

        // Complete
        await r2.CompleteMultipartUploadAsync(bucket, key, uploadId, partETags);
        progress.Report(100);
    }
    catch
    {
        await r2.AbortMultipartUploadAsync(bucket, key, uploadId);
        throw;
    }
}
```

### Resume Incomplete Upload

```csharp
public async Task ResumeUploadAsync(
    string bucket, string key, string filePath, string uploadId)
{
    // List already uploaded parts
    var partsResult = await r2.ListPartsAsync(bucket, key, uploadId);
    var existingParts = partsResult.Data;

    var completedPartNumbers = existingParts
        .Select(p => p.PartNumber)
        .ToHashSet();

    Console.WriteLine($"Resuming upload with {existingParts.Count} parts already uploaded");

    // Continue uploading missing parts...
    // Then complete
}
```

### Cleanup Incomplete Uploads

R2 lifecycle policies can automatically clean up incomplete uploads, but you can also do it manually:

```csharp
public async Task CleanupIncompleteUploadsAsync(string bucket, string key)
{
    // This requires listing incomplete uploads (not directly exposed)
    // Use AbortMultipartUploadAsync if you have the uploadId

    await r2.AbortMultipartUploadAsync(bucket, key, "known-upload-id");
}
```

## Size Limits

| Limit | Value |
|-------|-------|
| Maximum object size | 5 TiB |
| Minimum part size | 5 MiB |
| Maximum part size | 5 GiB |
| Maximum parts per upload | 10,000 |

### Part Count Calculation

```
MaxObjectSize = MaxParts × MaxPartSize = 10,000 × 5 GiB = 50 TiB (theoretical)
ActualLimit = 5 TiB (R2 limit)
```

## R2 Pricing

| Operation | Cost |
|-----------|------|
| InitiateMultipartUpload | Class A |
| UploadPart | Class A |
| CompleteMultipartUpload | Class A |
| AbortMultipartUpload | Free |
| ListParts | Class A |

## Related

- [Uploading Objects](uploads.md) - Simple uploads
- [Presigned URLs](presigned-urls.md) - Browser multipart uploads
- [Lifecycle Policies](../accounts/r2/lifecycle.md) - Auto-cleanup incomplete uploads

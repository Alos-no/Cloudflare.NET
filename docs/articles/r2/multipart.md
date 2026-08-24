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

#### Setting the Object's Content-Type

S3 reads the assembled object's `Content-Type` from the call that starts the upload and never from the
individual parts. A caller that hands out presigned part URLs therefore has no later opportunity to set
it, so supply it here:

```csharp
var result = await r2.InitiateMultipartUploadAsync(
    bucketName:  "my-bucket",
    objectKey:   "uploads/report.pdf",
    contentType: "application/pdf");
```

Omitting the argument, or passing `null` or a blank string, leaves R2 to choose. R2's default is
`application/octet-stream`, which is what browsers download rather than display.

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

### Listing Open Uploads

`ListMultipartUploadsAsync` reports every multipart upload that was started in a bucket and has not
yet been completed or aborted. Object listing cannot see these uploads, because their parts only
become an object once the upload completes, so this is the only way to discover them:

```csharp
var open = await r2.ListMultipartUploadsAsync(
    bucketName: "my-bucket",
    prefix:     null); // or restrict to a key prefix

foreach (var upload in open.Data)
{
    Console.WriteLine($"{upload.Key} started {upload.Initiated:O}");
    Console.WriteLine($"  UploadId: {upload.UploadId}");
}
```

The parts of an open upload still consume storage you are billed for, and R2 refuses to delete a
bucket that has any upload left open. Pass the `UploadId` from this listing to
`AbortMultipartUploadAsync` to release them:

```csharp
foreach (var upload in open.Data)
{
    await r2.AbortMultipartUploadAsync("my-bucket", upload.Key, upload.UploadId);
}
```

> [!IMPORTANT]
> R2 has been observed to report an upload identifier in this listing that differs from the one it
> returned when the upload was started. Always abort using the `UploadId` that this listing supplied,
> not one you recorded earlier.

The method walks every page of results before returning, so the returned list is complete.

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

R2 applies a lifecycle rule that expires incomplete uploads after seven days, but you can release
their storage immediately by discovering the open uploads and aborting each one:

```csharp
public async Task<int> CleanupIncompleteUploadsAsync(string bucket, string? prefix = null)
{
    var open = await r2.ListMultipartUploadsAsync(bucket, prefix);

    foreach (var upload in open.Data)
    {
        await r2.AbortMultipartUploadAsync(bucket, upload.Key, upload.UploadId);
    }

    return open.Data.Count;
}
```

`ClearBucketAsync` already does exactly this after it deletes the objects, so calling it is enough to
leave a bucket that R2 will let you delete. See [Deleting Objects](deletes.md) for the parameter that
turns that cleanup off.

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
| ListMultipartUploads | Class A per page |

## Related

- [Uploading Objects](uploads.md) - Simple uploads
- [Presigned URLs](presigned-urls.md) - Browser multipart uploads
- [Lifecycle Policies](../accounts/r2/lifecycle.md) - Auto-cleanup incomplete uploads

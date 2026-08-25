# Uploading Objects

The R2 client provides multiple upload methods with intelligent strategy selection based on file size.

## Overview

```csharp
public class UploadService(IR2Client r2)
{
    public async Task UploadFileAsync(string key, string filePath)
    {
        // Automatically selects single-part or multipart based on size
        var result = await r2.UploadAsync("my-bucket", key, filePath);

        Console.WriteLine($"Uploaded {result.IngressBytes} bytes");
        Console.WriteLine($"Class A operations: {result.ClassAOperations}");
    }
}
```

## Automatic Upload

The `UploadAsync` method automatically chooses the best upload strategy:
- Files under 50 MiB: Single PUT request
- Files of 50 MiB or more, and non-seekable streams: Multipart upload

### From File Path

```csharp
var result = await r2.UploadAsync(
    bucketName: "my-bucket",
    objectKey: "documents/report.pdf",
    filePath: "/path/to/report.pdf");

Console.WriteLine($"Uploaded: {result.IngressBytes} bytes");
```

### From Stream

```csharp
await using var stream = File.OpenRead("/path/to/file.zip");

var result = await r2.UploadAsync(
    bucketName: "my-bucket",
    objectKey: "archives/file.zip",
    fileStream: stream);
```

### With Custom Part Size

For multipart uploads, you can specify the part size (5 MiB - 5 GiB):

```csharp
var result = await r2.UploadAsync(
    bucketName: "my-bucket",
    objectKey: "large-file.bin",
    filePath: "/path/to/large-file.bin",
    partSize: 100 * 1024 * 1024); // 100 MiB parts
```

## Single-Part Upload

Force a single PUT request for files up to 5 GiB:

### From File Path

```csharp
var result = await r2.UploadSinglePartAsync(
    bucketName: "my-bucket",
    objectKey: "images/photo.jpg",
    filePath: "/path/to/photo.jpg");
```

### From Stream

```csharp
await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Hello, R2!"));

var result = await r2.UploadSinglePartAsync(
    bucketName: "my-bucket",
    objectKey: "text/hello.txt",
    inputStream: stream);
```

### From In-Memory Data

```csharp
byte[] data = GetFileData();
await using var stream = new MemoryStream(data);

var result = await r2.UploadSinglePartAsync(
    bucketName: "my-bucket",
    objectKey: "data.bin",
    inputStream: stream);
```

## Content Type

Every upload method has an overload taking a `contentType`, so the stored object carries the MIME type
a CDN or browser needs to serve it correctly. Without it, R2 stores its own default
(`application/octet-stream`):

```csharp
// Automatic strategy selection, with the type recorded either way:
var result = await r2.UploadAsync(
    bucketName: "my-bucket",
    objectKey: "thumbnails/photo-small.webp",
    filePath: "/path/to/photo-small.webp",
    partSize: null,
    contentType: "image/webp");

// Single PUT:
await r2.UploadSinglePartAsync("my-bucket", "thumbnails/photo-small.webp", stream, "image/webp");

// Multipart (the type travels on the initiate request, the only place S3 reads it from):
await r2.UploadMultipartAsync("my-bucket", "videos/clip.mp4", filePath, null, "video/mp4");
```

Passing `null` or a blank string leaves the property unset, so the overload behaves exactly like the one
without the parameter. The value is applied verbatim: the client never infers a type from the file
extension, the bytes, or the object key.

## Checksums

A single-part upload can be bound to a digest of its bytes. R2 hashes what actually arrives and fails
the upload with 400 `BadDigest`, storing nothing, when the bytes do not hash to the stated digest. All
five `R2ChecksumAlgorithm` values are verified on this path:

```csharp
var fileBytes = await File.ReadAllBytesAsync("/path/to/photo-small.webp");
var checksum  = UploadChecksum.FromDigestBytes(R2ChecksumAlgorithm.Sha256, SHA256.HashData(fileBytes));

await using var stream = new MemoryStream(fileBytes);

await r2.UploadSinglePartAsync("my-bucket", "thumbnails/photo-small.webp", stream, "image/webp", checksum);
```

A checksum digests the whole object, but a multipart upload is verified per part and the client does not
compute per-part digests. `UploadAsync` therefore throws `ArgumentException` when a checksum accompanies
an input that would go multipart (50 MiB or more, or a non-seekable stream); use `UploadSinglePartAsync`
for objects up to 5 GiB, or upload without a checksum. For presigned uploads, where checksums guard
against an untrusted client, see [Checksum Verification](presigned-urls.md#checksum-verification).

## R2Result

Upload operations return `R2Result` with metrics:

```csharp
var result = await r2.UploadAsync("bucket", "key", filePath);

Console.WriteLine($"Class A Operations: {result.ClassAOperations}"); // 1 for single-part
Console.WriteLine($"Class B Operations: {result.ClassBOperations}"); // Always 0 for uploads
Console.WriteLine($"Ingress Bytes: {result.IngressBytes}");          // File size
Console.WriteLine($"Egress Bytes: {result.EgressBytes}");            // Always 0 for uploads
```

## Error Handling

```csharp
try
{
    await r2.UploadAsync("bucket", "key", filePath);
}
catch (FileNotFoundException)
{
    Console.WriteLine("File not found");
}
catch (ArgumentException ex)
{
    // File exceeds size limits
    Console.WriteLine($"Invalid file: {ex.Message}");
}
catch (CloudflareR2OperationException ex)
{
    Console.WriteLine($"Upload failed: {ex.Message}");
    // Access partial metrics if available
    Console.WriteLine($"Bytes uploaded before failure: {ex.PartialMetrics?.IngressBytes}");
}
```

## Common Patterns

### Upload with Progress

```csharp
public async Task UploadWithProgressAsync(string bucket, string key, string filePath)
{
    var fileInfo = new FileInfo(filePath);
    var totalBytes = fileInfo.Length;

    // For small files, use single-part
    if (totalBytes <= 5L * 1024 * 1024 * 1024)
    {
        Console.WriteLine("Uploading...");
        await r2.UploadSinglePartAsync(bucket, key, filePath);
        Console.WriteLine("Complete!");
    }
    else
    {
        // For large files, track multipart progress
        // See Multipart Uploads documentation
        await r2.UploadMultipartAsync(bucket, key, filePath);
    }
}
```

### Upload Multiple Files

```csharp
public async Task<R2Result> UploadDirectoryAsync(string bucket, string prefix, string localDir)
{
    var total = new R2Result();

    foreach (var file in Directory.GetFiles(localDir, "*", SearchOption.AllDirectories))
    {
        var relativePath = Path.GetRelativePath(localDir, file);
        var key = $"{prefix}/{relativePath.Replace('\\', '/')}";

        var result = await r2.UploadAsync(bucket, key, file);
        total += result;

        Console.WriteLine($"Uploaded: {key}");
    }

    return total;
}
```

### Upload with Retry

```csharp
public async Task<R2Result> UploadWithRetryAsync(
    string bucket, string key, string filePath, int maxRetries = 3)
{
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            return await r2.UploadAsync(bucket, key, filePath);
        }
        catch (CloudflareR2OperationException) when (attempt < maxRetries)
        {
            Console.WriteLine($"Attempt {attempt} failed, retrying...");
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
        }
    }

    throw new InvalidOperationException($"Upload failed after {maxRetries} attempts");
}
```

### Upload JSON Data

```csharp
public async Task<R2Result> UploadJsonAsync<T>(string bucket, string key, T data)
{
    var json = JsonSerializer.Serialize(data);
    var bytes = Encoding.UTF8.GetBytes(json);

    await using var stream = new MemoryStream(bytes);
    return await r2.UploadSinglePartAsync(bucket, key, stream, "application/json");
}
```

## Size Limits

| Upload Type | Maximum Size |
|-------------|--------------|
| Single-part (PUT) | 5 GiB |
| Multipart | 5 TiB |
| Minimum part size | 5 MiB |
| Maximum part size | 5 GiB |

## Related

- [Multipart Uploads](multipart.md) - Large file handling
- [Presigned URLs](presigned-urls.md) - Direct browser uploads
- [Downloading Objects](downloads.md) - Download files

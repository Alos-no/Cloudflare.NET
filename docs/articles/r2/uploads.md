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
- Files <= 5 GiB: Single PUT request
- Files > 5 GiB: Multipart upload

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
    return await r2.UploadSinglePartAsync(bucket, key, stream);
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

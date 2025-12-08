# Downloading Objects

Download objects from R2 buckets to local files or streams.

## Overview

```csharp
public class DownloadService(IR2Client r2)
{
    public async Task DownloadAsync(string key, string localPath)
    {
        var result = await r2.DownloadFileAsync("my-bucket", key, localPath);

        Console.WriteLine($"Downloaded {result.EgressBytes} bytes");
        Console.WriteLine($"Class B operations: {result.ClassBOperations}");
    }
}
```

## Download to File

```csharp
var result = await r2.DownloadFileAsync(
    bucketName: "my-bucket",
    objectKey: "documents/report.pdf",
    downloadPath: "/local/path/report.pdf");

Console.WriteLine($"Downloaded: {result.EgressBytes} bytes");
```

## Download to Stream

Download directly to a stream for processing:

```csharp
await using var fileStream = File.Create("/local/path/file.bin");

var result = await r2.DownloadFileAsync(
    bucketName: "my-bucket",
    objectKey: "data/file.bin",
    outputStream: fileStream);
```

### To Memory

```csharp
await using var memoryStream = new MemoryStream();

var result = await r2.DownloadFileAsync(
    bucketName: "my-bucket",
    objectKey: "config/settings.json",
    outputStream: memoryStream);

memoryStream.Position = 0;
var content = Encoding.UTF8.GetString(memoryStream.ToArray());
```

### To HTTP Response (ASP.NET)

```csharp
[HttpGet("files/{key}")]
public async Task<IActionResult> DownloadFile(string key)
{
    var stream = new MemoryStream();
    await r2.DownloadFileAsync("uploads", key, stream);
    stream.Position = 0;

    return File(stream, "application/octet-stream", Path.GetFileName(key));
}
```

## R2Result

Download operations return metrics:

```csharp
var result = await r2.DownloadFileAsync("bucket", "key", downloadPath);

Console.WriteLine($"Class A Operations: {result.ClassAOperations}"); // Always 0
Console.WriteLine($"Class B Operations: {result.ClassBOperations}"); // 1 for GET
Console.WriteLine($"Ingress Bytes: {result.IngressBytes}");          // Always 0
Console.WriteLine($"Egress Bytes: {result.EgressBytes}");            // Downloaded size
```

## Error Handling

```csharp
try
{
    await r2.DownloadFileAsync("bucket", "key", localPath);
}
catch (CloudflareR2OperationException ex)
{
    if (ex.Message.Contains("NoSuchKey"))
    {
        Console.WriteLine("Object not found");
    }
    else
    {
        Console.WriteLine($"Download failed: {ex.Message}");
    }
}
catch (IOException ex)
{
    Console.WriteLine($"Local file error: {ex.Message}");
}
```

## Common Patterns

### Download with Progress

```csharp
public async Task DownloadWithProgressAsync(
    string bucket, string key, string localPath, IProgress<long> progress)
{
    // Create a progress-tracking stream wrapper
    await using var fileStream = File.Create(localPath);
    await using var progressStream = new ProgressStream(fileStream, progress);

    await r2.DownloadFileAsync(bucket, key, progressStream);
}

// Simple progress stream
public class ProgressStream(Stream inner, IProgress<long> progress) : Stream
{
    private long _bytesWritten;

    public override void Write(byte[] buffer, int offset, int count)
    {
        inner.Write(buffer, offset, count);
        _bytesWritten += count;
        progress.Report(_bytesWritten);
    }

    // Implement other required Stream members...
}
```

### Download Multiple Files

```csharp
public async Task<R2Result> DownloadAllAsync(
    string bucket, string prefix, string localDir)
{
    var total = new R2Result();

    // First, list objects
    var listResult = await r2.ListObjectsAsync(bucket, prefix);
    total += listResult.Metrics;

    // Download each
    foreach (var obj in listResult.Data)
    {
        var relativePath = obj.Key.Substring(prefix.Length).TrimStart('/');
        var localPath = Path.Combine(localDir, relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);

        var result = await r2.DownloadFileAsync(bucket, obj.Key, localPath);
        total += result;

        Console.WriteLine($"Downloaded: {obj.Key}");
    }

    return total;
}
```

### Download with Retry

```csharp
public async Task<R2Result> DownloadWithRetryAsync(
    string bucket, string key, string localPath, int maxRetries = 3)
{
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            return await r2.DownloadFileAsync(bucket, key, localPath);
        }
        catch (CloudflareR2OperationException) when (attempt < maxRetries)
        {
            Console.WriteLine($"Attempt {attempt} failed, retrying...");
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
        }
    }

    throw new InvalidOperationException($"Download failed after {maxRetries} attempts");
}
```

### Download and Deserialize JSON

```csharp
public async Task<T?> DownloadJsonAsync<T>(string bucket, string key)
{
    await using var stream = new MemoryStream();
    await r2.DownloadFileAsync(bucket, key, stream);

    stream.Position = 0;
    return await JsonSerializer.DeserializeAsync<T>(stream);
}
```

### Check If Object Exists Before Download

```csharp
public async Task<bool> TryDownloadAsync(
    string bucket, string key, string localPath)
{
    try
    {
        await r2.DownloadFileAsync(bucket, key, localPath);
        return true;
    }
    catch (CloudflareR2OperationException ex) when (ex.Message.Contains("NoSuchKey"))
    {
        return false;
    }
}
```

## R2 Pricing Note

R2 has **free egress** - downloading files doesn't incur bandwidth charges. You only pay for Class B operations ($0.36 per million GET requests).

## Related

- [Uploading Objects](uploads.md) - Upload files to R2
- [Listing Objects](listing.md) - Find objects to download
- [Deleting Objects](deletes.md) - Remove downloaded objects

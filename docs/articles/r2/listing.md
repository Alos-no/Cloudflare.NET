# Listing Objects

Enumerate objects in R2 buckets with automatic pagination handling.

## Overview

```csharp
public class BrowserService(IR2Client r2)
{
    public async Task ListFilesAsync(string prefix)
    {
        var result = await r2.ListObjectsAsync("my-bucket", prefix);

        foreach (var obj in result.Data)
        {
            Console.WriteLine($"{obj.Key}: {obj.Size} bytes");
        }

        Console.WriteLine($"Class A operations: {result.Metrics.ClassAOperations}");
    }
}
```

## List All Objects

List all objects in a bucket:

```csharp
var result = await r2.ListObjectsAsync(
    bucketName: "my-bucket",
    prefix: null); // null for all objects

foreach (var obj in result.Data)
{
    Console.WriteLine($"Key: {obj.Key}");
    Console.WriteLine($"Size: {obj.Size} bytes");
    Console.WriteLine($"Modified: {obj.LastModified}");
    Console.WriteLine($"ETag: {obj.ETag}");
    Console.WriteLine();
}
```

## List by Prefix

Filter objects by key prefix:

```csharp
// List all objects under "documents/"
var result = await r2.ListObjectsAsync("my-bucket", "documents/");

// List all objects under "images/2024/"
var images = await r2.ListObjectsAsync("my-bucket", "images/2024/");
```

## S3Object Properties

The returned `S3Object` contains:

| Property | Type | Description |
|----------|------|-------------|
| `Key` | `string` | Object key (path) |
| `Size` | `long` | Object size in bytes |
| `LastModified` | `DateTime` | Last modification timestamp |
| `ETag` | `string` | Entity tag (content hash) |
| `StorageClass` | `string` | Storage class (STANDARD, INFREQUENT_ACCESS) |
| `Owner` | `Owner` | Object owner information |

## R2Result

Listing returns `R2Result<IReadOnlyList<S3Object>>`:

```csharp
var result = await r2.ListObjectsAsync("bucket", "prefix/");

// Access the data
var objects = result.Data;
Console.WriteLine($"Found {objects.Count} objects");

// Access the metrics
var metrics = result.Metrics;
Console.WriteLine($"Class A operations: {metrics.ClassAOperations}");
```

## Pagination

The SDK handles pagination automatically, fetching all pages:

```csharp
// This fetches ALL matching objects, regardless of how many pages
var result = await r2.ListObjectsAsync("my-bucket", null);

// Could be thousands of objects
Console.WriteLine($"Total objects: {result.Data.Count}");
Console.WriteLine($"List operations: {result.Metrics.ClassAOperations}"); // Multiple if paginated
```

## Error Handling

```csharp
try
{
    var result = await r2.ListObjectsAsync("bucket", "prefix/");
}
catch (CloudflareR2ListException<S3Object> ex)
{
    // Listing failed mid-pagination
    Console.WriteLine($"Listing failed: {ex.Message}");
    Console.WriteLine($"Partial data retrieved: {ex.PartialData.Count} objects");
    Console.WriteLine($"Partial metrics: {ex.PartialMetrics}");

    // You can still use the partial data
    foreach (var obj in ex.PartialData)
    {
        Console.WriteLine($"  {obj.Key}");
    }
}
catch (CloudflareR2OperationException ex)
{
    Console.WriteLine($"List operation failed: {ex.Message}");
}
```

## Common Patterns

### Get Total Size

```csharp
public async Task<long> GetTotalSizeAsync(string bucket, string? prefix = null)
{
    var result = await r2.ListObjectsAsync(bucket, prefix);
    return result.Data.Sum(o => o.Size);
}
```

### Find Files by Extension

```csharp
public async Task<IReadOnlyList<S3Object>> FindByExtensionAsync(
    string bucket, string extension)
{
    var result = await r2.ListObjectsAsync(bucket, null);

    return result.Data
        .Where(o => o.Key.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
        .ToList();
}
```

### Get Folder Structure

```csharp
public async Task<IReadOnlyList<string>> GetFoldersAsync(
    string bucket, string? prefix = null)
{
    var result = await r2.ListObjectsAsync(bucket, prefix);

    // Extract unique folder prefixes
    var folders = result.Data
        .Select(o => {
            var relativePath = prefix != null
                ? o.Key.Substring(prefix.Length)
                : o.Key;
            var slashIndex = relativePath.IndexOf('/');
            return slashIndex >= 0 ? relativePath.Substring(0, slashIndex) : null;
        })
        .Where(f => f != null)
        .Distinct()
        .OrderBy(f => f)
        .ToList();

    return folders!;
}
```

### Find Large Files

```csharp
public async Task<IReadOnlyList<S3Object>> FindLargeFilesAsync(
    string bucket, long minSizeBytes)
{
    var result = await r2.ListObjectsAsync(bucket, null);

    return result.Data
        .Where(o => o.Size >= minSizeBytes)
        .OrderByDescending(o => o.Size)
        .ToList();
}
```

### Find Recently Modified

```csharp
public async Task<IReadOnlyList<S3Object>> FindRecentAsync(
    string bucket, TimeSpan maxAge)
{
    var cutoff = DateTime.UtcNow - maxAge;
    var result = await r2.ListObjectsAsync(bucket, null);

    return result.Data
        .Where(o => o.LastModified >= cutoff)
        .OrderByDescending(o => o.LastModified)
        .ToList();
}
```

### Stream Processing for Large Buckets

For very large buckets, process objects as you go:

```csharp
public async Task ProcessAllObjectsAsync(
    string bucket, Func<S3Object, Task> processor)
{
    var result = await r2.ListObjectsAsync(bucket, null);

    foreach (var obj in result.Data)
    {
        await processor(obj);
    }
}
```

### Export Object List to CSV

```csharp
public async Task ExportToCsvAsync(string bucket, string csvPath)
{
    var result = await r2.ListObjectsAsync(bucket, null);

    using var writer = new StreamWriter(csvPath);
    await writer.WriteLineAsync("Key,Size,LastModified,ETag");

    foreach (var obj in result.Data)
    {
        await writer.WriteLineAsync(
            $"\"{obj.Key}\",{obj.Size},{obj.LastModified:O},{obj.ETag}");
    }
}
```

## List Multipart Upload Parts

List parts of an in-progress multipart upload:

```csharp
var result = await r2.ListPartsAsync(
    bucketName: "my-bucket",
    objectKey: "large-file.bin",
    uploadId: "upload-id-here");

foreach (var part in result.Data)
{
    Console.WriteLine($"Part {part.PartNumber}: {part.Size} bytes, ETag: {part.ETag}");
}
```

## R2 Pricing Note

List operations are **Class A** operations ($4.50 per million). Each page of results counts as one operation.

## Related

- [Deleting Objects](deletes.md) - Delete listed objects
- [Downloading Objects](downloads.md) - Download found objects
- [Multipart Uploads](multipart.md) - List multipart parts

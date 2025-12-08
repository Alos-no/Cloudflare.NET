# Deleting Objects

Delete operations in R2 are **free** - they don't count against your operation quota.

## Overview

```csharp
public class CleanupService(IR2Client r2)
{
    public async Task DeleteFileAsync(string key)
    {
        await r2.DeleteObjectAsync("my-bucket", key);
    }

    public async Task ClearTempFilesAsync()
    {
        var objects = await r2.ListObjectsAsync("my-bucket", "temp/");
        var keys = objects.Data.Select(o => o.Key);

        await r2.DeleteObjectsAsync("my-bucket", keys);
    }
}
```

## Single Object Delete

Delete a single object by key:

```csharp
var result = await r2.DeleteObjectAsync(
    bucketName: "my-bucket",
    objectKey: "documents/old-report.pdf");

// Result metrics will be zero (free operation)
Console.WriteLine($"Class A operations: {result.ClassAOperations}"); // 0
```

> [!NOTE]
> Deleting a non-existent key succeeds silently without error.

## Batch Delete

Delete multiple objects efficiently in batches (up to 1000 per request):

```csharp
var keysToDelete = new[]
{
    "temp/file1.txt",
    "temp/file2.txt",
    "temp/file3.txt"
};

var result = await r2.DeleteObjectsAsync(
    bucketName: "my-bucket",
    objectKeys: keysToDelete);
```

### Continue on Error

By default, batch delete continues even if some objects fail:

```csharp
// Will delete all possible objects, throwing at the end if any failed
var result = await r2.DeleteObjectsAsync(
    bucketName: "my-bucket",
    objectKeys: keysToDelete,
    continueOnError: true);
```

### Stop on First Error

```csharp
// Stops immediately on first failure
try
{
    var result = await r2.DeleteObjectsAsync(
        bucketName: "my-bucket",
        objectKeys: keysToDelete,
        continueOnError: false);
}
catch (CloudflareR2BatchException<string> ex)
{
    Console.WriteLine($"Failed to delete: {string.Join(", ", ex.FailedItems)}");
}
```

## Clear Bucket

Delete **all** objects in a bucket:

```csharp
var result = await r2.ClearBucketAsync("my-bucket");

Console.WriteLine($"Cleared bucket with {result.ClassAOperations} list operations");
```

> [!WARNING]
> This operation is irreversible. All objects in the bucket will be permanently deleted.

### With Continue on Error

```csharp
try
{
    await r2.ClearBucketAsync("my-bucket", continueOnError: true);
}
catch (CloudflareR2BatchException<string> ex)
{
    Console.WriteLine($"Some objects could not be deleted: {ex.FailedItems.Count}");
}
```

## Error Handling

### Batch Delete Exceptions

```csharp
try
{
    await r2.DeleteObjectsAsync("bucket", keysToDelete);
}
catch (CloudflareR2BatchException<string> ex)
{
    Console.WriteLine($"Operation partially failed");
    Console.WriteLine($"Failed keys: {string.Join(", ", ex.FailedItems)}");
    Console.WriteLine($"Partial metrics: {ex.PartialMetrics}");
}
```

### Clear Bucket Exceptions

```csharp
try
{
    await r2.ClearBucketAsync("bucket");
}
catch (CloudflareR2ListException<S3Object> ex)
{
    // Listing failed mid-stream
    Console.WriteLine($"Objects found before failure: {ex.PartialData.Count}");
}
catch (CloudflareR2BatchException<string> ex)
{
    // Delete batch failed
    Console.WriteLine($"Objects that couldn't be deleted: {ex.FailedItems.Count}");
}
```

## Common Patterns

### Delete by Prefix

```csharp
public async Task DeleteByPrefixAsync(string bucket, string prefix)
{
    var listResult = await r2.ListObjectsAsync(bucket, prefix);
    var keys = listResult.Data.Select(o => o.Key);

    await r2.DeleteObjectsAsync(bucket, keys);

    Console.WriteLine($"Deleted {listResult.Data.Count} objects");
}
```

### Delete Old Files

```csharp
public async Task DeleteOlderThanAsync(string bucket, TimeSpan maxAge)
{
    var cutoff = DateTime.UtcNow - maxAge;
    var listResult = await r2.ListObjectsAsync(bucket, null);

    var oldKeys = listResult.Data
        .Where(o => o.LastModified < cutoff)
        .Select(o => o.Key)
        .ToList();

    if (oldKeys.Any())
    {
        await r2.DeleteObjectsAsync(bucket, oldKeys);
        Console.WriteLine($"Deleted {oldKeys.Count} old objects");
    }
}
```

### Safe Delete with Confirmation

```csharp
public async Task<int> DeleteWithConfirmationAsync(
    string bucket, string prefix, Func<IReadOnlyList<S3Object>, bool> confirm)
{
    var listResult = await r2.ListObjectsAsync(bucket, prefix);

    if (!listResult.Data.Any())
    {
        Console.WriteLine("No objects found");
        return 0;
    }

    if (!confirm(listResult.Data))
    {
        Console.WriteLine("Delete cancelled");
        return 0;
    }

    var keys = listResult.Data.Select(o => o.Key);
    await r2.DeleteObjectsAsync(bucket, keys);

    return listResult.Data.Count;
}
```

### Delete Multipart Upload Parts

When you need to clean up incomplete multipart uploads:

```csharp
public async Task CleanupIncompleteUploadAsync(
    string bucket, string key, string uploadId)
{
    // Abort the multipart upload (also deletes uploaded parts)
    await r2.AbortMultipartUploadAsync(bucket, key, uploadId);
}
```

## R2Result for Deletes

Delete operations return metrics, but they will always be zero since deletes are free:

```csharp
var result = await r2.DeleteObjectAsync("bucket", "key");

Console.WriteLine($"Class A: {result.ClassAOperations}"); // 0
Console.WriteLine($"Class B: {result.ClassBOperations}"); // 0
Console.WriteLine($"Ingress: {result.IngressBytes}");     // 0
Console.WriteLine($"Egress: {result.EgressBytes}");       // 0
```

## Batch Size

The SDK automatically batches delete requests. R2 supports up to 1000 objects per delete request.

```csharp
// Even 10,000 keys will be batched into 10 requests
var manyKeys = Enumerable.Range(1, 10000).Select(i => $"file{i}.txt");
await r2.DeleteObjectsAsync("bucket", manyKeys);
```

## Related

- [Listing Objects](listing.md) - Find objects to delete
- [Lifecycle Policies](../accounts/r2/lifecycle.md) - Auto-delete with rules
- [Uploading Objects](uploads.md) - Add new objects

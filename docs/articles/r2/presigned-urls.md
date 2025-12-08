# Presigned URLs

Generate secure, time-limited URLs that allow clients to upload directly to R2 without exposing credentials.

## Overview

```csharp
public class PresignedService(IR2Client r2)
{
    public string GenerateUploadUrl(string key, long fileSize, string contentType)
    {
        return r2.CreatePresignedPutUrl("my-bucket", new PresignedPutRequest(
            Key: key,
            ExpiresAfter: TimeSpan.FromMinutes(15),
            ContentLength: fileSize,
            ContentType: contentType
        ));
    }
}
```

## Single-Part Presigned URL

Generate a URL for direct file upload:

```csharp
var url = r2.CreatePresignedPutUrl("my-bucket", new PresignedPutRequest(
    Key: "uploads/document.pdf",
    ExpiresAfter: TimeSpan.FromMinutes(30),
    ContentLength: 1024 * 1024 * 10, // 10 MB
    ContentType: "application/pdf"
));

Console.WriteLine($"Upload URL: {url}");
```

### PresignedPutRequest Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `Key` | `string` | Yes | Object key (path) |
| `ExpiresAfter` | `TimeSpan` | Yes | URL validity duration |
| `ContentLength` | `long` | Yes | Exact file size in bytes |
| `ContentType` | `string` | Yes | MIME type of the file |
| `Conditions` | `IEnumerable<S3PostCondition>?` | No | Additional S3 conditions |
| `HeadersToSign` | `IReadOnlyDictionary<string, string>?` | No | Headers to include in signature |

## Using Presigned URLs

### Server-Side (Generate URL)

```csharp
[HttpPost("upload-url")]
public IActionResult GetUploadUrl([FromBody] UploadRequest request)
{
    var url = r2.CreatePresignedPutUrl("uploads", new PresignedPutRequest(
        Key: $"{Guid.NewGuid()}/{request.FileName}",
        ExpiresAfter: TimeSpan.FromMinutes(15),
        ContentLength: request.FileSize,
        ContentType: request.ContentType
    ));

    return Ok(new { uploadUrl = url });
}
```

### Client-Side (JavaScript)

```javascript
// Get presigned URL from your API
const { uploadUrl } = await fetch('/api/upload-url', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
        fileName: file.name,
        fileSize: file.size,
        contentType: file.type
    })
}).then(r => r.json());

// Upload directly to R2
await fetch(uploadUrl, {
    method: 'PUT',
    headers: {
        'Content-Type': file.type,
        'Content-Length': file.size
    },
    body: file
});
```

## Multipart Presigned URLs

Generate presigned URLs for multipart upload parts:

### Single Part

```csharp
var partUrl = r2.CreatePresignedUploadPartUrl("my-bucket", new PresignedUploadPartRequest(
    Key: "uploads/large-file.zip",
    UploadId: "your-upload-id",
    PartNumber: 1,
    ExpiresAfter: TimeSpan.FromMinutes(60),
    ContentLength: 100 * 1024 * 1024, // 100 MiB
    ContentType: "application/octet-stream"
));
```

### Batch Presigned URLs

Generate URLs for all parts at once:

```csharp
var partUrls = r2.CreatePresignedUploadPartsUrls("my-bucket", new PresignedUploadPartsRequest(
    Key: "uploads/large-file.zip",
    UploadId: "your-upload-id",
    PartNumbers: Enumerable.Range(1, 10).ToArray(), // Parts 1-10
    ExpiresAfter: TimeSpan.FromHours(1),
    ContentLength: 100 * 1024 * 1024, // Each part is 100 MiB
    ContentType: "application/octet-stream"
));

foreach (var (partNumber, url) in partUrls)
{
    Console.WriteLine($"Part {partNumber}: {url}");
}
```

## Signed Headers

Include additional headers in the signature to enforce constraints:

```csharp
var url = r2.CreatePresignedPutUrl("my-bucket", new PresignedPutRequest(
    Key: "uploads/image.jpg",
    ExpiresAfter: TimeSpan.FromMinutes(15),
    ContentLength: fileSize,
    ContentType: "image/jpeg",
    HeadersToSign: new Dictionary<string, string>
    {
        ["x-amz-meta-user-id"] = "user-123",
        ["x-amz-meta-upload-source"] = "web-app"
    }
));

// Client must include these headers when uploading
```

## Common Patterns

### Secure File Upload API

```csharp
public class SecureUploadService(IR2Client r2)
{
    public UploadSession CreateUploadSession(
        string userId, string fileName, long fileSize, string contentType)
    {
        var key = $"user-uploads/{userId}/{Guid.NewGuid()}/{SanitizeFileName(fileName)}";

        var url = r2.CreatePresignedPutUrl("uploads", new PresignedPutRequest(
            Key: key,
            ExpiresAfter: TimeSpan.FromMinutes(30),
            ContentLength: fileSize,
            ContentType: contentType,
            HeadersToSign: new Dictionary<string, string>
            {
                ["x-amz-meta-user-id"] = userId,
                ["x-amz-meta-original-name"] = fileName
            }
        ));

        return new UploadSession(key, url, DateTime.UtcNow.AddMinutes(30));
    }

    private static string SanitizeFileName(string fileName)
    {
        // Remove dangerous characters
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", fileName.Split(invalid));
    }
}

public record UploadSession(string Key, string UploadUrl, DateTime ExpiresAt);
```

### Browser Multipart Upload Flow

```csharp
public class MultipartUploadSession(IR2Client r2)
{
    public async Task<MultipartUploadInfo> InitiateAsync(
        string bucket, string key, long fileSize, long partSize)
    {
        // Calculate number of parts
        var partCount = (int)Math.Ceiling((double)fileSize / partSize);

        // Initiate multipart upload
        var initResult = await r2.InitiateMultipartUploadAsync(bucket, key);
        var uploadId = initResult.Data;

        // Generate presigned URLs for all parts
        var partUrls = r2.CreatePresignedUploadPartsUrls(bucket, new PresignedUploadPartsRequest(
            Key: key,
            UploadId: uploadId,
            PartNumbers: Enumerable.Range(1, partCount).ToArray(),
            ExpiresAfter: TimeSpan.FromHours(24),
            ContentLength: partSize,
            ContentType: "application/octet-stream"
        ));

        return new MultipartUploadInfo(uploadId, partUrls, partSize);
    }

    public async Task CompleteAsync(
        string bucket, string key, string uploadId, IEnumerable<PartETag> parts)
    {
        await r2.CompleteMultipartUploadAsync(bucket, key, uploadId, parts);
    }

    public async Task AbortAsync(string bucket, string key, string uploadId)
    {
        await r2.AbortMultipartUploadAsync(bucket, key, uploadId);
    }
}

public record MultipartUploadInfo(
    string UploadId,
    IReadOnlyDictionary<int, string> PartUrls,
    long PartSize
);
```

### Image Upload with Validation

```csharp
public class ImageUploadService(IR2Client r2)
{
    private static readonly HashSet<string> AllowedTypes = new()
    {
        "image/jpeg", "image/png", "image/gif", "image/webp"
    };

    private const long MaxImageSize = 10 * 1024 * 1024; // 10 MB

    public string? CreateImageUploadUrl(
        string userId, string contentType, long contentLength)
    {
        if (!AllowedTypes.Contains(contentType))
        {
            return null; // Invalid content type
        }

        if (contentLength > MaxImageSize)
        {
            return null; // Too large
        }

        var extension = contentType.Split('/')[1];
        var key = $"images/{userId}/{Guid.NewGuid()}.{extension}";

        return r2.CreatePresignedPutUrl("media", new PresignedPutRequest(
            Key: key,
            ExpiresAfter: TimeSpan.FromMinutes(10),
            ContentLength: contentLength,
            ContentType: contentType
        ));
    }
}
```

## URL Expiration

| Duration | Use Case |
|----------|----------|
| 5-15 minutes | Interactive uploads |
| 1 hour | Background processing |
| 24 hours | Long-running multipart uploads |
| 7 days (max) | Batch processing |

> [!WARNING]
> Keep expiration times short to minimize security risk.

## Security Considerations

1. **Always validate file metadata** before generating URLs
2. **Use short expiration times** when possible
3. **Enforce Content-Length** to prevent quota attacks
4. **Validate Content-Type** to prevent wrong file types
5. **Include user context** in signed headers for auditing
6. **Rate limit** URL generation to prevent abuse

## Error Handling

```csharp
try
{
    var url = r2.CreatePresignedPutUrl("bucket", request);
}
catch (CloudflareR2OperationException ex)
{
    Console.WriteLine($"Failed to generate URL: {ex.Message}");
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Invalid parameters: {ex.Message}");
}
```

## Related

- [Uploading Objects](uploads.md) - Server-side uploads
- [Multipart Uploads](multipart.md) - Large file handling
- [CORS Configuration](../accounts/r2/cors.md) - Enable browser uploads

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
| `Checksum` | `UploadChecksum?` | No | Digest the uploaded bytes must hash to; see [Checksum Verification](#checksum-verification) |

## Presigned Download URL

`CreatePresignedGetUrl` produces a URL that lets the holder download one object without a Cloudflare
credential and without your service relaying the bytes. Use it to hand a private object to a browser,
a mobile client, or a third party for a limited window:

```csharp
var url = r2.CreatePresignedGetUrl("my-bucket", new PresignedGetRequest(
    Key: "invoices/2026-08.pdf",
    ExpiresAfter: TimeSpan.FromMinutes(15)
));

Console.WriteLine($"Download URL: {url}");
```

### PresignedGetRequest Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `Key` | `string` | Yes | Object key (path) |
| `ExpiresAfter` | `TimeSpan` | Yes | URL validity duration |
| `ResponseContentType` | `string?` | No | Overrides the `Content-Type` header R2 returns |
| `ResponseContentDisposition` | `string?` | No | Overrides the `Content-Disposition` header R2 returns |

### Controlling the Response Headers

The two optional properties change the headers R2 sends when the URL is fetched, without touching the
stored object. This is how you make a browser download a file under a different name, or open it
inline rather than saving it:

```csharp
// The browser saves the file as "August invoice.pdf" no matter what the key is.
var url = r2.CreatePresignedGetUrl("my-bucket", new PresignedGetRequest(
    Key: "invoices/8f2c1a90.bin",
    ExpiresAfter: TimeSpan.FromMinutes(5),
    ResponseContentType: "application/pdf",
    ResponseContentDisposition: "attachment; filename=\"August invoice.pdf\""
));
```

Both values are part of the signature, so a recipient cannot edit them in the URL. Changing either
one invalidates the signature and R2 rejects the request.

### After Expiry

Once `ExpiresAfter` has elapsed, R2 answers the URL with HTTP 403. Generate a fresh URL rather than
issuing long-lived ones:

```csharp
[HttpGet("download/{id}")]
public IActionResult GetDownloadUrl(string id)
{
    var url = r2.CreatePresignedGetUrl("documents", new PresignedGetRequest(
        Key: $"user-uploads/{id}",
        ExpiresAfter: TimeSpan.FromMinutes(10)
    ));

    return Redirect(url);
}
```

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

Generate URLs for all parts at once. Each part is given by its part number and its exact size in bytes,
because the size is signed into that part's URL:

```csharp
const long partSize = 100L * 1024 * 1024; // 100 MiB

var partUrls = r2.CreatePresignedUploadPartsUrls("my-bucket", new PresignedUploadPartsRequest(
    Key: "uploads/large-file.zip",
    UploadId: "your-upload-id",
    ExpiresAfter: TimeSpan.FromHours(1),
    PartNumberAndLength: Enumerable.Range(1, 10).ToDictionary(n => n, _ => partSize)
));

foreach (var (partNumber, url) in partUrls)
{
    Console.WriteLine($"Part {partNumber}: {url}");
}
```

Every part except the last must be the same size, and each part must fall between 5 MiB and 5 GiB. The
method throws `ArgumentException` before signing anything if those rules are broken.

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

A signed header is not a suggestion. The client's request is rejected unless it sends the header with
exactly the signed value, because the header name appears in the URL's `X-Amz-SignedHeaders` list and
its value feeds the signature. Omitting it, or sending a different value, produces a different
signature and R2 answers 403 with `SignatureDoesNotMatch`. Nothing is stored.

### Requiring Server-Side Encryption

The same mechanism can force a client to declare server-side encryption on every upload:

```csharp
var encryptionHeader = new Dictionary<string, string>
{
    ["x-amz-server-side-encryption"] = "AES256"
};

var putUrl = r2.CreatePresignedPutUrl("my-bucket", new PresignedPutRequest(
    Key: "uploads/document.pdf",
    ExpiresAfter: TimeSpan.FromMinutes(15),
    ContentLength: fileSize,
    ContentType: "application/pdf",
    HeadersToSign: encryptionHeader
));

var partUrl = r2.CreatePresignedUploadPartUrl("my-bucket", new PresignedUploadPartRequest(
    Key: "uploads/large-file.zip",
    UploadId: uploadId,
    PartNumber: 1,
    ExpiresAfter: TimeSpan.FromHours(1),
    ContentLength: partSize,
    ContentType: "application/octet-stream",
    HeadersToSign: encryptionHeader
));
```

R2 accepts this header on both a single-part PUT and a part upload, and a multipart upload whose parts
all carry it assembles normally. R2 does not echo the header back in its response.

> [!NOTE]
> R2 encrypts every object at rest whether or not this header is sent, so signing it does not change
> how the object is stored. What it changes is that a client cannot upload without presenting it.

## Checksum Verification

Bind a presigned upload to a digest of the expected bytes. The digest's header is signed into the URL,
so the client must send it, and R2 hashes the bytes that actually arrive: when they do not hash to the
stated digest, R2 rejects the upload with 400 `BadDigest` and stores nothing.

```csharp
// Compute the digest locally, then sign it into the URL.
var checksum = UploadChecksum.FromDigestBytes(R2ChecksumAlgorithm.Sha256, SHA256.HashData(fileBytes));

var url = r2.CreatePresignedPutUrl("my-bucket", new PresignedPutRequest(
    Key: "uploads/document.pdf",
    ExpiresAfter: TimeSpan.FromMinutes(15),
    ContentLength: fileBytes.Length,
    ContentType: "application/pdf",
    Checksum: checksum
));

// The client must now send: x-amz-checksum-sha256: <the base64 digest>
```

`R2ChecksumAlgorithm` carries everything a caller would otherwise have to look up: the wire name, the
header the digest travels in, and the exact digest length. The set is closed to the algorithms R2 was
verified to enforce (verified against live R2, 2026-08-24, and pinned by this repository's integration
tests):

| Algorithm | Header | Digest bytes | Single-part PUT | Multipart part upload |
|-----------|--------|--------------|-----------------|-----------------------|
| `Crc32` | `x-amz-checksum-crc32` | 4 | Verified | Verified |
| `Crc32C` | `x-amz-checksum-crc32c` | 4 | Verified | Verified |
| `Sha1` | `x-amz-checksum-sha1` | 20 | Verified | Rejected: R2 answers 501 `NotImplemented` |
| `Sha256` | `x-amz-checksum-sha256` | 32 | Verified | Rejected: R2 answers 501 `NotImplemented` |
| `Md5` | `Content-MD5` | 16 | Verified | Verified |

Because R2 answers 501 to any part upload carrying a SHA-1 or SHA-256 checksum header, the part URL
methods refuse those two algorithms with `ArgumentException` before signing anything. Use `Crc32`,
`Crc32C` or `Md5` for parts.

`UploadChecksum` validates at construction: the digest must be well-formed base64 decoding to exactly
the algorithm's digest length, so a URL is never signed for a digest R2 could not accept. For untrusted
input, such as an algorithm name and digest arriving in an API request, use `TryCreate`:

```csharp
if (!UploadChecksum.TryCreate(request.Algorithm, request.Digest, out var checksum))
    return BadRequest("Unknown checksum algorithm or malformed digest.");
```

The batch part URL generator takes one digest per part, keyed by part number. A part without an entry
gets no checksum header; a part number missing from `PartNumberAndLength` fails the whole call:

```csharp
var partUrls = r2.CreatePresignedUploadPartsUrls("my-bucket", new PresignedUploadPartsRequest(
    Key: "uploads/large-file.zip",
    UploadId: uploadId,
    ExpiresAfter: TimeSpan.FromHours(1),
    PartNumberAndLength: partSizes,
    ChecksumsByPartNumber: partDigests // IReadOnlyDictionary<int, UploadChecksum>
));
```

> [!NOTE]
> `Content-MD5` is a content header in `HttpClient`'s model: attach it to
> `HttpContent.Headers`, not `HttpRequestMessage.Headers`. The `x-amz-checksum-*` headers are plain
> request headers.

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

        // Generate presigned URLs for all parts. Each part is named with its exact size, because the
        // size is signed into that part's URL.
        var partUrls = r2.CreatePresignedUploadPartsUrls(bucket, new PresignedUploadPartsRequest(
            Key: key,
            UploadId: uploadId,
            ExpiresAfter: TimeSpan.FromHours(24),
            PartNumberAndLength: Enumerable.Range(1, partCount).ToDictionary(n => n, _ => partSize)
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

# Cloudflare.NET.R2

This package provides a high-level client for interacting with Cloudflare's R2 object storage, which is compatible with the S3 API. It is built on top of the official AWS SDK for .NET but is tailored specifically for R2, providing helper methods, custom exceptions, and detailed metric tracking for Cloudflare's billing model (Class A/B operations, ingress/egress).

## 1. Installation

Install the package from NuGet:

```bash
dotnet add package Cloudflare.NET.R2
```

This package depends on `Cloudflare.NET`, which will be installed as a dependency if not already present.

## 2. Credentials vs. API Tokens

Understand the two types of credentials used:

1.  **R2 S3 Credentials (Access Key ID & Secret):** Used for **data plane** operations like uploading, downloading, and listing objects. You generate these from the R2 section of the Cloudflare dashboard. **This is what the `Cloudflare.NET.R2` client uses.**
2.  **Cloudflare API Token:** Used for **management plane** operations like creating or deleting buckets. This is the standard API token used by the core `Cloudflare.NET` library's `IAccountsApi`.

## 3. Usage

### 3.1 Configuration

In your `appsettings.json`, configure both the core Cloudflare settings (for the Account ID) and the R2-specific credentials.

```json
{
  "Cloudflare": {
    "ApiToken": "your-cloudflare-api-token-for-management",
    "AccountId": "your-cloudflare-account-id"
  },
  "R2": {
    "AccessKeyId": "your-r2-access-key-id",
    "SecretAccessKey": "your-r2-secret-access-key"
    // EndpointUrl and Region are optional and have sensible defaults.
    // "EndpointUrl": "https://{0}.r2.cloudflarestorage.com",
    // "Region": "auto",
  }
}
```

### 3.2 Register Services

In your application's service configuration (e.g., `Program.cs`), register the R2 client. Also register the core client if you need to manage buckets.

```csharp
using Cloudflare.NET.Core;
using Cloudflare.NET.R2;

var builder = WebApplication.CreateBuilder(args);

// (Optional) Register the core Cloudflare client (for bucket management)
builder.Services.AddCloudflareApiClient(builder.Configuration);

// Register the R2 client for object operations
builder.Services.AddCloudflareR2Client(builder.Configuration);

// ... rest of your service configuration
```

### 3.3 Example: Uploading a File

Inject `IR2Client` into your services to interact with R2. The client automatically handles error wrapping and tracks billing metrics.

```csharp
using Cloudflare.NET.R2;
using Cloudflare.NET.R2.Exceptions;
using Cloudflare.NET.R2.Models;

public class MyStorageService(IR2Client r2Client)
{
    public async Task UploadFile(string bucketName, string objectKey, string filePath)
    {
        try
        {
            // UploadAsync automatically chooses between single and multipart upload.
            R2Result result = await r2Client.UploadAsync(bucketName, objectKey, filePath);

            Console.WriteLine($"Upload complete. Consumed {result.ClassAOperations} Class A operations.");
            Console.WriteLine($"Ingress: {result.IngressBytes} bytes.");
        }
        catch (CloudflareR2OperationException ex)
        {
            // Custom exceptions provide partial metrics for failed operations.
            Console.WriteLine($"Upload failed. Partial metrics: {ex.PartialMetrics}");
            // Handle error...
        }
    }
}
```

### 4. Advanced Features

#### Multipart Uploads

The `UploadAsync` method automatically handles multipart uploads for large files (by default, > 50 MiB). You can also call `UploadMultipartAsync` directly for more control, such as specifying a custom part size.

```csharp
// Upload a large file with 10 MiB parts
var result = await r2Client.UploadMultipartAsync(bucketName, "large-object.zip", stream, 10 * 1024 * 1024);
Console.WriteLine($"Multipart upload complete. Consumed {result.ClassAOperations} Class A operations (init + parts + complete).");
```

#### Presigned URLs

Generate temporary, secure URLs for direct browser uploads or downloads. This is ideal for client-side applications as it offloads the upload process from your server.

```csharp
using Cloudflare.NET.R2.Models;

// Generate a URL that's valid for 10 minutes for uploading a 1024-byte text file.
var presignedPutRequest = new PresignedPutRequest(
    Key: "user-uploads/file.txt",
    ExpiresAfter: TimeSpan.FromMinutes(10),
    ContentLength: 1024,
    ContentType: "text/plain"
);

string uploadUrl = r2Client.CreatePresignedPutUrl(bucketName, presignedPutRequest);

// This URL can now be sent to a client application to perform a PUT request.
```


#### Clearing a Bucket

The `ClearBucketAsync` helper method handles pagination and batch deletion to remove all objects from a bucket.

```csharp
R2Result clearMetrics = await r2Client.ClearBucketAsync("my-bucket-to-empty");
Console.WriteLine($"Bucket clear finished. Consumed {clearMetrics.ClassAOperations} list operations.");
```

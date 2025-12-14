# R2 Temporary Credentials

Create time-limited, scoped credentials for R2 access. Temporary credentials are useful for granting limited access to specific buckets, prefixes, or objects without exposing your main API credentials.

## Overview

```csharp
public class CredentialService(ICloudflareApiClient cf)
{
    public async Task<TempCredentials> GetUploadCredentialsAsync(
        string bucket,
        string parentKeyId,
        int ttlSeconds = 3600)
    {
        var request = new CreateTempCredentialsRequest(
            Bucket: bucket,
            ParentAccessKeyId: parentKeyId,
            Permission: TempCredentialPermission.ObjectWriteOnly,
            TtlSeconds: ttlSeconds
        );

        return await cf.Accounts.Buckets.CreateTempCredentialsAsync(request);
    }
}
```

## Creating Temporary Credentials

### Read-Only Access

Grant read access to a specific bucket:

```csharp
var credentials = await cf.Accounts.Buckets.CreateTempCredentialsAsync(
    new CreateTempCredentialsRequest(
        Bucket: "my-bucket",
        ParentAccessKeyId: "your-r2-access-key-id",
        Permission: TempCredentialPermission.ObjectReadOnly,
        TtlSeconds: 3600  // 1 hour
    )
);

Console.WriteLine($"Access Key: {credentials.AccessKeyId}");
Console.WriteLine($"Secret Key: {credentials.SecretAccessKey}");
Console.WriteLine($"Session Token: {credentials.SessionToken}");
```

### Write-Only Access

Grant write access for uploads:

```csharp
var credentials = await cf.Accounts.Buckets.CreateTempCredentialsAsync(
    new CreateTempCredentialsRequest(
        Bucket: "uploads-bucket",
        ParentAccessKeyId: "your-r2-access-key-id",
        Permission: TempCredentialPermission.ObjectWriteOnly,
        TtlSeconds: 1800  // 30 minutes
    )
);
```

### Read-Write Access

Grant full object access:

```csharp
var credentials = await cf.Accounts.Buckets.CreateTempCredentialsAsync(
    new CreateTempCredentialsRequest(
        Bucket: "my-bucket",
        ParentAccessKeyId: "your-r2-access-key-id",
        Permission: TempCredentialPermission.ObjectReadWrite,
        TtlSeconds: 7200  // 2 hours
    )
);
```

### Admin Access

Grant administrative access (includes bucket operations):

```csharp
// Read-only admin access
var readAdmin = await cf.Accounts.Buckets.CreateTempCredentialsAsync(
    new CreateTempCredentialsRequest(
        Bucket: "my-bucket",
        ParentAccessKeyId: "your-r2-access-key-id",
        Permission: TempCredentialPermission.AdminReadOnly,
        TtlSeconds: 3600
    )
);

// Full admin access
var fullAdmin = await cf.Accounts.Buckets.CreateTempCredentialsAsync(
    new CreateTempCredentialsRequest(
        Bucket: "my-bucket",
        ParentAccessKeyId: "your-r2-access-key-id",
        Permission: TempCredentialPermission.AdminReadWrite,
        TtlSeconds: 3600
    )
);
```

### Prefix-Scoped Access

Restrict access to objects with specific prefixes:

```csharp
var credentials = await cf.Accounts.Buckets.CreateTempCredentialsAsync(
    new CreateTempCredentialsRequest(
        Bucket: "my-bucket",
        ParentAccessKeyId: "your-r2-access-key-id",
        Permission: TempCredentialPermission.ObjectReadWrite,
        TtlSeconds: 3600,
        Prefixes: ["users/123/", "shared/"]
    )
);
```

### Object-Specific Access

Restrict access to specific objects:

```csharp
var credentials = await cf.Accounts.Buckets.CreateTempCredentialsAsync(
    new CreateTempCredentialsRequest(
        Bucket: "my-bucket",
        ParentAccessKeyId: "your-r2-access-key-id",
        Permission: TempCredentialPermission.ObjectReadOnly,
        TtlSeconds: 3600,
        Objects: ["documents/report.pdf", "documents/summary.pdf"]
    )
);
```

## Using Temporary Credentials

### With AWS SDK for .NET

```csharp
using Amazon.S3;
using Amazon.Runtime;

var sessionCredentials = new SessionAWSCredentials(
    credentials.AccessKeyId,
    credentials.SecretAccessKey,
    credentials.SessionToken
);

var config = new AmazonS3Config
{
    ServiceURL = $"https://{accountId}.r2.cloudflarestorage.com"
};

using var s3Client = new AmazonS3Client(sessionCredentials, config);

// Use s3Client for operations...
```

### With R2 Client

```csharp
// Configure R2 client with temporary credentials
var r2Settings = new R2Settings
{
    AccessKeyId = credentials.AccessKeyId,
    SecretAccessKey = credentials.SecretAccessKey,
    SessionToken = credentials.SessionToken,
    EndpointUrl = $"https://{accountId}.r2.cloudflarestorage.com"
};

// Use R2 client...
```

## Models Reference

### CreateTempCredentialsRequest

| Property | Type | Description |
|----------|------|-------------|
| `Bucket` | `string` | Target bucket name |
| `ParentAccessKeyId` | `string` | Parent R2 access key ID to derive from |
| `Permission` | `TempCredentialPermission` | Permission level |
| `TtlSeconds` | `int` | Credential lifetime in seconds |
| `Prefixes` | `IReadOnlyList<string>?` | Optional prefix restrictions |
| `Objects` | `IReadOnlyList<string>?` | Optional specific object restrictions |

### TempCredentials

| Property | Type | Description |
|----------|------|-------------|
| `AccessKeyId` | `string` | Temporary access key ID |
| `SecretAccessKey` | `string` | Temporary secret access key |
| `SessionToken` | `string` | Session token for authentication |

### TempCredentialPermission

| Value | Description |
|-------|-------------|
| `ObjectReadOnly` | Read objects only |
| `ObjectWriteOnly` | Write objects only |
| `ObjectReadWrite` | Read and write objects |
| `AdminReadOnly` | Admin read (includes bucket metadata) |
| `AdminReadWrite` | Full admin access |

## Common Patterns

### Client Upload Credentials

Generate credentials for client-side uploads:

```csharp
public async Task<UploadCredentials> GetClientUploadCredentialsAsync(
    string userId,
    string bucket)
{
    var credentials = await cf.Accounts.Buckets.CreateTempCredentialsAsync(
        new CreateTempCredentialsRequest(
            Bucket: bucket,
            ParentAccessKeyId: _r2AccessKeyId,
            Permission: TempCredentialPermission.ObjectWriteOnly,
            TtlSeconds: 900,  // 15 minutes
            Prefixes: [$"uploads/{userId}/"]
        )
    );

    return new UploadCredentials(
        credentials.AccessKeyId,
        credentials.SecretAccessKey,
        credentials.SessionToken,
        ExpiresAt: DateTime.UtcNow.AddSeconds(900)
    );
}
```

### Download Service

Generate read credentials for downloads:

```csharp
public async Task<TempCredentials> GetDownloadCredentialsAsync(
    string bucket,
    IEnumerable<string> objectKeys)
{
    return await cf.Accounts.Buckets.CreateTempCredentialsAsync(
        new CreateTempCredentialsRequest(
            Bucket: bucket,
            ParentAccessKeyId: _r2AccessKeyId,
            Permission: TempCredentialPermission.ObjectReadOnly,
            TtlSeconds: 3600,
            Objects: objectKeys.ToList()
        )
    );
}
```

### Scoped Service Account

Create credentials for a background service:

```csharp
public async Task<TempCredentials> GetServiceCredentialsAsync(
    string bucket,
    string servicePrefix)
{
    return await cf.Accounts.Buckets.CreateTempCredentialsAsync(
        new CreateTempCredentialsRequest(
            Bucket: bucket,
            ParentAccessKeyId: _r2AccessKeyId,
            Permission: TempCredentialPermission.ObjectReadWrite,
            TtlSeconds: 43200,  // 12 hours
            Prefixes: [servicePrefix]
        )
    );
}
```

## Required Permissions

| Permission | Scope | Level |
|------------|-------|-------|
| Workers R2 Storage | Account | Write |

## Prerequisites

To create temporary credentials, you need:

1. **R2 API Token**: A Cloudflare API token with R2 permissions
2. **R2 Access Key**: An R2 access key created in the Cloudflare dashboard
3. **Parent Access Key ID**: The ID of the R2 access key to derive credentials from

> [!NOTE]
> The `ParentAccessKeyId` is your R2 access key ID (not your Cloudflare API token). You can create R2 access keys in the Cloudflare dashboard under R2 > Manage R2 API Tokens.

## Security Best Practices

1. **Minimum TTL**: Use the shortest TTL that meets your requirements
2. **Least privilege**: Use the most restrictive permission level needed
3. **Scope narrowly**: Use prefixes or objects to limit access scope
4. **Don't store**: Generate credentials on-demand; don't store them
5. **Rotate parent keys**: Regularly rotate your parent R2 access keys

## Use Cases

| Scenario | Permission | Scope |
|----------|------------|-------|
| Browser upload | `ObjectWriteOnly` | User prefix |
| File download | `ObjectReadOnly` | Specific objects |
| Backup service | `ObjectReadWrite` | Backup prefix |
| Analytics | `AdminReadOnly` | Full bucket |
| Migration tool | `AdminReadWrite` | Full bucket |

## Related

- [Bucket Management](buckets.md) - Create and manage buckets
- [Presigned URLs](../../r2/presigned-urls.md) - Alternative for single-object access
- [R2 Uploads](../../r2/uploads.md) - Upload objects to R2

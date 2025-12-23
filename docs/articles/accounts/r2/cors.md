# R2 CORS Configuration

Configure Cross-Origin Resource Sharing (CORS) policies for R2 buckets to control which origins can access your bucket content from web browsers.

## Overview

```csharp
public class CorsService(ICloudflareApiClient cf)
{
    public async Task EnableCorsAsync(string bucket)
    {
        await cf.Accounts.Buckets.SetCorsAsync(bucket, new BucketCorsPolicy([
            new CorsRule(
                Allowed: new CorsAllowed(
                    Methods: ["GET", "HEAD"],
                    Origins: ["https://example.com"]
                )
            )
        ]));
    }
}
```

> [!NOTE]
> **Jurisdictional Buckets:** If your bucket was created with a jurisdiction (e.g., `R2Jurisdiction.EuropeanUnion`), you must pass the jurisdiction parameter to all CORS operations. See [Working with Jurisdictional Buckets](buckets.md#working-with-jurisdictional-buckets).
>
> ```csharp
> await cf.Accounts.Buckets.SetCorsAsync("my-eu-bucket", corsPolicy, R2Jurisdiction.EuropeanUnion);
> ```

## Setting CORS Policy

### Basic CORS for Web Access

```csharp
await cf.Accounts.Buckets.SetCorsAsync("my-bucket", new BucketCorsPolicy([
    new CorsRule(
        Allowed: new CorsAllowed(
            Methods: ["GET", "HEAD"],
            Origins: ["https://example.com", "https://www.example.com"]
        )
    )
]));
```

### Allow All Origins

```csharp
await cf.Accounts.Buckets.SetCorsAsync("my-bucket", new BucketCorsPolicy([
    new CorsRule(
        Allowed: new CorsAllowed(
            Methods: ["GET", "HEAD"],
            Origins: ["*"]
        )
    )
]));
```

### Full CRUD Access

```csharp
await cf.Accounts.Buckets.SetCorsAsync("my-bucket", new BucketCorsPolicy([
    new CorsRule(
        Allowed: new CorsAllowed(
            Methods: ["GET", "HEAD", "PUT", "POST", "DELETE"],
            Origins: ["https://app.example.com"],
            Headers: ["Content-Type", "Authorization", "X-Custom-Header"]
        ),
        ExposeHeaders: ["ETag", "Content-Length"],
        MaxAgeSeconds: 3600
    )
]));
```

### Multiple Rules

```csharp
await cf.Accounts.Buckets.SetCorsAsync("my-bucket", new BucketCorsPolicy([
    // Read-only for public site
    new CorsRule(
        Id: "public-read",
        Allowed: new CorsAllowed(
            Methods: ["GET", "HEAD"],
            Origins: ["https://www.example.com"]
        ),
        MaxAgeSeconds: 86400
    ),
    // Full access for admin
    new CorsRule(
        Id: "admin-full",
        Allowed: new CorsAllowed(
            Methods: ["GET", "HEAD", "PUT", "POST", "DELETE"],
            Origins: ["https://admin.example.com"],
            Headers: ["*"]
        )
    )
]));
```

## Getting CORS Policy

```csharp
var policy = await cf.Accounts.Buckets.GetCorsAsync("my-bucket");

foreach (var rule in policy.Rules)
{
    Console.WriteLine($"Rule: {rule.Id ?? "unnamed"}");
    Console.WriteLine($"  Origins: {string.Join(", ", rule.Allowed.Origins)}");
    Console.WriteLine($"  Methods: {string.Join(", ", rule.Allowed.Methods)}");
}
```

## Deleting CORS Policy

```csharp
await cf.Accounts.Buckets.DeleteCorsAsync("my-bucket");
```

## Models Reference

### BucketCorsPolicy

| Property | Type | Description |
|----------|------|-------------|
| `Rules` | `IReadOnlyList<CorsRule>` | List of CORS rules |

### CorsRule

| Property | Type | Description |
|----------|------|-------------|
| `Allowed` | `CorsAllowed` | Allowed origins, methods, headers |
| `Id` | `string?` | Optional rule identifier |
| `ExposeHeaders` | `IReadOnlyList<string>?` | Headers accessible to browser |
| `MaxAgeSeconds` | `int?` | Preflight cache duration |

### CorsAllowed

| Property | Type | Description |
|----------|------|-------------|
| `Methods` | `IReadOnlyList<string>` | HTTP methods (`GET`, `PUT`, etc.) |
| `Origins` | `IReadOnlyList<string>` | Allowed origins (`*` for all) |
| `Headers` | `IReadOnlyList<string>?` | Allowed request headers |

## HTTP Methods

| Method | Description |
|--------|-------------|
| `GET` | Download objects |
| `HEAD` | Get object metadata |
| `PUT` | Upload objects |
| `POST` | Multipart uploads |
| `DELETE` | Delete objects |

## Common Patterns

### CDN Configuration

```csharp
public async Task ConfigureCdnCorsAsync(string bucket, string cdnDomain)
{
    await cf.Accounts.Buckets.SetCorsAsync(bucket, new BucketCorsPolicy([
        new CorsRule(
            Allowed: new CorsAllowed(
                Methods: ["GET", "HEAD"],
                Origins: [$"https://{cdnDomain}"]
            ),
            ExposeHeaders: ["Content-Length", "Content-Type", "ETag"],
            MaxAgeSeconds: 86400  // 24 hours
        )
    ]));
}
```

### Development and Production

```csharp
public async Task SetupCorsAsync(string bucket, bool isDevelopment)
{
    var origins = isDevelopment
        ? new[] { "http://localhost:3000", "http://localhost:5173" }
        : new[] { "https://app.example.com" };

    await cf.Accounts.Buckets.SetCorsAsync(bucket, new BucketCorsPolicy([
        new CorsRule(
            Allowed: new CorsAllowed(
                Methods: ["GET", "HEAD", "PUT", "DELETE"],
                Origins: origins,
                Headers: ["Content-Type"]
            )
        )
    ]));
}
```

### Direct Upload from Browser

```csharp
// Enable CORS for presigned URL uploads
await cf.Accounts.Buckets.SetCorsAsync("uploads", new BucketCorsPolicy([
    new CorsRule(
        Allowed: new CorsAllowed(
            Methods: ["PUT"],
            Origins: ["https://app.example.com"],
            Headers: ["Content-Type", "Content-MD5"]
        ),
        ExposeHeaders: ["ETag"],
        MaxAgeSeconds: 3600
    )
]));
```

## Required Permissions

| Permission | Scope | Level |
|------------|-------|-------|
| Workers R2 Storage | Account | Write |

## Security Considerations

1. **Avoid `*` origins in production** - Use specific domains
2. **Limit methods** - Only allow methods you need
3. **Set MaxAgeSeconds** - Cache preflight to reduce requests
4. **Review headers** - Only expose necessary headers

## Related

- [Bucket Management](buckets.md) - Create and manage buckets
- [Custom Domains](custom-domains.md) - Attach custom hostnames
- [R2 Uploads](../../r2/uploads.md) - Upload objects

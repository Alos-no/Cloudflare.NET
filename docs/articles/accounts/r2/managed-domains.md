# R2 Managed Domains (r2.dev)

Control public access to R2 buckets via the managed r2.dev domain. This allows you to enable or disable public access to bucket contents without configuring custom domains.

## Overview

```csharp
public class ManagedDomainService(ICloudflareApiClient cf)
{
    public async Task EnablePublicAccessAsync(string bucket)
    {
        var result = await cf.Accounts.Buckets.EnableManagedDomainAsync(bucket);
        Console.WriteLine($"Public URL: {result.Domain}");
    }
}
```

> [!NOTE]
> **Jurisdictional Buckets:** If your bucket was created with a jurisdiction (e.g., `R2Jurisdiction.EuropeanUnion`), you must pass the jurisdiction parameter to all managed domain operations. See [Working with Jurisdictional Buckets](buckets.md#working-with-jurisdictional-buckets).
>
> ```csharp
> await cf.Accounts.Buckets.EnableManagedDomainAsync("my-eu-bucket", R2Jurisdiction.EuropeanUnion);
> ```

## Getting Managed Domain Status

Check if the r2.dev public URL is enabled for a bucket:

```csharp
var status = await cf.Accounts.Buckets.GetManagedDomainAsync("my-bucket");

Console.WriteLine($"Bucket ID: {status.BucketId}");
Console.WriteLine($"Domain: {status.Domain}");
Console.WriteLine($"Enabled: {status.Enabled}");
```

The domain format is: `{bucket-name}.{account-id}.r2.dev`

## Enabling Public Access

Enable the r2.dev subdomain to allow public access to bucket contents:

```csharp
var result = await cf.Accounts.Buckets.EnableManagedDomainAsync("my-bucket");

Console.WriteLine($"Public URL: https://{result.Domain}");
// Output: https://my-bucket.abc123.r2.dev
```

> [!WARNING]
> Enabling the managed domain makes all objects in the bucket publicly accessible. Ensure you have appropriate access controls or use custom domains with authentication for sensitive content.

## Disabling Public Access

Disable the r2.dev subdomain to prevent public access:

```csharp
await cf.Accounts.Buckets.DisableManagedDomainAsync("my-bucket");
```

After disabling, objects are only accessible via:
- The S3-compatible API with authentication
- Custom domains (if configured)

## Models Reference

### ManagedDomainResponse

| Property | Type | Description |
|----------|------|-------------|
| `BucketId` | `string?` | The unique identifier of the bucket |
| `Domain` | `string?` | The r2.dev domain hostname |
| `Enabled` | `bool` | Whether public access is enabled |

## Common Patterns

### Toggle Public Access

```csharp
public async Task SetPublicAccessAsync(string bucket, bool enabled)
{
    if (enabled)
    {
        var result = await cf.Accounts.Buckets.EnableManagedDomainAsync(bucket);
        Console.WriteLine($"Public URL enabled: https://{result.Domain}");
    }
    else
    {
        await cf.Accounts.Buckets.DisableManagedDomainAsync(bucket);
        Console.WriteLine("Public access disabled");
    }
}
```

### Check Before Enabling

```csharp
public async Task<string?> EnsurePublicAccessAsync(string bucket)
{
    var status = await cf.Accounts.Buckets.GetManagedDomainAsync(bucket);

    if (status.Enabled)
    {
        return status.Domain;
    }

    var result = await cf.Accounts.Buckets.EnableManagedDomainAsync(bucket);
    return result.Domain;
}
```

### Development vs Production

```csharp
public async Task ConfigureAccessAsync(string bucket, bool isDevelopment)
{
    if (isDevelopment)
    {
        // Enable r2.dev for easy development access
        await cf.Accounts.Buckets.EnableManagedDomainAsync(bucket);
    }
    else
    {
        // Production: disable r2.dev, use custom domains only
        await cf.Accounts.Buckets.DisableManagedDomainAsync(bucket);
    }
}
```

## Required Permissions

| Permission | Scope | Level |
|------------|-------|-------|
| Workers R2 Storage | Account | Write |

## Security Considerations

1. **Disable in production** - Use custom domains with proper access controls for production workloads
2. **No authentication** - The r2.dev domain provides no authentication; all objects are publicly accessible
3. **Rate limiting** - r2.dev URLs are subject to Cloudflare's standard rate limiting
4. **Caching** - Content served via r2.dev benefits from Cloudflare's edge caching

## Related

- [Custom Domains](custom-domains.md) - Attach custom hostnames for branded URLs
- [Bucket Management](buckets.md) - Create and manage buckets
- [CORS Configuration](cors.md) - Configure cross-origin access

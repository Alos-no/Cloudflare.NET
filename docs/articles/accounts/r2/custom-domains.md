# R2 Custom Domains

Attach custom hostnames to R2 buckets to serve content from your own domain instead of the default `r2.dev` URL.

## Overview

```csharp
public class DomainService(ICloudflareApiClient cf)
{
    public async Task AttachDomainAsync(string bucket, string hostname, string zoneId)
    {
        var result = await cf.Accounts.AttachCustomDomainAsync(bucket, hostname, zoneId);
        Console.WriteLine($"Status: {result.Status}");
    }
}
```

## Attaching a Custom Domain

```csharp
var result = await cf.Accounts.AttachCustomDomainAsync(
    bucketName: "my-bucket",
    hostname: "cdn.example.com",
    zoneId: "your-zone-id"
);

Console.WriteLine($"Domain: {result.Domain}");
Console.WriteLine($"Status: {result.Status}");
Console.WriteLine($"Edge Hostname: {result.EdgeHostname}");
```

### Prerequisites

1. The domain must be in a zone you control
2. The zone must be active in Cloudflare
3. You need the zone ID

## Checking Domain Status

```csharp
var status = await cf.Accounts.GetCustomDomainStatusAsync(
    bucketName: "my-bucket",
    hostname: "cdn.example.com"
);

Console.WriteLine($"Status: {status.Status}");
```

### Status Values

| Status | Description |
|--------|-------------|
| `pending` | Domain attachment in progress |
| `active` | Domain is active and serving traffic |
| `failed` | Attachment failed (check DNS/zone config) |

## Detaching a Custom Domain

```csharp
await cf.Accounts.DetachCustomDomainAsync(
    bucketName: "my-bucket",
    hostname: "cdn.example.com"
);
```

## Disabling Dev URL

Disable the default `r2.dev` public URL:

```csharp
await cf.Accounts.DisableDevUrlAsync("my-bucket");
```

> [!NOTE]
> After disabling the dev URL, content is only accessible via custom domains.

## Models Reference

### CustomDomainResponse

| Property | Type | Description |
|----------|------|-------------|
| `Domain` | `string` | The custom domain hostname |
| `Status` | `string` | Current status (`pending`, `active`, `failed`) |
| `EdgeHostname` | `string?` | Cloudflare edge hostname for CNAME |

## Common Patterns

### Attach Domain with Status Check

```csharp
public async Task<bool> AttachAndVerifyAsync(
    string bucket,
    string hostname,
    string zoneId,
    int maxAttempts = 10)
{
    await cf.Accounts.AttachCustomDomainAsync(bucket, hostname, zoneId);

    for (int i = 0; i < maxAttempts; i++)
    {
        await Task.Delay(TimeSpan.FromSeconds(5));

        var status = await cf.Accounts.GetCustomDomainStatusAsync(bucket, hostname);

        if (status.Status == "active")
            return true;

        if (status.Status == "failed")
            return false;
    }

    return false;
}
```

### Setup Complete CDN Configuration

```csharp
public async Task SetupCdnAsync(string bucket, string hostname, string zoneId)
{
    // 1. Attach custom domain
    await cf.Accounts.AttachCustomDomainAsync(bucket, hostname, zoneId);

    // 2. Disable dev URL for security
    await cf.Accounts.DisableDevUrlAsync(bucket);

    // 3. Configure CORS for web access
    await cf.Accounts.SetBucketCorsAsync(bucket, new BucketCorsPolicy([
        new CorsRule(
            Allowed: new CorsAllowed(
                Methods: ["GET", "HEAD"],
                Origins: [$"https://{hostname}"]
            ),
            MaxAgeSeconds: 86400
        )
    ]));
}
```

## Required Permissions

| Permission | Scope | Level |
|------------|-------|-------|
| Workers R2 Storage | Account | Write |

## DNS Configuration

After attaching a custom domain, configure your DNS:

1. Add a CNAME record pointing to the edge hostname
2. Or use Cloudflare DNS (recommended) for automatic setup

```
cdn.example.com CNAME <edge-hostname>
```

## Related

- [Bucket Management](buckets.md) - Create and manage buckets
- [CORS Configuration](cors.md) - Configure cross-origin access

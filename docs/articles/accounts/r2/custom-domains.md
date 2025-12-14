# R2 Custom Domains

Attach custom hostnames to R2 buckets to serve content from your own domain instead of the default `r2.dev` URL.

## Overview

```csharp
public class DomainService(ICloudflareApiClient cf)
{
    public async Task AttachDomainAsync(string bucket, string hostname, string zoneId)
    {
        var result = await cf.Accounts.Buckets.AttachCustomDomainAsync(bucket, hostname, zoneId);
        Console.WriteLine($"Status: {result.Status}");
    }
}
```

## Listing Custom Domains

List all custom domains attached to a bucket:

```csharp
var domains = await cf.Accounts.Buckets.ListCustomDomainsAsync("my-bucket");

foreach (var domain in domains)
{
    Console.WriteLine($"Domain: {domain.Domain}");
    Console.WriteLine($"  Enabled: {domain.Enabled}");
    Console.WriteLine($"  Status: {domain.Status?.Ownership ?? "pending"}");
    Console.WriteLine($"  Zone: {domain.ZoneName}");
}
```

## Attaching a Custom Domain

```csharp
var result = await cf.Accounts.Buckets.AttachCustomDomainAsync(
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
var status = await cf.Accounts.Buckets.GetCustomDomainStatusAsync(
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

## Updating Custom Domain Configuration

Update settings for an existing custom domain, such as enabling/disabling it or changing the minimum TLS version:

```csharp
// Disable a custom domain
var result = await cf.Accounts.Buckets.UpdateCustomDomainAsync(
    bucketName: "my-bucket",
    hostname: "cdn.example.com",
    new UpdateCustomDomainRequest(Enabled: false)
);

// Set minimum TLS version
var result = await cf.Accounts.Buckets.UpdateCustomDomainAsync(
    bucketName: "my-bucket",
    hostname: "cdn.example.com",
    new UpdateCustomDomainRequest(MinTls: "1.3")
);

// Enable with minimum TLS 1.2
var result = await cf.Accounts.Buckets.UpdateCustomDomainAsync(
    bucketName: "my-bucket",
    hostname: "cdn.example.com",
    new UpdateCustomDomainRequest(Enabled: true, MinTls: "1.2")
);
```

## Detaching a Custom Domain

```csharp
await cf.Accounts.Buckets.DetachCustomDomainAsync(
    bucketName: "my-bucket",
    hostname: "cdn.example.com"
);
```

> [!TIP]
> To disable the r2.dev public URL for enhanced security, see [Managed Domains (r2.dev)](managed-domains.md).

## Models Reference

### CustomDomain

Returned when listing custom domains for a bucket:

| Property | Type | Description |
|----------|------|-------------|
| `Domain` | `string` | The custom domain hostname |
| `Enabled` | `bool` | Whether the domain is enabled |
| `Status` | `CustomDomainStatusObject?` | Ownership and SSL status |
| `MinTls` | `string?` | Minimum TLS version (e.g., "1.2", "1.3") |
| `ZoneId` | `string?` | The Zone ID the domain belongs to |
| `ZoneName` | `string?` | The Zone name the domain belongs to |

### CustomDomainResponse

| Property | Type | Description |
|----------|------|-------------|
| `Domain` | `string` | The custom domain hostname |
| `Status` | `string` | Current status (`pending`, `active`, `failed`) |
| `EdgeHostname` | `string?` | Cloudflare edge hostname for CNAME |

### UpdateCustomDomainRequest

| Property | Type | Description |
|----------|------|-------------|
| `Enabled` | `bool?` | Whether the domain should be enabled |
| `MinTls` | `string?` | Minimum TLS version (e.g., "1.2", "1.3") |

## Common Patterns

### Attach Domain with Status Check

```csharp
public async Task<bool> AttachAndVerifyAsync(
    string bucket,
    string hostname,
    string zoneId,
    int maxAttempts = 10)
{
    await cf.Accounts.Buckets.AttachCustomDomainAsync(bucket, hostname, zoneId);

    for (int i = 0; i < maxAttempts; i++)
    {
        await Task.Delay(TimeSpan.FromSeconds(5));

        var status = await cf.Accounts.Buckets.GetCustomDomainStatusAsync(bucket, hostname);

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
    await cf.Accounts.Buckets.AttachCustomDomainAsync(bucket, hostname, zoneId);

    // 2. Disable dev URL for security
    await cf.Accounts.Buckets.DisableManagedDomainAsync(bucket);

    // 3. Configure CORS for web access
    await cf.Accounts.Buckets.SetCorsAsync(bucket, new BucketCorsPolicy([
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
- [Managed Domains (r2.dev)](managed-domains.md) - Enable/disable public access
- [CORS Configuration](cors.md) - Configure cross-origin access

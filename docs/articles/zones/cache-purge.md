# Cache Purge

Purge cached content from Cloudflare's edge servers for your zone. You can purge everything, specific files, URL prefixes, or entire hostnames.

## Overview

```csharp
public class CacheService(ICloudflareApiClient cf)
{
    public async Task PurgeAsync(string zoneId)
    {
        var result = await cf.Zones.PurgeCacheAsync(zoneId, new PurgeCacheRequest
        {
            Files = ["https://example.com/styles.css"]
        });

        Console.WriteLine($"Purge completed for zone: {result.Id}");
    }
}
```

## Purge Methods

### Purge Everything

Purge all cached content for the entire zone:

```csharp
await cf.Zones.PurgeCacheAsync(zoneId, new PurgeCacheRequest
{
    PurgeEverything = true
});
```

> [!WARNING]
> Purging everything can significantly increase origin load as all assets need to be re-fetched. Use with caution in production.

### Purge Specific Files

Purge specific URLs from the cache:

```csharp
await cf.Zones.PurgeCacheAsync(zoneId, new PurgeCacheRequest
{
    Files =
    [
        "https://example.com/styles.css",
        "https://example.com/script.js",
        "https://example.com/images/logo.png"
    ]
});
```

> [!NOTE]
> URLs must be fully qualified including the protocol (https://). You can purge up to 30 files per request.

### Purge by Prefix

Purge all URLs matching a prefix (Enterprise plans only):

```csharp
await cf.Zones.PurgeCacheAsync(zoneId, new PurgeCacheRequest
{
    Prefixes =
    [
        "https://example.com/images/",
        "https://example.com/static/v2/"
    ]
});
```

### Purge by Hostname

Purge all cached content for specific hostnames:

```csharp
await cf.Zones.PurgeCacheAsync(zoneId, new PurgeCacheRequest
{
    Hosts =
    [
        "cdn.example.com",
        "assets.example.com"
    ]
});
```

## Models Reference

### PurgeCacheRequest

Request payload for cache purge operations. At least one property must be set.

| Property | Type | Description |
|----------|------|-------------|
| `PurgeEverything` | `bool?` | If `true`, purges all assets in the cache |
| `Files` | `IReadOnlyList<string>?` | List of specific URLs to purge (max 30) |
| `Prefixes` | `IReadOnlyList<string>?` | List of URL prefixes to purge (Enterprise only) |
| `Hosts` | `IReadOnlyList<string>?` | List of hostnames to purge |

### PurgeCacheResult

Result of a successful cache purge operation.

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Zone identifier where the purge was performed |

## Common Patterns

### Purge After Deployment

Purge static assets after a deployment:

```csharp
public class DeploymentService(ICloudflareApiClient cf)
{
    public async Task PurgeAfterDeployAsync(string zoneId, string version)
    {
        await cf.Zones.PurgeCacheAsync(zoneId, new PurgeCacheRequest
        {
            Files =
            [
                $"https://example.com/static/{version}/app.js",
                $"https://example.com/static/{version}/styles.css"
            ]
        });
    }
}
```

### Purge with Retry on Rate Limit

Handle rate limiting when purging many files:

```csharp
public async Task PurgeBatchAsync(string zoneId, IEnumerable<string> urls)
{
    // Cloudflare allows 30 files per request
    var batches = urls.Chunk(30);

    foreach (var batch in batches)
    {
        await cf.Zones.PurgeCacheAsync(zoneId, new PurgeCacheRequest
        {
            Files = batch.ToList()
        });

        // Small delay to avoid rate limiting
        await Task.Delay(100);
    }
}
```

### Invalidate Content After Update

Purge content when database records change:

```csharp
public async Task InvalidateProductCacheAsync(string zoneId, int productId)
{
    await cf.Zones.PurgeCacheAsync(zoneId, new PurgeCacheRequest
    {
        Files =
        [
            $"https://example.com/api/products/{productId}",
            $"https://example.com/products/{productId}"
        ]
    });
}
```

## Required Permissions

| Permission | Scope | Level |
|------------|-------|-------|
| Cache Purge | Zone | Purge |

## Rate Limits

- **Files**: Maximum 30 URLs per request
- **Rate**: 1,000 purge requests per 10 seconds per zone
- **Everything**: Limited to 1 per minute per zone (recommended to avoid)

## Best Practices

1. **Prefer targeted purges** over `PurgeEverything` to minimize origin load
2. **Use cache tags** (via Cache-Tag header) for more granular invalidation
3. **Batch file purges** to stay within the 30 file limit
4. **Add delays** between batch requests to avoid rate limiting
5. **Monitor origin load** after large purge operations

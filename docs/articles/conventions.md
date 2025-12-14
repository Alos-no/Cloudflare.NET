# SDK Conventions

This guide covers common patterns and conventions used throughout the Cloudflare.NET SDK.

## Pagination

The SDK supports both pagination patterns used by the Cloudflare API. All paginated methods provide two approaches:

1. **Automatic pagination** via `IAsyncEnumerable<T>` - handles paging automatically
2. **Manual pagination** for fine-grained control over page requests

### Page-based Pagination

Page-based pagination uses `page` and `per_page` parameters. This pattern is used by:

- DNS Records
- Access Rules (Zone and Account)
- Zone Lockdown Rules
- User-Agent Rules

#### Automatic Pagination

```csharp
// Iterate over all records automatically
await foreach (var record in cf.Zones.ListAllDnsRecordsAsync(zoneId))
{
    Console.WriteLine($"{record.Name}: {record.Content}");
}

// With filters
var filters = new ListDnsRecordsFilters { Type = DnsRecordType.A };
await foreach (var record in cf.Zones.ListAllDnsRecordsAsync(zoneId, filters))
{
    // Process A records only
}
```

#### Manual Pagination

```csharp
// Get a specific page
var page = await cf.Zones.ListDnsRecordsAsync(zoneId, new ListDnsRecordsFilters
{
    Page = 1,
    PerPage = 50
});

// Access pagination info
Console.WriteLine($"Page {page.ResultInfo.Page} of {page.ResultInfo.TotalPages}");
Console.WriteLine($"Total records: {page.ResultInfo.TotalCount}");

// Iterate manually
while (page.ResultInfo.Page < page.ResultInfo.TotalPages)
{
    foreach (var record in page.Items)
    {
        // Process record
    }

    // Get next page
    page = await cf.Zones.ListDnsRecordsAsync(zoneId, new ListDnsRecordsFilters
    {
        Page = page.ResultInfo.Page + 1,
        PerPage = 50
    });
}
```

### Cursor-based Pagination

Cursor-based pagination uses an opaque cursor string for continuation. This pattern is used by:

- R2 Buckets
- Rulesets (Zone and Account)

#### Automatic Pagination

```csharp
// Iterate over all buckets automatically
await foreach (var bucket in cf.Accounts.Buckets.ListAllAsync())
{
    Console.WriteLine($"Bucket: {bucket.Name}");
}
```

#### Manual Pagination

```csharp
// Get first page
var page = await cf.Accounts.Buckets.ListAsync(new ListR2BucketsFilters
{
    PerPage = 50
});

// Process items
foreach (var bucket in page.Items)
{
    Console.WriteLine($"Bucket: {bucket.Name}");
}

// Continue if more pages exist
while (!string.IsNullOrEmpty(page.CursorInfo?.Cursor))
{
    page = await cf.Accounts.Buckets.ListAsync(new ListR2BucketsFilters
    {
        PerPage = 50,
        Cursor = page.CursorInfo.Cursor
    });

    foreach (var bucket in page.Items)
    {
        // Process bucket
    }
}
```

## Async Enumerable Pattern

All `ListAll*Async` methods return `IAsyncEnumerable<T>`, enabling efficient streaming of results:

```csharp
// Count records without loading all into memory
var count = await cf.Zones.ListAllDnsRecordsAsync(zoneId).CountAsync();

// Take first N records
var firstTen = await cf.Zones.ListAllDnsRecordsAsync(zoneId).Take(10).ToListAsync();

// Filter with LINQ
var aRecords = await cf.Zones.ListAllDnsRecordsAsync(zoneId)
    .Where(r => r.Type == DnsRecordType.A)
    .ToListAsync();
```

> [!TIP]
> Use `System.Linq.Async` NuGet package for LINQ extension methods on `IAsyncEnumerable<T>`.

## Cancellation Tokens

All async methods accept an optional `CancellationToken`:

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

try
{
    var record = await cf.Zones.GetDnsRecordAsync(zoneId, recordId, cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Operation timed out");
}
```

## Filter Objects

List operations typically accept a filter object to narrow results:

```csharp
// DNS record filters
var dnsFilters = new ListDnsRecordsFilters
{
    Type = DnsRecordType.CNAME,
    Name = "www.example.com",
    Page = 1,
    PerPage = 100
};

// Access rule filters
var ruleFilters = new ListAccessRulesFilters
{
    Mode = AccessRuleMode.Block,
    Page = 1,
    PerPage = 50
};
```

## Result Types

### Single Results

Methods returning a single item typically return the item directly or `null` if not found:

```csharp
// Returns DnsRecord or null
var record = await cf.Zones.FindDnsRecordByNameAsync(zoneId, "www.example.com");

if (record is not null)
{
    Console.WriteLine($"Found: {record.Id}");
}
```

### Paginated Results

Paginated methods return wrapper types containing items and pagination info:

- <xref:Cloudflare.NET.Core.Models.PagePaginatedResult`1> - For page-based pagination
- <xref:Cloudflare.NET.Core.Models.CursorPaginatedResult`1> - For cursor-based pagination

## Error Handling

The SDK throws <xref:Cloudflare.NET.Core.Exceptions.CloudflareApiException> when the API returns `success: false`:

```csharp
try
{
    await cf.Zones.CreateCnameRecordAsync(zoneId, name, target);
}
catch (CloudflareApiException ex)
{
    // API returned an error response
    foreach (var error in ex.Errors)
    {
        Console.WriteLine($"[{error.Code}] {error.Message}");
    }
}
catch (HttpRequestException ex)
{
    // Network or HTTP-level error
    Console.WriteLine($"HTTP Error: {ex.StatusCode} - {ex.Message}");
}
```

## Extensible Enums

The SDK uses **extensible enums** for values that may be extended by Cloudflare in the future. This pattern provides:

- **Strong typing** with IntelliSense for known values
- **Forward compatibility** by accepting unknown string values
- **Graceful degradation** when new API values are returned

### Usage

Extensible enums can be used like regular enums with static properties:

```csharp
// Use predefined constants with IntelliSense
var location = R2LocationHint.EastNorthAmerica;
var jurisdiction = R2Jurisdiction.EuropeanUnion;
var storageClass = R2StorageClass.InfrequentAccess;
```

They also support implicit conversion from strings for custom or new values:

```csharp
// Future-proof: accept values not yet defined in SDK
R2LocationHint futureRegion = "new-region-2025";
R2Jurisdiction customJurisdiction = "custom-jurisdiction";
```

### Comparison

Extensible enums support equality comparison:

```csharp
if (bucket.Location == R2LocationHint.WestEurope)
{
    Console.WriteLine("Bucket is in Western Europe");
}

// Pattern matching works too
if (bucket.StorageClass is { } sc && sc == R2StorageClass.InfrequentAccess)
{
    Console.WriteLine("Using infrequent access storage");
}
```

### Available Extensible Enums

#### R2 Storage

| Type | Purpose | Known Values |
|------|---------|--------------|
| `R2LocationHint` | Bucket placement hint | `wnam`, `enam`, `weur`, `eeur`, `apac`, `oc` |
| `R2Jurisdiction` | Data residency | `default`, `eu`, `fedramp` |
| `R2StorageClass` | Storage tier | `Standard`, `InfrequentAccess` |

#### DNS & Zones

| Type | Purpose | Known Values |
|------|---------|--------------|
| `DnsRecordType` | DNS record type | `A`, `AAAA`, `CNAME`, `MX`, `TXT`, `NS`, `SOA`, `PTR`, `SRV`, `HTTPS`, `SVCB`, `CAA`, `DS`, `DNSKEY`, etc. |
| `ZoneStatus` | Zone activation status | `active`, `pending`, `initializing`, `moved`, `deleted`, `deactivated` |
| `ZoneSettingId` | Zone setting identifier | `ssl`, `min_tls_version`, `always_use_https`, `brotli`, `http2`, `http3`, `development_mode`, `security_level`, etc. |

#### Security & Firewall

| Type | Purpose | Known Values |
|------|---------|--------------|
| `AccessRuleMode` | IP access rule action | `block`, `challenge`, `js_challenge`, `managed_challenge`, `whitelist` |
| `AccessRuleTarget` | Access rule target type | `ip`, `ip_range`, `asn`, `country` |
| `UaRuleMode` | User-agent rule action | `block`, `challenge`, `js_challenge`, `managed_challenge` |
| `LockdownTarget` | Zone lockdown target type | `ip`, `ip_range` |
| `RulesetAction` | WAF ruleset action | `block`, `challenge`, `js_challenge`, `managed_challenge`, `log`, `skip`, `execute`, `rewrite`, `redirect`, `route`, `score`, `serve_error`, `compress_response`, `set_cache_settings`, `set_config` |
| `ManagedWafOverrideAction` | Managed WAF override | `block`, `challenge`, `js_challenge`, `managed_challenge`, `log`, `default` |

### Why Extensible Enums?

Traditional C# enums would fail to deserialize when Cloudflare adds new values:

```csharp
// Traditional enum - BREAKS if API returns "new-value"
public enum StorageClass { Standard, InfrequentAccess }

// Extensible enum - gracefully handles unknown values
R2StorageClass unknownClass = "new-storage-class-2025"; // Works!
```

This pattern is commonly used by cloud SDKs (Azure SDK, AWS SDK) to handle API evolution gracefully.

## Related

- [Getting Started](getting-started.md) - Quick start guide
- [Configuration](configuration.md) - SDK configuration options
- [API Coverage](api-coverage.md) - Supported API endpoints

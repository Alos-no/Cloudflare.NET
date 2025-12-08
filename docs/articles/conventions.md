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
await foreach (var bucket in cf.Accounts.ListAllR2BucketsAsync())
{
    Console.WriteLine($"Bucket: {bucket.Name}");
}
```

#### Manual Pagination

```csharp
// Get first page
var page = await cf.Accounts.ListR2BucketsAsync(new ListR2BucketsFilters
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
    page = await cf.Accounts.ListR2BucketsAsync(new ListR2BucketsFilters
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

## Related

- [Getting Started](getting-started.md) - Quick start guide
- [Configuration](configuration.md) - SDK configuration options
- [API Coverage](api-coverage.md) - Supported API endpoints

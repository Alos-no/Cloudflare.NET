# R2 Bucket Management

Manage R2 buckets through the Cloudflare REST API. This covers bucket creation, listing, and deletion. For object operations (upload, download, delete), see the [R2 Object Storage](../../r2/index.md) section.

## Overview

```csharp
public class BucketService(ICloudflareApiClient cf)
{
    public async Task<R2Bucket> CreateBucketAsync(string name)
    {
        return await cf.Accounts.CreateR2BucketAsync(name);
    }
}
```

## Creating Buckets

### Basic Creation

```csharp
var bucket = await cf.Accounts.CreateR2BucketAsync("my-new-bucket");

Console.WriteLine($"Name: {bucket.Name}");
Console.WriteLine($"Created: {bucket.CreationDate}");
Console.WriteLine($"Location: {bucket.Location}");
Console.WriteLine($"Storage Class: {bucket.StorageClass}");
```

### With Location Hint

Suggest a geographic region for bucket placement using [extensible enums](#r2locationhint-extensible-enum):

```csharp
// Create bucket in Western Europe
var bucket = await cf.Accounts.CreateR2BucketAsync(
    "eu-data-bucket",
    R2LocationHint.WestEurope
);
```

### With Jurisdiction (Data Residency)

Enforce data residency within a specific boundary:

```csharp
// Create EU-jurisdictional bucket for GDPR compliance
var bucket = await cf.Accounts.CreateR2BucketAsync(
    "gdpr-compliant-bucket",
    locationHint: R2LocationHint.WestEurope,
    jurisdiction: R2Jurisdiction.EuropeanUnion
);

// Access via jurisdictional S3 endpoint:
// https://{account_id}.eu.r2.cloudflarestorage.com
```

### With Storage Class

Set the default storage class for new objects:

```csharp
// Create bucket optimized for infrequent access
var bucket = await cf.Accounts.CreateR2BucketAsync(
    "archive-bucket",
    locationHint: R2LocationHint.EastNorthAmerica,
    storageClass: R2StorageClass.InfrequentAccess
);
```

### Bucket Naming Rules

- Must be 3-63 characters long
- Can contain lowercase letters, numbers, and hyphens
- Must start and end with a letter or number
- Must be unique within your account

## Listing Buckets

### List All Buckets

```csharp
await foreach (var bucket in cf.Accounts.ListAllR2BucketsAsync())
{
    Console.WriteLine($"{bucket.Name} - {bucket.Location ?? "default"}");
}
```

### List with Pagination

```csharp
var page = await cf.Accounts.ListR2BucketsAsync(new ListR2BucketsFilters
{
    PerPage = 50
});

foreach (var bucket in page.Items)
{
    Console.WriteLine(bucket.Name);
}

// Get next page using cursor
if (page.CursorInfo.Cursor is not null)
{
    var nextPage = await cf.Accounts.ListR2BucketsAsync(new ListR2BucketsFilters
    {
        Cursor = page.CursorInfo.Cursor
    });
}
```

## Deleting Buckets

```csharp
await cf.Accounts.DeleteR2BucketAsync("my-bucket");
```

> [!WARNING]
> The bucket must be empty before deletion. Use the R2 Client to clear the bucket first:
> ```csharp
> await r2.ClearBucketAsync("my-bucket");
> await cf.Accounts.DeleteR2BucketAsync("my-bucket");
> ```

## Disabling Dev URL

Disable the public `r2.dev` URL for a bucket:

```csharp
await cf.Accounts.DisableDevUrlAsync("my-bucket");
```

## Models Reference

### R2Bucket

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Bucket name |
| `CreationDate` | `DateTime` | When the bucket was created |
| `Location` | `R2LocationHint?` | Location hint for bucket placement |
| `Jurisdiction` | `R2Jurisdiction?` | Data residency jurisdiction |
| `StorageClass` | `R2StorageClass?` | Default storage class for new objects |

### R2LocationHint (Extensible Enum)

Location hints suggest where bucket data should be stored. These are **extensible enums** that provide IntelliSense for known values while allowing custom values for new regions.

| Constant | Value | Description |
|----------|-------|-------------|
| `WestNorthAmerica` | `wnam` | Western North America |
| `EastNorthAmerica` | `enam` | Eastern North America |
| `WestEurope` | `weur` | Western Europe |
| `EastEurope` | `eeur` | Eastern Europe |
| `AsiaPacific` | `apac` | Asia-Pacific region |
| `Oceania` | `oc` | Australia & New Zealand |

```csharp
// Using known values with IntelliSense
var location = R2LocationHint.EastNorthAmerica;

// Using custom values for new regions
R2LocationHint customRegion = "new-region-2025";

// Comparison
if (bucket.Location == R2LocationHint.WestEurope) { ... }
```

### R2Jurisdiction (Extensible Enum)

Jurisdictions guarantee data residency within specific geographic or regulatory boundaries.

| Constant | Value | Description |
|----------|-------|-------------|
| `Default` | `default` | No restriction (global) |
| `EuropeanUnion` | `eu` | EU data residency (GDPR compliance) |
| `FedRamp` | `fedramp` | US federal compliance (Enterprise only) |

```csharp
// EU jurisdiction for GDPR compliance
var jurisdiction = R2Jurisdiction.EuropeanUnion;

// S3 endpoint for jurisdictional buckets:
// https://{account_id}.eu.r2.cloudflarestorage.com
```

### R2StorageClass (Extensible Enum)

Storage classes determine cost and access characteristics.

| Constant | Value | Description |
|----------|-------|-------------|
| `Standard` | `Standard` | Frequently accessed data (default) |
| `InfrequentAccess` | `InfrequentAccess` | Lower cost, retrieval fees apply |

```csharp
// Using in lifecycle transitions
new StorageClassTransition(
    LifecycleCondition.AfterDays(30),
    R2StorageClass.InfrequentAccess
);
```

### ListR2BucketsFilters

| Property | Type | Description |
|----------|------|-------------|
| `PerPage` | `int?` | Number of buckets per page |
| `Cursor` | `string?` | Cursor for next page |

## Common Patterns

### Create Bucket with Validation

```csharp
public async Task<R2Bucket?> CreateBucketIfNotExistsAsync(string name)
{
    // Check if bucket exists
    await foreach (var bucket in cf.Accounts.ListAllR2BucketsAsync())
    {
        if (bucket.Name == name)
        {
            return bucket;
        }
    }

    // Create new bucket
    return await cf.Accounts.CreateR2BucketAsync(name);
}
```

### List Buckets by Location

```csharp
public async Task<List<R2Bucket>> GetBucketsByLocationAsync(string location)
{
    var buckets = new List<R2Bucket>();

    await foreach (var bucket in cf.Accounts.ListAllR2BucketsAsync())
    {
        if (bucket.Location == location)
        {
            buckets.Add(bucket);
        }
    }

    return buckets;
}
```

### Safe Bucket Deletion

```csharp
public async Task SafeDeleteBucketAsync(string name, IR2Client r2)
{
    // Clear all objects first
    await r2.ClearBucketAsync(name);

    // Then delete the bucket
    await cf.Accounts.DeleteR2BucketAsync(name);
}
```

## Required Permissions

| Permission | Scope | Level |
|------------|-------|-------|
| Workers R2 Storage | Account | Read (for listing) |
| Workers R2 Storage | Account | Write (for create/delete) |

## Related

- [Custom Domains](custom-domains.md) - Attach custom hostnames
- [CORS Configuration](cors.md) - Configure cross-origin access
- [Lifecycle Policies](lifecycle.md) - Automatic object management
- [R2 Object Storage](../../r2/index.md) - Upload, download, and manage objects

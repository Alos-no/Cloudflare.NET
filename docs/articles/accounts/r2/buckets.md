# R2 Bucket Management

Manage R2 buckets through the Cloudflare REST API. This covers bucket creation, listing, and deletion. For object operations (upload, download, delete), see the [R2 Object Storage](../../r2/index.md) section.

## Overview

```csharp
public class BucketService(ICloudflareApiClient cf)
{
    public async Task<R2Bucket> CreateBucketAsync(string name)
    {
        return await cf.Accounts.Buckets.CreateAsync(name);
    }
}
```

## Creating Buckets

### Basic Creation

```csharp
var bucket = await cf.Accounts.Buckets.CreateAsync("my-new-bucket");

Console.WriteLine($"Name: {bucket.Name}");
Console.WriteLine($"Created: {bucket.CreationDate}");
Console.WriteLine($"Location: {bucket.Location}");
Console.WriteLine($"Storage Class: {bucket.StorageClass}");
```

### With Location Hint

Suggest a geographic region for bucket placement using [extensible enums](#r2locationhint-extensible-enum):

```csharp
// Create bucket in Western Europe
var bucket = await cf.Accounts.Buckets.CreateAsync(
    "eu-data-bucket",
    R2LocationHint.WestEurope
);
```

### With Jurisdiction (Data Residency)

Enforce data residency within a specific boundary:

```csharp
// Create EU-jurisdictional bucket for GDPR compliance
var bucket = await cf.Accounts.Buckets.CreateAsync(
    "gdpr-compliant-bucket",
    locationHint: R2LocationHint.WestEurope,
    jurisdiction: R2Jurisdiction.EuropeanUnion
);

// Access via jurisdictional S3 endpoint:
// https://{account_id}.eu.r2.cloudflarestorage.com
```

> [!IMPORTANT]
> **If you create a bucket with a jurisdiction, you must pass the same jurisdiction value to ALL subsequent API operations on that bucket.** Without it, the API returns error 10006 "The specified bucket does not exist" even though the bucket exists. See [Working with Jurisdictional Buckets](#working-with-jurisdictional-buckets) for details.

### With Storage Class

Set the default storage class for new objects:

```csharp
// Create bucket optimized for infrequent access
var bucket = await cf.Accounts.Buckets.CreateAsync(
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

## Getting Bucket Details

Retrieve detailed information about a specific bucket:

```csharp
var bucket = await cf.Accounts.Buckets.GetAsync("my-bucket");

Console.WriteLine($"Name: {bucket.Name}");
Console.WriteLine($"Created: {bucket.CreationDate}");
Console.WriteLine($"Location: {bucket.Location}");
Console.WriteLine($"Storage Class: {bucket.StorageClass}");
```

### With Jurisdiction

For buckets created with jurisdictional restrictions, specify the jurisdiction:

```csharp
var bucket = await cf.Accounts.Buckets.GetAsync(
    "gdpr-compliant-bucket",
    R2Jurisdiction.EuropeanUnion
);
```

## Listing Buckets

### List All Buckets

```csharp
await foreach (var bucket in cf.Accounts.Buckets.ListAllAsync())
{
    Console.WriteLine($"{bucket.Name} - {bucket.Location ?? "default"}");
}
```

### List with Pagination

```csharp
var page = await cf.Accounts.Buckets.ListAsync(new ListR2BucketsFilters
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
    var nextPage = await cf.Accounts.Buckets.ListAsync(new ListR2BucketsFilters
    {
        Cursor = page.CursorInfo.Cursor
    });
}
```

### Filter by Jurisdiction

List only buckets in a specific jurisdiction:

```csharp
// List only EU jurisdiction buckets
await foreach (var bucket in cf.Accounts.Buckets.ListAllAsync(
    jurisdiction: R2Jurisdiction.EuropeanUnion))
{
    Console.WriteLine($"EU Bucket: {bucket.Name}");
}
```

### Filtering and Sorting

Use `ListR2BucketsFilters` to filter by name and control sort order:

```csharp
// Find buckets containing "backup" in their name, sorted descending
await foreach (var bucket in cf.Accounts.Buckets.ListAllAsync(
    new ListR2BucketsFilters
    {
        NameContains = "backup",
        Order = "name",
        Direction = "desc"
    }))
{
    Console.WriteLine(bucket.Name);
}

// Paginate starting after a specific bucket (lexicographic order)
var page = await cf.Accounts.Buckets.ListAsync(new ListR2BucketsFilters
{
    PerPage = 100,
    StartAfter = "my-bucket"
});
```

## Deleting Buckets

```csharp
await cf.Accounts.Buckets.DeleteAsync("my-bucket");
```

> [!WARNING]
> The bucket must be empty before deletion. Use the R2 Client to clear the bucket first:
> ```csharp
> await r2.ClearBucketAsync("my-bucket");
> await cf.Accounts.Buckets.DeleteAsync("my-bucket");
> ```

## Updating Buckets

Update the default storage class for new objects uploaded to a bucket:

```csharp
// Change storage class to Infrequent Access
var updated = await cf.Accounts.Buckets.UpdateAsync(
    "my-bucket",
    R2StorageClass.InfrequentAccess
);

Console.WriteLine($"Storage Class: {updated.StorageClass}");
```

### With Jurisdiction

For buckets created with jurisdictional restrictions, specify the jurisdiction:

```csharp
var updated = await cf.Accounts.Buckets.UpdateAsync(
    "gdpr-compliant-bucket",
    R2StorageClass.InfrequentAccess,
    R2Jurisdiction.EuropeanUnion
);
```

> [!NOTE]
> - Only the default storage class can be updated after bucket creation
> - Changing the storage class only affects **new** objects uploaded after the update
> - Existing objects retain their original storage class; use [lifecycle rules](lifecycle.md) to transition them
> - The bucket's location hint and jurisdiction cannot be changed after creation

### Common Use Cases

**Migrate to cost-optimized storage:**

```csharp
// Transition archive buckets to Infrequent Access for lower storage costs
var archiveBuckets = new[] { "logs-2023", "backups-old", "cold-storage" };

foreach (var bucketName in archiveBuckets)
{
    await cf.Accounts.Buckets.UpdateAsync(bucketName, R2StorageClass.InfrequentAccess);
    Console.WriteLine($"Updated {bucketName} to Infrequent Access");
}
```

**Restore active bucket to Standard storage:**

```csharp
// When data needs frequent access again
await cf.Accounts.Buckets.UpdateAsync("reactivated-bucket", R2StorageClass.Standard);
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
| `PerPage` | `int?` | Number of buckets per page (1-1000) |
| `Cursor` | `string?` | Cursor for next page |
| `NameContains` | `string?` | Filter to buckets containing this phrase |
| `Order` | `string?` | Field to order by (currently only `"name"`) |
| `Direction` | `string?` | Sort direction: `"asc"` or `"desc"` |
| `StartAfter` | `string?` | Start listing after this bucket name (lexicographic) |

## Working with Jurisdictional Buckets

Buckets created with a jurisdiction restriction (e.g., `R2Jurisdiction.EuropeanUnion` or `R2Jurisdiction.FedRamp`) have special requirements for API access.

### When Jurisdiction is Required

| Operation | Jurisdiction Required? |
|-----------|----------------------|
| `CreateAsync` | Pass jurisdiction to create a jurisdictional bucket |
| `GetAsync` | **Yes** - Required for jurisdictional buckets |
| `UpdateAsync` | **Yes** - Required for jurisdictional buckets |
| `DeleteAsync` | **Yes** - Required for jurisdictional buckets |
| `ListAsync` / `ListAllAsync` | Optional filter - pass to list only buckets in that jurisdiction |
| All CORS operations | **Yes** - Required for jurisdictional buckets |
| All Lifecycle operations | **Yes** - Required for jurisdictional buckets |
| All Custom Domain operations | **Yes** - Required for jurisdictional buckets |
| All Managed Domain operations | **Yes** - Required for jurisdictional buckets |
| All Lock operations | **Yes** - Required for jurisdictional buckets |
| All Sippy operations | **Yes** - Required for jurisdictional buckets |
| `CreateTempCredentialsAsync` | No - Bucket is specified in request body |

### Best Practice: Store and Reuse Jurisdiction

```csharp
public class JurisdictionalBucketService(ICloudflareApiClient cf)
{
    // Store the jurisdiction when creating the bucket
    public async Task<(R2Bucket Bucket, R2Jurisdiction? Jurisdiction)> CreateBucketAsync(
        string name,
        R2Jurisdiction? jurisdiction = null)
    {
        var bucket = await cf.Accounts.Buckets.CreateAsync(
            name,
            jurisdiction: jurisdiction
        );

        // Return both the bucket and the jurisdiction for later use
        return (bucket, jurisdiction);
    }

    // Always pass jurisdiction for subsequent operations
    public async Task ConfigureBucketAsync(
        string bucketName,
        R2Jurisdiction? jurisdiction,
        BucketCorsPolicy corsPolicy)
    {
        // All these operations need the jurisdiction parameter
        await cf.Accounts.Buckets.SetCorsAsync(bucketName, corsPolicy, jurisdiction);
        await cf.Accounts.Buckets.EnableManagedDomainAsync(bucketName, jurisdiction);
    }

    public async Task DeleteBucketAsync(string bucketName, R2Jurisdiction? jurisdiction)
    {
        await cf.Accounts.Buckets.DeleteAsync(bucketName, jurisdiction);
    }
}
```

### Discovering Jurisdiction from Existing Buckets

If you don't know a bucket's jurisdiction, retrieve it from the `ListAsync` response:

```csharp
// ListAsync returns all buckets with their jurisdiction
await foreach (var bucket in cf.Accounts.Buckets.ListAllAsync())
{
    Console.WriteLine($"{bucket.Name}: {bucket.Jurisdiction?.Value ?? "default"}");

    // Now you can use the jurisdiction for subsequent operations
    if (bucket.Jurisdiction is not null)
    {
        var details = await cf.Accounts.Buckets.GetAsync(
            bucket.Name,
            bucket.Jurisdiction
        );
    }
}
```

### Filtering Buckets by Jurisdiction

To list only buckets in a specific jurisdiction, pass the optional `jurisdiction` parameter:

```csharp
// List only EU jurisdiction buckets
await foreach (var bucket in cf.Accounts.Buckets.ListAllAsync(
    jurisdiction: R2Jurisdiction.EuropeanUnion))
{
    Console.WriteLine($"EU Bucket: {bucket.Name}");
}

// List only FedRAMP jurisdiction buckets
var fedRampBuckets = await cf.Accounts.Buckets.ListAsync(
    jurisdiction: R2Jurisdiction.FedRamp);
```

### Common Error: Missing Jurisdiction

If you forget to pass jurisdiction for a jurisdictional bucket, you'll receive:

```
Error 10006: The specified bucket does not exist
```

This error is misleading - the bucket exists, but the API can't find it without the jurisdiction header. Solution: pass the jurisdiction parameter:

```csharp
// This fails for EU jurisdiction buckets:
var bucket = await cf.Accounts.Buckets.GetAsync("my-eu-bucket");  // Error 10006!

// This works:
var bucket = await cf.Accounts.Buckets.GetAsync("my-eu-bucket", R2Jurisdiction.EuropeanUnion);
```

## Common Patterns

### Create Bucket with Validation

```csharp
public async Task<R2Bucket?> CreateBucketIfNotExistsAsync(string name)
{
    // Check if bucket exists using GetAsync
    try
    {
        return await cf.Accounts.Buckets.GetAsync(name);
    }
    catch (CloudflareApiException ex) when (ex.Errors.Any(e => e.Code == 10006))
    {
        // Bucket not found, create it
        return await cf.Accounts.Buckets.CreateAsync(name);
    }
}
```

### List Buckets by Location

```csharp
public async Task<List<R2Bucket>> GetBucketsByLocationAsync(string location)
{
    var buckets = new List<R2Bucket>();

    await foreach (var bucket in cf.Accounts.Buckets.ListAllAsync())
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
    await cf.Accounts.Buckets.DeleteAsync(name);
}
```

## Required Permissions

| Permission | Scope | Level |
|------------|-------|-------|
| Workers R2 Storage | Account | Read (for listing) |
| Workers R2 Storage | Account | Write (for create/delete) |

## Related

- [Custom Domains](custom-domains.md) - Attach custom hostnames
- [Managed Domains (r2.dev)](managed-domains.md) - Enable/disable public access
- [CORS Configuration](cors.md) - Configure cross-origin access
- [Lifecycle Policies](lifecycle.md) - Automatic object management
- [Bucket Locks](bucket-locks.md) - Configure object retention
- [Sippy Migration](sippy.md) - Incremental data migration
- [Temporary Credentials](temp-credentials.md) - Scoped, time-limited access
- [R2 Object Storage](../../r2/index.md) - Upload, download, and manage objects

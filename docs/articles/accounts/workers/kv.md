# Workers KV

Workers KV is Cloudflare's global, low-latency key-value data store. This API allows you to manage KV namespaces and their key-value pairs programmatically.

## Overview

```csharp
public class KvService(ICloudflareApiClient cf)
{
    public async Task StoreConfigAsync(string namespaceId, string key, string value)
    {
        await cf.Accounts.Kv.WriteValueAsync(namespaceId, key, value);
    }

    public async Task<string?> GetConfigAsync(string namespaceId, string key)
    {
        return await cf.Accounts.Kv.GetValueAsync(namespaceId, key);
    }
}
```

## API Limits

| Limit | Value |
|-------|-------|
| Key name max length | 512 bytes |
| Value max size | 25 MiB |
| Metadata max size | 1024 bytes (serialized JSON) |
| Minimum TTL | 60 seconds |
| Write rate limit | 1 per second per key |
| Bulk write max items | 10,000 pairs |
| Bulk write max size | 100 MB total |
| Bulk delete max keys | 10,000 keys |
| Bulk get max keys | 100 keys |

## Namespace Operations

### Creating a Namespace

```csharp
var ns = await cf.Accounts.Kv.CreateAsync("my-config-store");

Console.WriteLine($"Created namespace: {ns.Id}");
Console.WriteLine($"Title: {ns.Title}");
```

> [!NOTE]
> Namespace titles must be unique within your account. Creating a namespace with a duplicate title returns error code 10014.

### Listing Namespaces

```csharp
// List all namespaces automatically
await foreach (var ns in cf.Accounts.Kv.ListAllAsync())
{
    Console.WriteLine($"{ns.Title}: {ns.Id}");
}

// With ordering
var filters = new ListKvNamespacesFilters(
    Order: KvNamespaceOrderField.Title,
    Direction: ListOrderDirection.Ascending
);

await foreach (var ns in cf.Accounts.Kv.ListAllAsync(filters))
{
    Console.WriteLine(ns.Title);
}
```

#### Manual Pagination

```csharp
var page = await cf.Accounts.Kv.ListAsync(new ListKvNamespacesFilters(
    Page: 1,
    PerPage: 50
));

Console.WriteLine($"Page {page.PageInfo.Page} of {page.PageInfo.TotalPages}");

foreach (var ns in page.Items)
{
    Console.WriteLine(ns.Title);
}
```

### Getting a Namespace

```csharp
var ns = await cf.Accounts.Kv.GetAsync(namespaceId);

Console.WriteLine($"Title: {ns.Title}");
Console.WriteLine($"Supports URL Encoding: {ns.SupportsUrlEncoding}");
```

### Renaming a Namespace

```csharp
var updated = await cf.Accounts.Kv.RenameAsync(namespaceId, "new-title");

Console.WriteLine($"Renamed to: {updated.Title}");
```

### Deleting a Namespace

```csharp
await cf.Accounts.Kv.DeleteAsync(namespaceId);
```

> [!WARNING]
> Deleting a namespace permanently removes all keys and values within it. This action cannot be undone.

## Key-Value Operations

### Writing Values

#### String Values

```csharp
await cf.Accounts.Kv.WriteValueAsync(namespaceId, "config/app-name", "MyApp");
```

#### Binary Values

```csharp
byte[] imageData = await File.ReadAllBytesAsync("logo.png");
await cf.Accounts.Kv.WriteValueAsync(namespaceId, "assets/logo", imageData);
```

#### With Expiration

```csharp
// Expire at a specific Unix timestamp
await cf.Accounts.Kv.WriteValueAsync(
    namespaceId,
    "session/abc123",
    sessionData,
    new KvWriteOptions(Expiration: DateTimeOffset.UtcNow.AddHours(24).ToUnixTimeSeconds())
);

// Expire after a TTL (in seconds, minimum 60)
await cf.Accounts.Kv.WriteValueAsync(
    namespaceId,
    "cache/user-profile",
    profileJson,
    new KvWriteOptions(ExpirationTtl: 3600) // 1 hour
);
```

#### With Metadata

Attach arbitrary JSON metadata to any key:

```csharp
var metadata = JsonSerializer.SerializeToElement(new
{
    contentType = "application/json",
    version = 2,
    createdBy = "system"
});

await cf.Accounts.Kv.WriteValueAsync(
    namespaceId,
    "config/settings",
    settingsJson,
    new KvWriteOptions(Metadata: metadata)
);
```

### Reading Values

#### String Values

```csharp
string? value = await cf.Accounts.Kv.GetValueAsync(namespaceId, "config/app-name");

if (value is null)
{
    Console.WriteLine("Key not found");
}
else
{
    Console.WriteLine($"Value: {value}");
}
```

#### Binary Values

```csharp
byte[]? data = await cf.Accounts.Kv.GetValueBytesAsync(namespaceId, "assets/logo");

if (data is not null)
{
    await File.WriteAllBytesAsync("downloaded-logo.png", data);
}
```

#### With Expiration Info (String)

```csharp
var result = await cf.Accounts.Kv.GetValueWithExpirationAsync(namespaceId, "session/abc123");

if (result is not null)
{
    Console.WriteLine($"Value: {result.Value}");

    if (result.Expiration is not null)
    {
        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(result.Expiration.Value);
        Console.WriteLine($"Expires: {expiresAt}");
    }
}
```

#### With Expiration Info (Binary)

```csharp
var result = await cf.Accounts.Kv.GetValueBytesWithExpirationAsync(namespaceId, "assets/logo");

if (result is not null)
{
    await File.WriteAllBytesAsync("downloaded-logo.png", result.Value);

    if (result.Expiration is not null)
    {
        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(result.Expiration.Value);
        Console.WriteLine($"Expires: {expiresAt}");
    }
}
```

#### Metadata Only

Read metadata without fetching the value (useful for large values):

```csharp
var metadata = await cf.Accounts.Kv.GetMetadataAsync(namespaceId, "config/settings");

if (metadata is not null)
{
    var version = metadata.Value.GetProperty("version").GetInt32();
    Console.WriteLine($"Version: {version}");
}
```

### Deleting Values

```csharp
await cf.Accounts.Kv.DeleteValueAsync(namespaceId, "config/old-setting");
```

> [!TIP]
> Delete operations are idempotent. Deleting a non-existent key does not throw an error.

## Listing Keys

### List All Keys

```csharp
await foreach (var key in cf.Accounts.Kv.ListAllKeysAsync(namespaceId))
{
    Console.WriteLine($"Key: {key.Name}");

    if (key.Expiration is not null)
    {
        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(key.Expiration.Value);
        Console.WriteLine($"  Expires: {expiresAt}");
    }

    if (key.Metadata is not null)
    {
        Console.WriteLine($"  Metadata: {key.Metadata}");
    }
}
```

### Filter by Prefix

```csharp
// List only keys under "config/"
await foreach (var key in cf.Accounts.Kv.ListAllKeysAsync(namespaceId, prefix: "config/"))
{
    Console.WriteLine(key.Name);
}
```

### Manual Cursor Pagination

```csharp
var page = await cf.Accounts.Kv.ListKeysAsync(namespaceId, new ListKvKeysFilters(
    Prefix: "users/",
    Limit: 100
));

foreach (var key in page.Items)
{
    Console.WriteLine(key.Name);
}

// Get next page using cursor
if (page.CursorInfo?.Cursor is not null)
{
    var nextPage = await cf.Accounts.Kv.ListKeysAsync(namespaceId, new ListKvKeysFilters(
        Prefix: "users/",
        Limit: 100,
        Cursor: page.CursorInfo.Cursor
    ));
}
```

## Bulk Operations

### Bulk Write

Write up to 10,000 key-value pairs in a single request:

```csharp
var items = new[]
{
    new KvBulkWriteItem("config/setting1", "value1"),
    new KvBulkWriteItem("config/setting2", "value2"),
    new KvBulkWriteItem("config/setting3", "value3", ExpirationTtl: 3600),
};

var result = await cf.Accounts.Kv.BulkWriteAsync(namespaceId, items);

Console.WriteLine($"Successfully written: {result.SuccessfulKeyCount}");

if (result.UnsuccessfulKeys is { Count: > 0 })
{
    Console.WriteLine($"Failed keys: {string.Join(", ", result.UnsuccessfulKeys)}");
}
```

#### With Metadata

```csharp
var metadata = JsonSerializer.SerializeToElement(new { source = "import" });

var items = new[]
{
    new KvBulkWriteItem(
        Key: "data/item1",
        Value: "value1",
        Metadata: metadata
    ),
    new KvBulkWriteItem(
        Key: "data/item2",
        Value: "value2",
        Metadata: metadata,
        ExpirationTtl: 86400 // 24 hours
    )
};

await cf.Accounts.Kv.BulkWriteAsync(namespaceId, items);
```

### Bulk Delete

Delete up to 10,000 keys in a single request:

```csharp
var keysToDelete = new[] { "old/key1", "old/key2", "old/key3" };

var result = await cf.Accounts.Kv.BulkDeleteAsync(namespaceId, keysToDelete);

Console.WriteLine($"Successfully deleted: {result.SuccessfulKeyCount}");
```

### Bulk Get

Retrieve up to 100 values in a single request:

```csharp
var keys = new[] { "config/a", "config/b", "config/c" };

var values = await cf.Accounts.Kv.BulkGetAsync(namespaceId, keys);

foreach (var (key, value) in values)
{
    if (value is not null)
    {
        Console.WriteLine($"{key}: {value}");
    }
    else
    {
        Console.WriteLine($"{key}: (not found)");
    }
}
```

#### With Metadata

```csharp
var results = await cf.Accounts.Kv.BulkGetWithMetadataAsync(namespaceId, keys);

foreach (var (key, item) in results)
{
    if (item is not null)
    {
        Console.WriteLine($"{key}: {item.Value}");

        if (item.Metadata is not null)
        {
            Console.WriteLine($"  Metadata: {item.Metadata}");
        }
    }
}
```

## Models Reference

### KvNamespace

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Unique namespace identifier |
| `Title` | `string` | Human-readable namespace name |
| `SupportsUrlEncoding` | `bool?` | Whether the namespace supports URL-encoded keys |

### KvKey

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | The key name |
| `Expiration` | `long?` | Unix timestamp when the key expires |
| `Metadata` | `JsonElement?` | Arbitrary JSON metadata attached to the key |

### KvWriteOptions

| Property | Type | Description |
|----------|------|-------------|
| `Expiration` | `long?` | Absolute Unix timestamp for expiration |
| `ExpirationTtl` | `int?` | Seconds until expiration (minimum 60) |
| `Metadata` | `JsonElement?` | Arbitrary JSON metadata (max 1024 bytes) |

### KvBulkWriteItem

| Property | Type | Description |
|----------|------|-------------|
| `Key` | `string` | The key name (max 512 bytes) |
| `Value` | `string` | The value to store |
| `Expiration` | `long?` | Absolute Unix timestamp for expiration |
| `ExpirationTtl` | `int?` | Seconds until expiration (minimum 60) |
| `Metadata` | `JsonElement?` | Arbitrary JSON metadata |
| `Base64` | `bool?` | Set to true if value is base64-encoded binary |

### ListKvNamespacesFilters

| Property | Type | Description |
|----------|------|-------------|
| `Page` | `int?` | Page number (1-based) |
| `PerPage` | `int?` | Items per page |
| `Order` | `KvNamespaceOrderField?` | Field to order by (`Id` or `Title`) |
| `Direction` | `ListOrderDirection?` | Sort direction (`Ascending` or `Descending`) |

### ListKvKeysFilters

| Property | Type | Description |
|----------|------|-------------|
| `Prefix` | `string?` | Filter keys by prefix |
| `Limit` | `int?` | Maximum keys to return (default 1000) |
| `Cursor` | `string?` | Cursor for pagination |

### KvStringValueResult

Returned by `GetValueWithExpirationAsync`.

| Property | Type | Description |
|----------|------|-------------|
| `Value` | `string` | The value as a string |
| `Expiration` | `long?` | Unix timestamp when the key expires |

### KvValueResult

Returned by `GetValueBytesWithExpirationAsync`.

| Property | Type | Description |
|----------|------|-------------|
| `Value` | `byte[]` | The raw value bytes |
| `Expiration` | `long?` | Unix timestamp when the key expires |

### KvBulkWriteResult

Returned by `BulkWriteAsync`.

| Property | Type | Description |
|----------|------|-------------|
| `SuccessfulKeyCount` | `int` | Number of keys successfully written |
| `UnsuccessfulKeys` | `IReadOnlyList<string>?` | Keys that failed to write (should be retried) |

### KvBulkDeleteResult

Returned by `BulkDeleteAsync`.

| Property | Type | Description |
|----------|------|-------------|
| `SuccessfulKeyCount` | `int` | Number of keys successfully deleted |
| `UnsuccessfulKeys` | `IReadOnlyList<string>?` | Keys that failed to delete |

### KvBulkGetItemWithMetadata

Returned by `BulkGetWithMetadataAsync` as dictionary values.

| Property | Type | Description |
|----------|------|-------------|
| `Value` | `string?` | The value (null if key not found) |
| `Metadata` | `JsonElement?` | The metadata associated with the key |

## Common Patterns

### Configuration Store

```csharp
public class ConfigurationStore(ICloudflareApiClient cf, string namespaceId)
{
    public async Task<T?> GetAsync<T>(string key)
    {
        var json = await cf.Accounts.Kv.GetValueAsync(namespaceId, key);
        return json is null ? default : JsonSerializer.Deserialize<T>(json);
    }

    public async Task SetAsync<T>(string key, T value, int? ttlSeconds = null)
    {
        var json = JsonSerializer.Serialize(value);
        var options = ttlSeconds is not null
            ? new KvWriteOptions(ExpirationTtl: ttlSeconds)
            : null;

        await cf.Accounts.Kv.WriteValueAsync(namespaceId, key, json, options);
    }
}
```

### Session Store

```csharp
public class SessionStore(ICloudflareApiClient cf, string namespaceId)
{
    private const int SessionTtlSeconds = 3600; // 1 hour

    public async Task<string?> GetSessionAsync(string sessionId)
    {
        return await cf.Accounts.Kv.GetValueAsync(namespaceId, $"session/{sessionId}");
    }

    public async Task SetSessionAsync(string sessionId, string data)
    {
        await cf.Accounts.Kv.WriteValueAsync(
            namespaceId,
            $"session/{sessionId}",
            data,
            new KvWriteOptions(ExpirationTtl: SessionTtlSeconds)
        );
    }

    public async Task DeleteSessionAsync(string sessionId)
    {
        await cf.Accounts.Kv.DeleteValueAsync(namespaceId, $"session/{sessionId}");
    }
}
```

### Bulk Import

```csharp
public async Task ImportDataAsync(
    ICloudflareApiClient cf,
    string namespaceId,
    IEnumerable<KeyValuePair<string, string>> data)
{
    // Process in batches of 10,000
    var batches = data
        .Select(kv => new KvBulkWriteItem(kv.Key, kv.Value))
        .Chunk(10_000);

    foreach (var batch in batches)
    {
        var result = await cf.Accounts.Kv.BulkWriteAsync(namespaceId, batch);

        if (result.UnsuccessfulKeys is { Count: > 0 })
        {
            // Handle failures - retry or log
            Console.WriteLine($"Failed to write: {string.Join(", ", result.UnsuccessfulKeys)}");
        }
    }
}
```

## Required Permissions

| Permission | Scope | Level |
|------------|-------|-------|
| Workers KV Storage | Account | Read (for listing and reading) |
| Workers KV Storage | Account | Write (for create, update, delete) |

## Related

- [SDK Conventions](../../conventions.md) - Pagination patterns and common usage
- [API Coverage](../../api-coverage.md) - Full list of supported endpoints
- [Configuration](../../configuration.md) - SDK configuration options

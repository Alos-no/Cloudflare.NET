# DNS Records

Manage DNS records for your Cloudflare zones. The SDK supports creating, listing, finding, deleting, and bulk import/export of DNS records.

## Creating DNS Records

### Create a CNAME Record

```csharp
public class DnsService(ICloudflareApiClient cf)
{
    public async Task<DnsRecord> CreateCnameAsync(
        string zoneId,
        string hostname,
        string target)
    {
        return await cf.Zones.CreateCnameRecordAsync(
            zoneId,
            hostname,   // e.g., "cdn.example.com"
            target      // e.g., "example.cdn.cloudflare.net"
        );
    }
}
```

## Listing DNS Records

### List with Pagination

Use `ListDnsRecordsAsync` for manual pagination control:

```csharp
var page = await cf.Zones.ListDnsRecordsAsync(zoneId, new ListDnsRecordsFilters
{
    Type = "CNAME",
    Page = 1,
    PerPage = 50
});

Console.WriteLine($"Found {page.ResultInfo.TotalCount} records");

foreach (var record in page.Items)
{
    Console.WriteLine($"{record.Name} -> {record.Type}");
}
```

### List All Records

Use `ListAllDnsRecordsAsync` for automatic pagination:

```csharp
await foreach (var record in cf.Zones.ListAllDnsRecordsAsync(zoneId))
{
    Console.WriteLine($"{record.Name} ({record.Type})");
}
```

### Filtering Records

Filter by type, name, content, or proxied status:

```csharp
var filters = new ListDnsRecordsFilters
{
    Type = "A",                        // Filter by record type
    Name = "api.example.com",          // Filter by exact name
    Proxied = true,                    // Only proxied records
    Order = "name",                    // Sort by name
    Direction = ListOrderDirection.Asc // Ascending order
};

await foreach (var record in cf.Zones.ListAllDnsRecordsAsync(zoneId, filters))
{
    // Process filtered records
}
```

## Finding a Specific Record

Find a DNS record by its hostname:

```csharp
var record = await cf.Zones.FindDnsRecordByNameAsync(zoneId, "api.example.com");

if (record is not null)
{
    Console.WriteLine($"Found: {record.Id}");
}
else
{
    Console.WriteLine("Record not found");
}
```

> [!NOTE]
> If multiple records exist with the same name (e.g., A and AAAA), this method returns the first one from the API response.

## Deleting DNS Records

Delete a record by its ID:

```csharp
// First find the record
var record = await cf.Zones.FindDnsRecordByNameAsync(zoneId, "old.example.com");

if (record is not null)
{
    await cf.Zones.DeleteDnsRecordAsync(zoneId, record.Id);
}
```

## Bulk Import/Export

### Export to BIND Format

Export all DNS records as a BIND zone file:

```csharp
string bindContent = await cf.Zones.ExportDnsRecordsAsync(zoneId);

// Save to file
await File.WriteAllTextAsync("zone-backup.txt", bindContent);
```

### Import from BIND Format

Import DNS records from a BIND zone file:

```csharp
await using var bindStream = File.OpenRead("zone-import.txt");

var result = await cf.Zones.ImportDnsRecordsAsync(
    zoneId,
    bindStream,
    proxied: true,           // Proxy imported records through Cloudflare
    overwriteExisting: false // Don't overwrite existing records
);

Console.WriteLine($"Added: {result.RecordsAdded}");
Console.WriteLine($"Deleted: {result.RecordsDeleted}");
Console.WriteLine($"Parsed: {result.TotalRecordsParsed}");
```

## Models Reference

### DnsRecord

Represents a DNS record returned by the API.

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Unique identifier of the DNS record |
| `Name` | `string` | Record name (e.g., `api.example.com`) |
| `Type` | `string` | Record type (`A`, `AAAA`, `CNAME`, `TXT`, etc.) |

### ListDnsRecordsFilters

Filtering and pagination options for listing DNS records.

| Property | Type | Description |
|----------|------|-------------|
| `Type` | `string?` | Filter by record type (e.g., `A`, `CNAME`) |
| `Name` | `string?` | Filter by exact record name |
| `Content` | `string?` | Filter by record content |
| `Proxied` | `bool?` | Filter by proxied status |
| `Page` | `int?` | Page number (1-based) |
| `PerPage` | `int?` | Records per page (max 100) |
| `Order` | `string?` | Field to order by (`type`, `name`, `content`, `ttl`, `proxied`) |
| `Direction` | `ListOrderDirection?` | Sort direction (`Asc` or `Desc`) |

### DnsImportResult

Result of a bulk import operation.

| Property | Type | Description |
|----------|------|-------------|
| `RecordsAdded` | `int` | Number of records added |
| `RecordsDeleted` | `int` | Number of records deleted (if overwriting) |
| `TotalRecordsParsed` | `int` | Total records parsed from the BIND file |

## Common Patterns

### Migrate DNS Records Between Zones

```csharp
public async Task MigrateDnsAsync(string sourceZoneId, string targetZoneId)
{
    // Export from source
    var bindContent = await cf.Zones.ExportDnsRecordsAsync(sourceZoneId);

    // Import to target
    using var stream = new MemoryStream(Encoding.UTF8.GetBytes(bindContent));
    var result = await cf.Zones.ImportDnsRecordsAsync(
        targetZoneId,
        stream,
        proxied: true,
        overwriteExisting: true
    );

    Console.WriteLine($"Migrated {result.RecordsAdded} records");
}
```

### Clean Up Old CNAME Records

```csharp
public async Task CleanupCnamesAsync(string zoneId, string pattern)
{
    var filters = new ListDnsRecordsFilters { Type = "CNAME" };

    await foreach (var record in cf.Zones.ListAllDnsRecordsAsync(zoneId, filters))
    {
        if (record.Name.Contains(pattern))
        {
            await cf.Zones.DeleteDnsRecordAsync(zoneId, record.Id);
            Console.WriteLine($"Deleted: {record.Name}");
        }
    }
}
```

## Required Permissions

| Permission | Scope | Level |
|------------|-------|-------|
| DNS | Zone | Read (for listing/export) |
| DNS | Zone | Write (for create/delete/import) |

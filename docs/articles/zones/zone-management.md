# Zone Management

Manage Cloudflare zones (domains) through the SDK. This includes creating, listing, editing, and deleting zones, as well as triggering activation checks.

## Overview

Access zone management through `cf.Zones`:

```csharp
public class ZoneService(ICloudflareApiClient cf)
{
    public async Task<Zone> GetZoneAsync(string zoneId)
    {
        return await cf.Zones.GetZoneDetailsAsync(zoneId);
    }
}
```

## Creating Zones

> [!NOTE]
> **Preview:** This operation has limited test coverage.

### Basic Zone Creation

```csharp
var request = new CreateZoneRequest(
    Name: "example.com",
    Type: ZoneType.Full,
    Account: new ZoneAccountReference(accountId)
);

var zone = await cf.Zones.CreateZoneAsync(request);
Console.WriteLine($"Created zone: {zone.Name} ({zone.Status})");
```

### With DNS Record Import (Jump Start)

Automatically import existing DNS records when adding a zone:

```csharp
var request = new CreateZoneRequest(
    Name: "example.com",
    Type: ZoneType.Full,
    Account: new ZoneAccountReference(accountId),
    JumpStart: true  // Attempts to fetch existing DNS records
);

var zone = await cf.Zones.CreateZoneAsync(request);
```

> [!NOTE]
> Jump Start is a best-effort operation. If DNS record import fails, the zone is still created successfully.

### Zone Types

| Type | Description |
|------|-------------|
| `ZoneType.Full` | Full DNS hosting - Cloudflare becomes the authoritative nameserver |
| `ZoneType.Partial` | CNAME setup - DNS remains with current provider, specific records proxied via CNAME |
| `ZoneType.Secondary` | Secondary DNS - Cloudflare acts as secondary nameserver |

```csharp
// Create a partial (CNAME) zone
var partialZone = await cf.Zones.CreateZoneAsync(new CreateZoneRequest(
    Name: "saas-customer.com",
    Type: ZoneType.Partial,
    Account: new ZoneAccountReference(accountId)
));
```

## Listing Zones

### List with Pagination

```csharp
var page = await cf.Zones.ListZonesAsync(new ListZonesFilters
{
    Status = ZoneStatus.Active,
    Page = 1,
    PerPage = 50
});

Console.WriteLine($"Found {page.PageInfo.TotalCount} zones");

foreach (var zone in page.Items)
{
    Console.WriteLine($"{zone.Name}: {zone.Status}");
}
```

### List All Zones

Use automatic pagination to iterate through all zones:

```csharp
await foreach (var zone in cf.Zones.ListAllZonesAsync())
{
    Console.WriteLine($"{zone.Name} ({zone.Plan.Name})");
}
```

### Filtering Zones

```csharp
var filters = new ListZonesFilters
{
    Status = ZoneStatus.Active,           // Filter by status
    AccountId = accountId,                 // Filter by account
    Order = ZoneOrderField.Name,           // Sort by name
    Direction = ListOrderDirection.Asc     // Ascending order
};

await foreach (var zone in cf.Zones.ListAllZonesAsync(filters))
{
    Console.WriteLine($"{zone.Name}: {zone.Status}");
}
```

## Getting Zone Details

```csharp
var zone = await cf.Zones.GetZoneDetailsAsync(zoneId);

Console.WriteLine($"Zone: {zone.Name}");
Console.WriteLine($"Status: {zone.Status}");
Console.WriteLine($"Plan: {zone.Plan.Name}");
Console.WriteLine($"Paused: {zone.Paused}");
Console.WriteLine($"Nameservers: {string.Join(", ", zone.NameServers)}");
```

## Editing Zones

> [!NOTE]
> **Preview:** This operation has limited test coverage.

### Pause/Unpause a Zone

When paused, Cloudflare stops proxying traffic:

```csharp
// Pause the zone
var paused = await cf.Zones.SetZonePausedAsync(zoneId, true);
Console.WriteLine($"Zone paused: {paused.Paused}");

// Resume the zone
var resumed = await cf.Zones.SetZonePausedAsync(zoneId, false);
```

### Change Zone Type

```csharp
var updated = await cf.Zones.SetZoneTypeAsync(zoneId, ZoneType.Partial);
```

> [!NOTE]
> Not all type transitions are supported. Some require Enterprise plan.

### Set Vanity Nameservers

Custom-branded nameservers (Business/Enterprise only):

```csharp
var nameservers = new List<string>
{
    "ns1.yourdomain.com",
    "ns2.yourdomain.com"
};

var updated = await cf.Zones.SetVanityNameServersAsync(zoneId, nameservers);
```

### Generic Edit

Use `EditZoneAsync` for direct control:

```csharp
var request = new EditZoneRequest(Paused: true);
var zone = await cf.Zones.EditZoneAsync(zoneId, request);
```

> [!WARNING]
> Only one property can be changed per API call. The SDK provides convenience methods (`SetZonePausedAsync`, `SetZoneTypeAsync`) for clearer intent.

## Triggering Activation Check

For pending zones, manually trigger nameserver verification:

```csharp
var result = await cf.Zones.TriggerActivationCheckAsync(zoneId);

// Fetch updated status
var zone = await cf.Zones.GetZoneDetailsAsync(zoneId);
Console.WriteLine($"Zone status: {zone.Status}");
```

> [!NOTE]
> Rate limited: every 5 minutes (paid plans), every hour (free plans).

## Deleting Zones

> [!NOTE]
> **Preview:** This operation has limited test coverage.

```csharp
await cf.Zones.DeleteZoneAsync(zoneId);
```

> [!WARNING]
> This operation is irreversible. All DNS records, settings, and configuration will be permanently deleted.

## Models Reference

### Zone

The main zone object returned by the API.

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Unique zone identifier (32 hex characters) |
| `Name` | `string` | Domain name (e.g., "example.com") |
| `Status` | `ZoneStatus` | Zone status (active, pending, etc.) |
| `Account` | `ZoneAccount` | Account that owns the zone |
| `Plan` | `ZonePlan` | Billing plan (Free, Pro, Business, Enterprise) |
| `Paused` | `bool` | Whether the zone is paused |
| `Type` | `ZoneType` | Zone setup type (full, partial, secondary) |
| `NameServers` | `IReadOnlyList<string>` | Cloudflare-assigned nameservers |
| `VanityNameServers` | `IReadOnlyList<string>?` | Custom nameservers (if configured) |
| `CreatedOn` | `DateTime` | Creation timestamp |
| `ModifiedOn` | `DateTime` | Last modification timestamp |
| `ActivatedOn` | `DateTime?` | Activation timestamp (null if pending) |
| `DevelopmentMode` | `int` | Seconds remaining in dev mode (0 = off) |

### ZoneStatus (Extensible Enum)

| Known Value | Description |
|-------------|-------------|
| `Active` | Zone is active and proxying traffic |
| `Pending` | Awaiting nameserver verification |
| `Initializing` | Zone is being set up |
| `Moved` | Zone has been moved to another account |
| `Deleted` | Zone has been deleted |
| `Deactivated` | Zone has been deactivated |

### ZoneType (Extensible Enum)

| Known Value | Description |
|-------------|-------------|
| `Full` | Full DNS hosting with Cloudflare nameservers |
| `Partial` | CNAME setup (DNS remains with current provider) |
| `Secondary` | Secondary DNS configuration |

### ListZonesFilters

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string?` | Filter by exact domain name |
| `Status` | `ZoneStatus?` | Filter by zone status |
| `AccountId` | `string?` | Filter by account ID |
| `AccountName` | `string?` | Filter by account name |
| `Page` | `int?` | Page number (1-based) |
| `PerPage` | `int?` | Results per page (1-50) |
| `Order` | `ZoneOrderField?` | Field to order by |
| `Direction` | `ListOrderDirection?` | Sort direction |

## Common Patterns

### Find Zone by Domain Name

```csharp
public async Task<Zone?> FindZoneByNameAsync(string domainName)
{
    var filters = new ListZonesFilters(Name: domainName);

    await foreach (var zone in cf.Zones.ListAllZonesAsync(filters))
    {
        if (zone.Name.Equals(domainName, StringComparison.OrdinalIgnoreCase))
        {
            return zone;
        }
    }

    return null;
}
```

### Create Zone If Not Exists

```csharp
public async Task<Zone> EnsureZoneExistsAsync(string domainName, string accountId)
{
    var existing = await FindZoneByNameAsync(domainName);

    if (existing is not null)
    {
        return existing;
    }

    return await cf.Zones.CreateZoneAsync(new CreateZoneRequest(
        Name: domainName,
        Type: ZoneType.Full,
        Account: new ZoneAccountReference(accountId),
        JumpStart: true
    ));
}
```

### Wait for Zone Activation

```csharp
public async Task<Zone> WaitForActivationAsync(string zoneId, TimeSpan timeout)
{
    var deadline = DateTime.UtcNow + timeout;

    while (DateTime.UtcNow < deadline)
    {
        var zone = await cf.Zones.GetZoneDetailsAsync(zoneId);

        if (zone.Status == ZoneStatus.Active)
        {
            return zone;
        }

        // Trigger activation check (respect rate limits)
        await cf.Zones.TriggerActivationCheckAsync(zoneId);
        await Task.Delay(TimeSpan.FromMinutes(5));
    }

    throw new TimeoutException($"Zone {zoneId} did not activate within {timeout}");
}
```

## Required Permissions

| Permission | Scope | Level |
|------------|-------|-------|
| Zone | Zone | Read (for listing/get) |
| Zone | Zone | Write (for create/edit/delete) |

## Related

- [Zone Holds](zone-holds.md) - Prevent unauthorized zone takeovers
- [Zone Settings](zone-settings.md) - Configure zone-level settings
- [DNS Records](dns-records.md) - Manage DNS records

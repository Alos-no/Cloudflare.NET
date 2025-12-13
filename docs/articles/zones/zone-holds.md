# Zone Holds

Zone holds prevent creation and activation of zones with the same hostname, protecting against unauthorized domain takeovers.

## Overview

Access zone holds through `cf.Zones`:

```csharp
public class ZoneHoldService(ICloudflareApiClient cf)
{
    public async Task<ZoneHold> ProtectZoneAsync(string zoneId)
    {
        return await cf.Zones.CreateZoneHoldAsync(zoneId);
    }
}
```

## What Are Zone Holds?

A zone hold blocks anyone (including other Cloudflare accounts) from creating or activating a zone with the same domain name. This is useful for:

- **Preventing domain takeover** after deleting a zone
- **Protecting subdomains** in multi-tenant SaaS applications
- **Scheduled protection** that activates in the future

## Getting Zone Hold Status

```csharp
var hold = await cf.Zones.GetZoneHoldAsync(zoneId);

if (hold.Hold)
{
    Console.WriteLine($"Zone is held since {hold.HoldAfter}");
    Console.WriteLine($"Includes subdomains: {hold.IncludeSubdomains}");
}
else
{
    Console.WriteLine("Zone is not held");
}
```

## Creating Zone Holds

> [!NOTE]
> **Preview:** This operation has limited test coverage.

### Basic Hold

```csharp
var hold = await cf.Zones.CreateZoneHoldAsync(zoneId);
Console.WriteLine($"Hold active: {hold.Hold}");
```

### Hold with Subdomain Protection

Extend protection to all subdomains and SSL for SaaS custom hostnames:

```csharp
var hold = await cf.Zones.CreateZoneHoldAsync(zoneId, includeSubdomains: true);

// A hold on "example.com" now also blocks:
// - staging.example.com
// - api.example.com
// - Any SSL for SaaS custom hostnames
```

> [!TIP]
> Use subdomain protection for SaaS platforms where customers use custom hostnames under your domain.

## Updating Zone Holds

> [!NOTE]
> **Preview:** This operation has limited test coverage.

### Enable/Disable Subdomain Protection

```csharp
var request = new UpdateZoneHoldRequest(IncludeSubdomains: true);
var updated = await cf.Zones.UpdateZoneHoldAsync(zoneId, request);
```

### Schedule a Future Hold

Set a hold to activate at a specific time:

```csharp
// Hold will activate in 7 days
var request = new UpdateZoneHoldRequest(
    HoldAfter: DateTime.UtcNow.AddDays(7)
);

var scheduled = await cf.Zones.UpdateZoneHoldAsync(zoneId, request);
Console.WriteLine($"Hold scheduled for: {scheduled.HoldAfter}");
```

### Update Multiple Properties

```csharp
var request = new UpdateZoneHoldRequest(
    HoldAfter: DateTime.UtcNow,
    IncludeSubdomains: true
);

var updated = await cf.Zones.UpdateZoneHoldAsync(zoneId, request);
```

## Removing Zone Holds

Release the hold to allow zone creation with this hostname:

```csharp
var result = await cf.Zones.RemoveZoneHoldAsync(zoneId);
Console.WriteLine($"Hold released: {!result.Hold}");
```

> [!WARNING]
> After removing a hold, another account could claim the domain name.

## Models Reference

### ZoneHold

| Property | Type | Description |
|----------|------|-------------|
| `Hold` | `bool` | Whether the hold is currently active |
| `HoldAfter` | `DateTime?` | When the hold is/was activated |
| `IncludeSubdomains` | `bool?` | Whether subdomains are protected |

### UpdateZoneHoldRequest

| Property | Type | Description |
|----------|------|-------------|
| `HoldAfter` | `DateTime?` | Schedule when the hold should activate |
| `IncludeSubdomains` | `bool?` | Whether to extend protection to subdomains |

## Common Patterns

### Protect Zone Before Deletion

```csharp
public async Task SafeDeleteZoneAsync(string zoneId)
{
    // Create hold to prevent takeover
    await cf.Zones.CreateZoneHoldAsync(zoneId, includeSubdomains: true);

    // Now safe to delete - no one can claim this domain
    await cf.Zones.DeleteZoneAsync(zoneId);
}
```

### Check and Create Hold

```csharp
public async Task EnsureZoneProtectedAsync(string zoneId)
{
    var hold = await cf.Zones.GetZoneHoldAsync(zoneId);

    if (!hold.Hold)
    {
        await cf.Zones.CreateZoneHoldAsync(zoneId, includeSubdomains: true);
        Console.WriteLine("Zone protection enabled");
    }
}
```

### Temporary Hold Release

```csharp
public async Task TemporarilyReleaseHoldAsync(string zoneId, TimeSpan duration)
{
    // Remove current hold
    await cf.Zones.RemoveZoneHoldAsync(zoneId);

    // Schedule new hold to reactivate
    var reactivateAt = DateTime.UtcNow.Add(duration);
    var request = new UpdateZoneHoldRequest(
        HoldAfter: reactivateAt,
        IncludeSubdomains: true
    );

    await cf.Zones.CreateZoneHoldAsync(zoneId, includeSubdomains: true);
    await cf.Zones.UpdateZoneHoldAsync(zoneId, request);
}
```

## Required Permissions

| Permission | Scope | Level |
|------------|-------|-------|
| Zone | Zone | Write |

## Related

- [Zone Management](zone-management.md) - Create and manage zones
- [Zone Settings](zone-settings.md) - Configure zone settings

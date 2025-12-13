# Zone Access Rules

IP Access Rules allow you to block, challenge, or whitelist traffic based on IP address, IP range, ASN, or country at the zone level.

## Overview

Access the Zone Access Rules API through `cf.Zones.AccessRules`:

```csharp
public class FirewallService(ICloudflareApiClient cf)
{
    public async Task BlockIpAsync(string zoneId, string ipAddress)
    {
        await cf.Zones.AccessRules.CreateAsync(zoneId,
            new CreateAccessRuleRequest(
                Mode: AccessRuleMode.Block,
                Configuration: new IpConfiguration(ipAddress),
                Notes: "Blocked by automation"
            ));
    }
}
```

## Creating Access Rules

### Block an IP Address

```csharp
var rule = await cf.Zones.AccessRules.CreateAsync(zoneId,
    new CreateAccessRuleRequest(
        Mode: AccessRuleMode.Block,
        Configuration: new IpConfiguration("192.168.1.1"),
        Notes: "Malicious actor"
    ));
```

### Challenge an IP Range

```csharp
var rule = await cf.Zones.AccessRules.CreateAsync(zoneId,
    new CreateAccessRuleRequest(
        Mode: AccessRuleMode.Challenge,
        Configuration: new CidrConfiguration("10.0.0.0/8"),
        Notes: "Suspicious network"
    ));
```

### Block by ASN

```csharp
var rule = await cf.Zones.AccessRules.CreateAsync(zoneId,
    new CreateAccessRuleRequest(
        Mode: AccessRuleMode.Block,
        Configuration: new AsnConfiguration("AS12345"),
        Notes: "Known bad ASN"
    ));
```

### Block by Country

```csharp
var rule = await cf.Zones.AccessRules.CreateAsync(zoneId,
    new CreateAccessRuleRequest(
        Mode: AccessRuleMode.Block,
        Configuration: new CountryConfiguration("XX"),
        Notes: "Geo-blocking"
    ));
```

### Whitelist an IP

```csharp
var rule = await cf.Zones.AccessRules.CreateAsync(zoneId,
    new CreateAccessRuleRequest(
        Mode: AccessRuleMode.Whitelist,
        Configuration: new IpConfiguration("10.0.0.1"),
        Notes: "Office IP - bypass all security"
    ));
```

## Listing Access Rules

### List with Pagination

```csharp
var page = await cf.Zones.AccessRules.ListAsync(zoneId,
    new ListAccessRulesFilters
    {
        Mode = AccessRuleMode.Block,
        Page = 1,
        PerPage = 50
    });

foreach (var rule in page.Items)
{
    Console.WriteLine($"{rule.Configuration.Value}: {rule.Mode}");
}
```

### List All Rules

```csharp
await foreach (var rule in cf.Zones.AccessRules.ListAllAsync(zoneId))
{
    Console.WriteLine($"{rule.Id}: {rule.Configuration.Target} = {rule.Configuration.Value}");
}
```

### Filter by Configuration

```csharp
var filters = new ListAccessRulesFilters
{
    ConfigurationTarget = AccessRuleTarget.Ip,
    ConfigurationValue = "192.168.1.1"
};

await foreach (var rule in cf.Zones.AccessRules.ListAllAsync(zoneId, filters))
{
    // Process matching rules
}
```

## Getting a Specific Rule

```csharp
var rule = await cf.Zones.AccessRules.GetAsync(zoneId, ruleId);
Console.WriteLine($"Mode: {rule.Mode}, Created: {rule.CreatedOn}");
```

## Updating Access Rules

```csharp
var updated = await cf.Zones.AccessRules.UpdateAsync(zoneId, ruleId,
    new UpdateAccessRuleRequest(
        Mode: AccessRuleMode.Challenge,
        Notes: "Changed from block to challenge"
    ));
```

## Deleting Access Rules

```csharp
await cf.Zones.AccessRules.DeleteAsync(zoneId, ruleId);
```

## Models Reference

### AccessRuleMode (Extensible Enum)

<xref:Cloudflare.NET.Security.Firewall.Models.AccessRuleMode> defines the action to take when an access rule matches:

| Known Value | API Value | Description |
|-------------|-----------|-------------|
| `Block` | `block` | Block all requests (returns 403) |
| `Challenge` | `challenge` | Present interactive CAPTCHA challenge |
| `JsChallenge` | `js_challenge` | Present JavaScript challenge |
| `ManagedChallenge` | `managed_challenge` | Cloudflare dynamically chooses challenge type |
| `Whitelist` | `whitelist` | Allow requests, bypass all security checks |

```csharp
using Cloudflare.NET.Security.Firewall.Models;

// Use static properties for IntelliSense support
var mode = AccessRuleMode.Block;

// Extensible for future modes
AccessRuleMode customMode = "new-mode";
```

### AccessRuleTarget (Extensible Enum)

The target type for access rule configuration. This is an [extensible enum](../../conventions.md#extensible-enums) used in `ListAccessRulesFilters`.

| Known Value | Description |
|-------------|-------------|
| `Ip` | Single IPv4 or IPv6 address |
| `IpRange` | CIDR notation range |
| `Asn` | Autonomous System Number |
| `Country` | Two-letter country code |

### Configuration Types

Use polymorphic configuration types to create rules for different targets:

| Type | Target | Example |
|------|--------|---------|
| `IpConfiguration` | Single IP | `new IpConfiguration("192.168.1.1")` |
| `CidrConfiguration` | IP range | `new CidrConfiguration("10.0.0.0/8")` |
| `AsnConfiguration` | ASN | `new AsnConfiguration("AS12345")` |
| `CountryConfiguration` | Country code | `new CountryConfiguration("US")` |

### AccessRule

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Unique identifier |
| `Mode` | `AccessRuleMode` | Action to take |
| `Configuration` | `AccessRuleConfiguration` | Match target |
| `Notes` | `string?` | Optional notes |
| `AllowedModes` | `IReadOnlyList<AccessRuleMode>` | Available modes |
| `Scope` | `Scope` | Rule scope (zone/account) |
| `CreatedOn` | `DateTimeOffset?` | Creation timestamp |
| `ModifiedOn` | `DateTimeOffset?` | Last modification |

## Common Patterns

### Bulk Block IPs

```csharp
public async Task BlockIpsAsync(string zoneId, IEnumerable<string> ips, string reason)
{
    foreach (var ip in ips)
    {
        await cf.Zones.AccessRules.CreateAsync(zoneId,
            new CreateAccessRuleRequest(
                Mode: AccessRuleMode.Block,
                Configuration: new IpConfiguration(ip),
                Notes: reason
            ));
    }
}
```

### Find and Remove Rule

```csharp
public async Task UnblockIpAsync(string zoneId, string ip)
{
    var filters = new ListAccessRulesFilters
    {
        ConfigurationTarget = AccessRuleTarget.Ip,
        ConfigurationValue = ip
    };

    await foreach (var rule in cf.Zones.AccessRules.ListAllAsync(zoneId, filters))
    {
        await cf.Zones.AccessRules.DeleteAsync(zoneId, rule.Id);
    }
}
```

## Required Permissions

| Permission | Scope | Level |
|------------|-------|-------|
| Firewall Services | Zone | Write |

# Account Access Rules

IP Access Rules at the account level allow you to block, challenge, or whitelist traffic based on IP address, IP range, ASN, or country. Account-level rules apply to **all zones** within the account unless overridden by zone-level rules.

## Overview

Access the Account Access Rules API through `cf.Accounts.AccessRules`:

```csharp
public class AccountFirewallService(ICloudflareApiClient cf)
{
    public async Task BlockIpGloballyAsync(string ipAddress)
    {
        await cf.Accounts.AccessRules.CreateAsync(
            new CreateAccessRuleRequest(
                Mode: AccessRuleMode.Block,
                Configuration: new IpConfiguration(ipAddress),
                Notes: "Blocked globally across all zones"
            ));
    }
}
```

## Account vs Zone Rules

| Scope | Applied To | API Path |
|-------|------------|----------|
| Account | All zones in account | `cf.Accounts.AccessRules` |
| Zone | Single zone only | `cf.Zones.AccessRules` |

Account-level rules are useful for:
- Blocking known malicious IPs across your entire infrastructure
- Whitelisting office IPs for all zones
- Enforcing organization-wide security policies

## Creating Access Rules

### Block an IP Globally

```csharp
var rule = await cf.Accounts.AccessRules.CreateAsync(
    new CreateAccessRuleRequest(
        Mode: AccessRuleMode.Block,
        Configuration: new IpConfiguration("192.168.1.1"),
        Notes: "Known malicious actor - blocked globally"
    ));
```

### Challenge an IP Range

```csharp
var rule = await cf.Accounts.AccessRules.CreateAsync(
    new CreateAccessRuleRequest(
        Mode: AccessRuleMode.Challenge,
        Configuration: new CidrConfiguration("10.0.0.0/8"),
        Notes: "Suspicious network - challenge required"
    ));
```

### Block by ASN

```csharp
var rule = await cf.Accounts.AccessRules.CreateAsync(
    new CreateAccessRuleRequest(
        Mode: AccessRuleMode.Block,
        Configuration: new AsnConfiguration("AS12345"),
        Notes: "Known bad hosting provider"
    ));
```

### Block by Country

```csharp
var rule = await cf.Accounts.AccessRules.CreateAsync(
    new CreateAccessRuleRequest(
        Mode: AccessRuleMode.Block,
        Configuration: new CountryConfiguration("XX"),
        Notes: "Geo-blocking policy"
    ));
```

### Whitelist Corporate IPs

```csharp
var rule = await cf.Accounts.AccessRules.CreateAsync(
    new CreateAccessRuleRequest(
        Mode: AccessRuleMode.Whitelist,
        Configuration: new CidrConfiguration("203.0.113.0/24"),
        Notes: "Corporate office network - bypass all security"
    ));
```

## Listing Access Rules

### List with Pagination

```csharp
var page = await cf.Accounts.AccessRules.ListAsync(
    new ListAccessRulesFilters
    {
        Mode = AccessRuleMode.Block,
        Page = 1,
        PerPage = 50
    });

Console.WriteLine($"Total rules: {page.ResultInfo.TotalCount}");

foreach (var rule in page.Items)
{
    Console.WriteLine($"{rule.Configuration.Value}: {rule.Mode}");
}
```

### List All Rules

```csharp
await foreach (var rule in cf.Accounts.AccessRules.ListAllAsync())
{
    Console.WriteLine($"{rule.Id}: {rule.Configuration.Target} = {rule.Configuration.Value}");
}
```

### Filter by Mode

```csharp
var filters = new ListAccessRulesFilters
{
    Mode = AccessRuleMode.Whitelist
};

await foreach (var rule in cf.Accounts.AccessRules.ListAllAsync(filters))
{
    Console.WriteLine($"Whitelisted: {rule.Configuration.Value}");
}
```

### Filter by Configuration

```csharp
var filters = new ListAccessRulesFilters
{
    ConfigurationTarget = AccessRuleTarget.Ip,
    ConfigurationValue = "192.168.1.1"
};

await foreach (var rule in cf.Accounts.AccessRules.ListAllAsync(filters))
{
    Console.WriteLine($"Found rule: {rule.Id} with mode {rule.Mode}");
}
```

## Getting a Specific Rule

```csharp
var rule = await cf.Accounts.AccessRules.GetAsync(ruleId);

Console.WriteLine($"Mode: {rule.Mode}");
Console.WriteLine($"Target: {rule.Configuration.Target}");
Console.WriteLine($"Value: {rule.Configuration.Value}");
Console.WriteLine($"Notes: {rule.Notes}");
Console.WriteLine($"Created: {rule.CreatedOn}");
```

## Updating Access Rules

```csharp
var updated = await cf.Accounts.AccessRules.UpdateAsync(ruleId,
    new UpdateAccessRuleRequest(
        Mode: AccessRuleMode.Challenge,
        Notes: "Changed from block to challenge"
    ));
```

## Deleting Access Rules

```csharp
await cf.Accounts.AccessRules.DeleteAsync(ruleId);
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

The target type for access rule configurations. This is an [extensible enum](../../conventions.md#extensible-enums) that supports custom values for forward compatibility.

| Known Value | Description |
|-------------|-------------|
| `Ip` | Single IP address |
| `IpRange` | CIDR notation range |
| `Asn` | Autonomous System Number |
| `Country` | ISO country code |

### Configuration Types

Use the appropriate configuration class for your target type:

| Type | Target | Example |
|------|--------|---------|
| `IpConfiguration` | Single IP address | `new IpConfiguration("192.168.1.1")` |
| `CidrConfiguration` | IP range (CIDR) | `new CidrConfiguration("10.0.0.0/8")` |
| `AsnConfiguration` | ASN identifier | `new AsnConfiguration("AS12345")` |
| `CountryConfiguration` | ISO country code | `new CountryConfiguration("US")` |

### AccessRule

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Unique identifier |
| `Mode` | `AccessRuleMode` | Action to take on matching requests |
| `Configuration` | `AccessRuleConfiguration` | Match target and value |
| `Notes` | `string?` | Optional descriptive notes |
| `AllowedModes` | `IReadOnlyList<AccessRuleMode>` | Modes available for this rule |
| `Scope` | `Scope` | Rule scope (always "account" for this API) |
| `CreatedOn` | `DateTimeOffset?` | When the rule was created |
| `ModifiedOn` | `DateTimeOffset?` | When the rule was last modified |

### ListAccessRulesFilters

| Property | Type | Description |
|----------|------|-------------|
| `Mode` | `AccessRuleMode?` | Filter by action mode |
| `ConfigurationTarget` | `AccessRuleTarget?` | Filter by target type (ip, ip_range, asn, country) |
| `ConfigurationValue` | `string?` | Filter by exact value |
| `Notes` | `string?` | Filter by notes content |
| `Page` | `int?` | Page number (1-based) |
| `PerPage` | `int?` | Results per page (max 100) |
| `Order` | `string?` | Field to order by |
| `Direction` | `ListOrderDirection?` | Sort direction |

## Common Patterns

### Bulk Block IPs

```csharp
public async Task BlockMaliciousIpsAsync(IEnumerable<string> ips)
{
    foreach (var ip in ips)
    {
        await cf.Accounts.AccessRules.CreateAsync(
            new CreateAccessRuleRequest(
                Mode: AccessRuleMode.Block,
                Configuration: new IpConfiguration(ip),
                Notes: $"Bulk blocked: {DateTime.UtcNow:O}"
            ));
    }
}
```

### Sync Rules from Threat Feed

```csharp
public async Task SyncThreatFeedAsync(IEnumerable<string> threatIps)
{
    // Get existing rules
    var existingRules = new HashSet<string>();
    await foreach (var rule in cf.Accounts.AccessRules.ListAllAsync())
    {
        if (rule.Notes?.Contains("threat-feed") == true)
        {
            existingRules.Add(rule.Configuration.Value);
        }
    }

    // Add new threats
    foreach (var ip in threatIps.Where(ip => !existingRules.Contains(ip)))
    {
        await cf.Accounts.AccessRules.CreateAsync(
            new CreateAccessRuleRequest(
                Mode: AccessRuleMode.Block,
                Configuration: new IpConfiguration(ip),
                Notes: "threat-feed: Auto-imported"
            ));
    }
}
```

### Find and Remove Rule by IP

```csharp
public async Task UnblockIpAsync(string ip)
{
    var filters = new ListAccessRulesFilters
    {
        ConfigurationTarget = AccessRuleTarget.Ip,
        ConfigurationValue = ip
    };

    await foreach (var rule in cf.Accounts.AccessRules.ListAllAsync(filters))
    {
        await cf.Accounts.AccessRules.DeleteAsync(rule.Id);
        Console.WriteLine($"Removed rule {rule.Id} for {ip}");
    }
}
```

### Export Rules for Backup

```csharp
public async Task<List<AccessRule>> ExportRulesAsync()
{
    var rules = new List<AccessRule>();

    await foreach (var rule in cf.Accounts.AccessRules.ListAllAsync())
    {
        rules.Add(rule);
    }

    return rules;
}
```

## Required Permissions

| Permission | Scope | Level |
|------------|-------|-------|
| Firewall Services | Account | Write |

## Related

- [Zone Access Rules](../../zones/security/access-rules.md) - Zone-level access rules
- [Account WAF Rulesets](rulesets.md) - Advanced WAF rules at account level
- [Zone WAF Rulesets](../../zones/security/rulesets.md) - Zone-level WAF rules

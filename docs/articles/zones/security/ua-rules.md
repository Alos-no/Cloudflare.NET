# User-Agent Rules

User-Agent Rules allow you to block or challenge requests based on the User-Agent header. This is useful for blocking known bad bots, scrapers, or outdated clients.

## Overview

Access the UA Rules API through `cf.Zones.UaRules`:

```csharp
public class BotBlockingService(ICloudflareApiClient cf)
{
    public async Task BlockBadBotAsync(string zoneId, string userAgent)
    {
        await cf.Zones.UaRules.CreateAsync(zoneId,
            new CreateUaRuleRequest(
                Mode: UaRuleMode.Block,
                Configuration: new UaRuleConfiguration("ua", userAgent),
                Description: "Block known bad bot"
            ));
    }
}
```

## Creating UA Rules

### Block a User-Agent

```csharp
var rule = await cf.Zones.UaRules.CreateAsync(zoneId,
    new CreateUaRuleRequest(
        Mode: UaRuleMode.Block,
        Configuration: new UaRuleConfiguration("ua", "BadBot/1.0"),
        Description: "Block BadBot scraper"
    ));
```

### Challenge Suspicious User-Agents

```csharp
var rule = await cf.Zones.UaRules.CreateAsync(zoneId,
    new CreateUaRuleRequest(
        Mode: UaRuleMode.Challenge,
        Configuration: new UaRuleConfiguration("ua", "curl"),
        Description: "Challenge curl requests"
    ));
```

### Block with Pattern Matching

User-Agent matching supports partial matches:

```csharp
// Blocks any User-Agent containing "scraper"
var rule = await cf.Zones.UaRules.CreateAsync(zoneId,
    new CreateUaRuleRequest(
        Mode: UaRuleMode.Block,
        Configuration: new UaRuleConfiguration("ua", "scraper"),
        Description: "Block scrapers"
    ));
```

## Listing UA Rules

### List All Rules

```csharp
await foreach (var rule in cf.Zones.UaRules.ListAllAsync(zoneId))
{
    Console.WriteLine($"{rule.Id}: {rule.Configuration.Value} -> {rule.Mode}");
}
```

### List with Pagination

```csharp
var page = await cf.Zones.UaRules.ListAsync(zoneId,
    new ListUaRulesFilters { PerPage = 50 });

foreach (var rule in page.Items)
{
    Console.WriteLine($"{rule.Description}: {rule.Mode}");
}
```

## Getting a Specific Rule

```csharp
var rule = await cf.Zones.UaRules.GetAsync(zoneId, ruleId);
Console.WriteLine($"User-Agent: {rule.Configuration.Value}");
Console.WriteLine($"Mode: {rule.Mode}");
Console.WriteLine($"Paused: {rule.Paused}");
```

## Updating UA Rules

```csharp
var updated = await cf.Zones.UaRules.UpdateAsync(zoneId, ruleId,
    new UpdateUaRuleRequest(
        Mode: UaRuleMode.ManagedChallenge,
        Description: "Updated to managed challenge"
    ));
```

### Pause a Rule

```csharp
var updated = await cf.Zones.UaRules.UpdateAsync(zoneId, ruleId,
    new UpdateUaRuleRequest(Paused: true));
```

## Deleting UA Rules

```csharp
await cf.Zones.UaRules.DeleteAsync(zoneId, ruleId);
```

## Models Reference

### UaRule

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Unique identifier |
| `Mode` | `UaRuleMode` | Action to take |
| `Configuration` | `UaRuleConfiguration` | User-Agent pattern |
| `Paused` | `bool` | Whether the rule is paused |
| `Description` | `string?` | Optional description |

### UaRuleMode (Extensible Enum)

The action to take when a User-Agent rule matches. This is an [extensible enum](../../conventions.md#extensible-enums) that supports custom values for forward compatibility.

| Known Value | Description |
|-------------|-------------|
| `Block` | Block the request |
| `Challenge` | Present CAPTCHA challenge |
| `JsChallenge` | Present JavaScript challenge |
| `ManagedChallenge` | Cloudflare-managed challenge |

```csharp
// Using known values
UaRuleMode.Block
UaRuleMode.Challenge
UaRuleMode.JsChallenge
UaRuleMode.ManagedChallenge

// Future-proof: accepts unknown values from API
UaRuleMode customMode = "new-mode";
```

### UaRuleConfiguration

| Property | Type | Description |
|----------|------|-------------|
| `Target` | `string` | Always `"ua"` |
| `Value` | `string` | User-Agent pattern to match |

## User-Agent Matching

- Matching is **case-insensitive**
- Matching is **substring-based** (pattern can appear anywhere in User-Agent)
- Example: Pattern `"bot"` matches `"Googlebot"`, `"bingbot"`, `"MyBot/1.0"`

## Common Patterns

### Block Known Bad Bots

```csharp
var badBots = new[]
{
    "AhrefsBot",
    "MJ12bot",
    "DotBot",
    "SemrushBot",
    "BLEXBot"
};

foreach (var bot in badBots)
{
    await cf.Zones.UaRules.CreateAsync(zoneId,
        new CreateUaRuleRequest(
            Mode: UaRuleMode.Block,
            Configuration: new UaRuleConfiguration("ua", bot),
            Description: $"Block {bot}"
        ));
}
```

### Challenge Empty User-Agents

```csharp
await cf.Zones.UaRules.CreateAsync(zoneId,
    new CreateUaRuleRequest(
        Mode: UaRuleMode.Challenge,
        Configuration: new UaRuleConfiguration("ua", ""),
        Description: "Challenge empty User-Agent"
    ));
```

### Audit Existing Rules

```csharp
public async Task<List<string>> GetBlockedUserAgentsAsync(string zoneId)
{
    var blocked = new List<string>();

    await foreach (var rule in cf.Zones.UaRules.ListAllAsync(zoneId))
    {
        if (rule.Mode == UaRuleMode.Block && !rule.Paused)
        {
            blocked.Add(rule.Configuration.Value);
        }
    }

    return blocked;
}
```

## Required Permissions

| Permission | Scope | Level |
|------------|-------|-------|
| Firewall Services | Zone | Write |

## Best Practices

1. **Be specific** with User-Agent patterns to avoid blocking legitimate traffic
2. **Use Challenge** instead of Block for uncertain cases
3. **Monitor traffic** before and after adding rules
4. **Document rules** with clear descriptions
5. **Review periodically** - bot User-Agents change over time

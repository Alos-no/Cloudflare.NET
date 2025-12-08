# Security & Firewall

The Cloudflare.NET SDK provides comprehensive support for managing Cloudflare's security features including WAF rulesets, IP access rules, zone lockdowns, and user-agent rules.

## WAF Rulesets

### Ruleset Phases

Cloudflare WAF uses phases to determine when rules execute. The SDK provides constants for all phases:

```csharp
using Cloudflare.NET.Security;

// Available phases
SecurityConstants.RulesetPhases.HttpRequestFirewallManaged  // Managed WAF rules
SecurityConstants.RulesetPhases.HttpRequestFirewallCustom   // Custom WAF rules
SecurityConstants.RulesetPhases.HttpRateLimit               // Rate limiting rules
```

### Creating Custom Rules

```csharp
public class WafService(ICloudflareApiClient cf)
{
    public async Task<Ruleset> CreateBlockingRuleAsync(string zoneId, string expression)
    {
        var request = new CreateRulesetRequest
        {
            Name = "Custom Blocking Rules",
            Kind = "zone",
            Phase = SecurityConstants.RulesetPhases.HttpRequestFirewallCustom,
            Rules =
            [
                new CreateRuleRequest
                {
                    Action = RuleAction.Block,
                    Expression = expression,
                    Description = "Block malicious requests",
                    Enabled = true
                }
            ]
        };

        return await cf.Zones.Rulesets.CreateRulesetAsync(zoneId, request);
    }
}
```

### Rate Limiting Rules

```csharp
public async Task<Ruleset> CreateRateLimitRuleAsync(string zoneId)
{
    var request = new CreateRulesetRequest
    {
        Name = "API Rate Limiting",
        Kind = "zone",
        Phase = SecurityConstants.RulesetPhases.HttpRateLimit,
        Rules =
        [
            new CreateRuleRequest
            {
                Action = RuleAction.Block,
                Expression = "(http.request.uri.path contains \"/api/\")",
                Description = "Rate limit API endpoints",
                Enabled = true,
                RateLimit = new RateLimitParameters
                {
                    RequestsPerPeriod = 100,
                    Period = SecurityConstants.RateLimiting.Periods.OneMinute,
                    Characteristics =
                    [
                        SecurityConstants.RateLimiting.Characteristics.IpSource
                    ],
                    MitigationTimeout = 60
                }
            }
        ]
    };

    return await cf.Zones.Rulesets.CreateRulesetAsync(zoneId, request);
}
```

### Listing Rulesets

```csharp
// List all rulesets for a zone
await foreach (var ruleset in cf.Zones.Rulesets.ListAllRulesetsAsync(zoneId))
{
    Console.WriteLine($"{ruleset.Name} ({ruleset.Phase}): {ruleset.Rules?.Count ?? 0} rules");
}

// Get a specific ruleset with all rules
var ruleset = await cf.Zones.Rulesets.GetRulesetAsync(zoneId, rulesetId);
```

## IP Access Rules

Manage IP-based access controls at the zone or account level:

```csharp
public class FirewallService(ICloudflareApiClient cf)
{
    // Block an IP address
    public async Task<AccessRule> BlockIpAsync(string zoneId, string ipAddress, string note)
    {
        var request = new CreateAccessRuleRequest
        {
            Mode = AccessRuleMode.Block,
            Configuration = new AccessRuleConfiguration
            {
                Target = ConfigurationTarget.Ip,
                Value = ipAddress
            },
            Notes = note
        };

        return await cf.Zones.AccessRules.CreateAccessRuleAsync(zoneId, request);
    }

    // Challenge an IP range
    public async Task<AccessRule> ChallengeIpRangeAsync(string zoneId, string cidr)
    {
        var request = new CreateAccessRuleRequest
        {
            Mode = AccessRuleMode.Challenge,
            Configuration = new AccessRuleConfiguration
            {
                Target = ConfigurationTarget.IpRange,
                Value = cidr
            }
        };

        return await cf.Zones.AccessRules.CreateAccessRuleAsync(zoneId, request);
    }

    // List all access rules
    public async IAsyncEnumerable<AccessRule> ListAccessRulesAsync(string zoneId)
    {
        await foreach (var rule in cf.Zones.AccessRules.ListAllAccessRulesAsync(zoneId))
        {
            yield return rule;
        }
    }
}
```

### Access Rule Modes

| Mode | Description |
|------|-------------|
| `Block` | Block all requests from the target |
| `Challenge` | Present a CAPTCHA challenge |
| `JsChallenge` | Present a JavaScript challenge |
| `ManagedChallenge` | Let Cloudflare decide the challenge type |
| `Whitelist` | Allow requests, bypassing other rules |

### Configuration Targets

| Target | Description | Example Value |
|--------|-------------|---------------|
| `Ip` | Single IP address | `192.168.1.1` |
| `IpRange` | CIDR range | `192.168.1.0/24` |
| `Asn` | Autonomous System Number | `AS13335` |
| `Country` | Country code | `US` |

## Zone Lockdown

Restrict access to specific URLs to whitelisted IPs:

```csharp
public async Task<ZoneLockdown> CreateLockdownAsync(
    string zoneId,
    IEnumerable<string> urls,
    IEnumerable<string> allowedIps)
{
    return await cf.Zones.Lockdown.CreateLockdownAsync(zoneId, new ZoneLockdown
    {
        Urls = urls.ToList(),
        Configurations = allowedIps.Select(ip => new LockdownConfiguration
        {
            Target = "ip",
            Value = ip
        }).ToList(),
        Description = "Admin panel lockdown"
    });
}
```

## User-Agent Rules

Block or challenge requests based on User-Agent:

```csharp
public async Task<UaRule> BlockBadBotAsync(string zoneId, string userAgentPattern)
{
    return await cf.Zones.UaRules.CreateUaRuleAsync(zoneId, new UaRule
    {
        Mode = "block",
        Configuration = new UaRuleConfiguration
        {
            Target = "ua",
            Value = userAgentPattern
        },
        Description = "Block known bad bot"
    });
}
```

## Account-Level Rules

For enterprise accounts, rules can be applied at the account level:

```csharp
// Account-level access rules
await cf.Accounts.AccessRules.CreateAccessRuleAsync(request);

// Account-level rulesets
await cf.Accounts.Rulesets.CreateRulesetAsync(request);
```

## Security Constants Reference

The `SecurityConstants` class provides centralized constants:

```csharp
// Ruleset phases
SecurityConstants.RulesetPhases.HttpRequestFirewallManaged
SecurityConstants.RulesetPhases.HttpRequestFirewallCustom
SecurityConstants.RulesetPhases.HttpRateLimit

// Skip products (for skip action)
SecurityConstants.SkipProducts.ZoneLockdown
SecurityConstants.SkipProducts.Waf
SecurityConstants.SkipProducts.RateLimit

// Rate limiting characteristics
SecurityConstants.RateLimiting.Characteristics.IpSource
SecurityConstants.RateLimiting.Characteristics.UriPath
SecurityConstants.RateLimiting.Characteristics.HttpMethod

// Rate limiting periods (in seconds)
SecurityConstants.RateLimiting.Periods.TenSeconds      // 10
SecurityConstants.RateLimiting.Periods.OneMinute       // 60
SecurityConstants.RateLimiting.Periods.TwoMinutes      // 120
SecurityConstants.RateLimiting.Periods.FiveMinutes     // 300
SecurityConstants.RateLimiting.Periods.TenMinutes      // 600
```

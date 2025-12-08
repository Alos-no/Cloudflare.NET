# Zone WAF Rulesets

WAF Rulesets allow you to create custom firewall rules and rate limiting rules at the zone level using Cloudflare's Ruleset Engine.

## Overview

Access the Zone Rulesets API through `cf.Zones.Rulesets`:

```csharp
public class WafService(ICloudflareApiClient cf)
{
    public async Task CreateBlockingRuleAsync(string zoneId)
    {
        await cf.Zones.Rulesets.CreateAsync(zoneId,
            new CreateRulesetRequest(
                Name: "Custom Security Rules",
                Kind: "zone",
                Phase: SecurityConstants.RulesetPhases.HttpRequestFirewallCustom,
                Rules: [
                    new CreateRuleRequest(
                        Action: RuleAction.Block,
                        Expression: "(http.request.uri.path contains \"/admin\")",
                        Description: "Block admin access",
                        Enabled: true
                    )
                ]
            ));
    }
}
```

## Ruleset Phases

Use `SecurityConstants.RulesetPhases` for phase constants:

| Constant | Phase | Description |
|----------|-------|-------------|
| `HttpRequestFirewallManaged` | `http_request_firewall_managed` | Managed WAF rules |
| `HttpRequestFirewallCustom` | `http_request_firewall_custom` | Custom WAF rules |
| `HttpRateLimit` | `http_ratelimit` | Rate limiting rules |
| `HttpRequestLateTransform` | `http_request_late_transform` | Header modification |
| `HttpRequestDynamicRedirect` | `http_request_dynamic_redirect` | URL redirects |

## Creating Rulesets

### Custom WAF Ruleset

```csharp
var ruleset = await cf.Zones.Rulesets.CreateAsync(zoneId,
    new CreateRulesetRequest(
        Name: "Security Rules",
        Kind: "zone",
        Phase: SecurityConstants.RulesetPhases.HttpRequestFirewallCustom,
        Description: "Custom security rules for the zone",
        Rules: [
            new CreateRuleRequest(
                Action: RuleAction.Block,
                Expression: "(http.request.uri.query contains \"<script\")",
                Description: "Block XSS attempts",
                Enabled: true
            ),
            new CreateRuleRequest(
                Action: RuleAction.Challenge,
                Expression: "(cf.threat_score gt 50)",
                Description: "Challenge high threat scores",
                Enabled: true
            )
        ]
    ));
```

### Rate Limiting Ruleset

```csharp
var ruleset = await cf.Zones.Rulesets.CreateAsync(zoneId,
    new CreateRulesetRequest(
        Name: "API Rate Limits",
        Kind: "zone",
        Phase: SecurityConstants.RulesetPhases.HttpRateLimit,
        Rules: [
            new CreateRuleRequest(
                Action: RuleAction.Block,
                Expression: "(http.request.uri.path starts_with \"/api/\")",
                Description: "Rate limit API endpoints",
                Enabled: true,
                Ratelimit: new RateLimitParameters
                {
                    Characteristics = [SecurityConstants.RateLimiting.Characteristics.IpSource],
                    Period = SecurityConstants.RateLimiting.Periods.P60_SECONDS,
                    RequestsPerPeriod = 100,
                    MitigationTimeout = 60
                }
            )
        ]
    ));
```

## Phase Entrypoints

Phase entrypoints are the main rulesets that execute for each phase.

### Get Phase Entrypoint

```csharp
var entrypoint = await cf.Zones.Rulesets.GetPhaseEntrypointAsync(
    zoneId,
    SecurityConstants.RulesetPhases.HttpRequestFirewallCustom);
```

### Update Phase Entrypoint

Replace all rules in a phase:

```csharp
var updated = await cf.Zones.Rulesets.UpdatePhaseEntrypointAsync(
    zoneId,
    SecurityConstants.RulesetPhases.HttpRequestFirewallCustom,
    [
        new CreateRuleRequest(
            Action: RuleAction.Block,
            Expression: "(ip.src eq 192.168.1.1)",
            Description: "Block specific IP",
            Enabled: true
        )
    ]);
```

## Managing Rules

### Add a Rule to Existing Ruleset

```csharp
var updated = await cf.Zones.Rulesets.AddRuleAsync(zoneId, rulesetId,
    new CreateRuleRequest(
        Action: RuleAction.Log,
        Expression: "(http.request.uri.path eq \"/debug\")",
        Description: "Log debug requests",
        Enabled: true
    ));
```

### Update a Rule

```csharp
var updated = await cf.Zones.Rulesets.UpdateRuleAsync(zoneId, rulesetId, ruleId,
    new { enabled = false });
```

### Delete a Rule

```csharp
var updated = await cf.Zones.Rulesets.DeleteRuleAsync(zoneId, rulesetId, ruleId);
```

## Listing Rulesets

### List All Rulesets

```csharp
await foreach (var ruleset in cf.Zones.Rulesets.ListAllAsync(zoneId))
{
    Console.WriteLine($"{ruleset.Name} ({ruleset.Phase}): {ruleset.Rules?.Count ?? 0} rules");
}
```

### Get Ruleset Details

```csharp
var ruleset = await cf.Zones.Rulesets.GetAsync(zoneId, rulesetId);

foreach (var rule in ruleset.Rules ?? [])
{
    Console.WriteLine($"  {rule.Description}: {rule.Action}");
}
```

## Rule Actions

| Action | Description |
|--------|-------------|
| `Block` | Block the request |
| `Challenge` | Present CAPTCHA |
| `ManagedChallenge` | Cloudflare-managed challenge |
| `JsChallenge` | JavaScript challenge |
| `Log` | Log without blocking |
| `Skip` | Skip subsequent rules/products |
| `Execute` | Execute another ruleset |

## Rate Limiting

### RateLimitParameters

| Property | Type | Description |
|----------|------|-------------|
| `Characteristics` | `string[]` | Properties to count (e.g., `ip.src`) |
| `Period` | `int` | Time window in seconds (10, 60, 120, 300, 600) |
| `RequestsPerPeriod` | `int` | Request threshold |
| `MitigationTimeout` | `int` | Block duration in seconds |
| `CountingExpression` | `string?` | Custom counting expression |

### Rate Limit Characteristics

Use `SecurityConstants.RateLimiting.Characteristics`:

| Constant | Value | Description |
|----------|-------|-------------|
| `IpSource` | `ip.src` | Client IP address |
| `UriPath` | `http.request.uri.path` | Request path |
| `HttpMethod` | `http.request.method` | HTTP method |
| `CfConnectingIp` | Header value | CF-Connecting-IP header |

## Common Patterns

### Protect Login Endpoint

```csharp
var ruleset = await cf.Zones.Rulesets.CreateAsync(zoneId,
    new CreateRulesetRequest(
        Name: "Login Protection",
        Kind: "zone",
        Phase: SecurityConstants.RulesetPhases.HttpRateLimit,
        Rules: [
            new CreateRuleRequest(
                Action: RuleAction.Block,
                Expression: "(http.request.uri.path eq \"/login\" and http.request.method eq \"POST\")",
                Description: "Rate limit login attempts",
                Enabled: true,
                Ratelimit: new RateLimitParameters
                {
                    Characteristics = [SecurityConstants.RateLimiting.Characteristics.IpSource],
                    Period = 60,
                    RequestsPerPeriod = 5,
                    MitigationTimeout = 300
                }
            )
        ]
    ));
```

### Block Bad Bots

```csharp
new CreateRuleRequest(
    Action: RuleAction.Block,
    Expression: "(cf.bot_management.score lt 30 and not cf.bot_management.verified_bot)",
    Description: "Block likely bots",
    Enabled: true
)
```

## Required Permissions

| Permission | Scope | Level |
|------------|-------|-------|
| Zone WAF | Zone | Read (for listing) |
| Zone WAF | Zone | Write (for create/update/delete) |
